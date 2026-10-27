//  Copyright (c) 2018 Alachisoft
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.SocketServer.Http
{
    /// <summary>
    /// Minimal HTTP server hosted inside the NCache Service (NCacheServer.RestPort, default 9804).
    ///
    /// It owns the listener and routes each request to the <see cref="IServiceHttpEndpoint"/> registered
    /// for its path. Endpoints are registered with <see cref="Register"/> before <see cref="Start"/>
    /// (later registrations are accepted too). The server provides, for every endpoint:
    ///  - path matching (case-insensitive, trailing slash ignored, "/*" prefix routes),
    ///  - 404 for unknown paths (listing the registered paths), 405 with an Allow header for
    ///    unsupported methods, HEAD served automatically for GET,
    ///  - 500 with a JSON error body when an endpoint throws,
    ///  - one thread-pool thread per request, so an endpoint may block.
    /// </summary>
    public sealed class ServiceHttpServer : IDisposable
    {
        private const string PrefixMarker = "/*";

        private readonly int _port;
        private readonly object _sync = new object();
        private readonly List<IServiceHttpEndpoint> _endpoints = new List<IServiceHttpEndpoint>();
        private HttpListener _listener;
        private volatile bool _stopped;

        public ServiceHttpServer(int port)
        {
            if (port <= 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException("port", "Service HTTP port must be between 1 and 65535.");
            _port = port;
        }

        public int Port
        {
            get { return _port; }
        }

        public bool IsListening
        {
            get { return _listener != null && _listener.IsListening; }
        }

        /// <summary>Snapshot of the registered endpoints, in registration order.</summary>
        public IList<IServiceHttpEndpoint> Endpoints
        {
            get { lock (_sync) return _endpoints.ToArray(); }
        }

        /// <summary>
        /// Registers an endpoint. Returns the server so registrations can be chained.
        /// Throws if the endpoint's path is malformed or already taken.
        /// </summary>
        public ServiceHttpServer Register(IServiceHttpEndpoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");

            string path = endpoint.Path;
            if (string.IsNullOrEmpty(path) || path[0] != '/')
                throw new ArgumentException("Endpoint path must be an absolute path starting with '/'. Endpoint: " + endpoint.GetType().FullName);
            if (path.IndexOf('?') >= 0 || path.IndexOf('#') >= 0)
                throw new ArgumentException("Endpoint path must not contain a query or fragment: " + path);
            if (endpoint.Methods == null || !endpoint.Methods.Any())
                throw new ArgumentException("Endpoint must accept at least one HTTP method: " + path);

            lock (_sync)
            {
                foreach (IServiceHttpEndpoint existing in _endpoints)
                {
                    if (string.Equals(NormalizePath(existing.Path), NormalizePath(path), StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("An endpoint is already registered at " + path + ": " + existing.GetType().FullName);
                }
                _endpoints.Add(endpoint);
            }
            return this;
        }

        public void Start()
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("HttpListener is not supported on this platform.");

            lock (_sync)
            {
                if (IsListening)
                    return;

                // Prefer all interfaces; fall back to loopback only (e.g. when the process lacks a URL ACL).
                string[] prefixes =
                {
                    "http://*:" + _port + "/",
                    "http://localhost:" + _port + "/",
                };

                Exception lastError = null;
                foreach (string prefix in prefixes)
                {
                    HttpListener listener = new HttpListener();
                    try
                    {
                        listener.Prefixes.Add(prefix);
                        listener.Start();
                        _listener = listener;
                        _stopped = false;
                        NCacheServiceLogger.LogInfo("Service HTTP server is listening at " + prefix + " with endpoints: " + DescribeEndpoints());
                        break;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        NCacheServiceLogger.LogError("Service HTTP server could not listen at " + prefix + ". " + ex.Message);
                        try { listener.Close(); }
                        catch (Exception) { }
                    }
                }

                if (_listener == null)
                    throw new InvalidOperationException("Unable to start the service HTTP server on port " + _port + ".", lastError);

                Task.Run((Func<Task>)AcceptLoop);
            }
        }

        public void Stop()
        {
            HttpListener listener;
            lock (_sync)
            {
                _stopped = true;
                listener = _listener;
                _listener = null;
            }

            if (listener == null)
                return;

            try
            {
                listener.Stop();
                listener.Close();
                NCacheServiceLogger.LogInfo("Service HTTP server has been stopped.");
            }
            catch (Exception ex)
            {
                NCacheServiceLogger.LogError("Error while stopping the service HTTP server. " + ex);
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public string DescribeEndpoints()
        {
            return string.Join(", ", Endpoints.Select(e => string.Join("/", e.Methods) + " " + e.Path));
        }

        private async Task AcceptLoop()
        {
            HttpListener listener = _listener;

            while (!_stopped && listener != null && listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException)
                {
                    break; // listener stopped
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_stopped) break;
                    NCacheServiceLogger.LogError("Service HTTP server failed to accept a request. " + ex);
                    continue;
                }

                ThreadPool.QueueUserWorkItem(state => HandleRequest((HttpListenerContext)state), context);
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            bool isHead = string.Equals(request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase);

            try
            {
                string path = NormalizePath(request.Url != null ? request.Url.AbsolutePath : string.Empty);

                string pathSuffix;
                IServiceHttpEndpoint endpoint = Resolve(path, out pathSuffix);
                if (endpoint == null)
                {
                    Write(response, NotFound(path), isHead);
                    return;
                }

                string method = isHead ? "GET" : request.HttpMethod.ToUpperInvariant();
                if (!endpoint.Methods.Any(m => string.Equals(m, method, StringComparison.OrdinalIgnoreCase)))
                {
                    ServiceHttpResponse notAllowed = ServiceHttpResponse.Json(HttpStatusCode.MethodNotAllowed,
                        JsonWriter.ErrorObject("Method " + request.HttpMethod + " is not allowed at " + path));
                    notAllowed.Headers["Allow"] = AllowHeader(endpoint);
                    Write(response, notAllowed, isHead);
                    return;
                }

                ServiceHttpResponse result = endpoint.Handle(new ServiceHttpRequest(request, path, pathSuffix))
                                             ?? ServiceHttpResponse.Empty(HttpStatusCode.NoContent);
                Write(response, result, isHead);
            }
            catch (Exception ex)
            {
                NCacheServiceLogger.LogError("Service HTTP request " + request.HttpMethod + " " + request.RawUrl + " failed. " + ex);
                try
                {
                    Write(response, ServiceHttpResponse.Json(HttpStatusCode.InternalServerError,
                        JsonWriter.ErrorObject("Request failed: " + ex.Message)), isHead);
                }
                catch (Exception) { }
            }
            finally
            {
                try { response.Close(); }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Finds the endpoint for a normalized path: an exact match wins, otherwise the longest
        /// registered "/*" prefix that covers the path.
        /// </summary>
        private IServiceHttpEndpoint Resolve(string path, out string pathSuffix)
        {
            pathSuffix = string.Empty;
            IServiceHttpEndpoint best = null;
            int bestLength = -1;

            foreach (IServiceHttpEndpoint endpoint in Endpoints)
            {
                string registered = endpoint.Path;
                bool isPrefix = registered.EndsWith(PrefixMarker, StringComparison.Ordinal);
                string basePath = NormalizePath(isPrefix ? registered.Substring(0, registered.Length - PrefixMarker.Length) : registered);

                if (string.Equals(basePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    pathSuffix = string.Empty;
                    return endpoint;
                }

                if (isPrefix && basePath.Length > bestLength && IsBelow(path, basePath))
                {
                    best = endpoint;
                    bestLength = basePath.Length;
                    pathSuffix = basePath == "/" ? path.Substring(1) : path.Substring(basePath.Length + 1);
                }
            }

            return best;
        }

        private static bool IsBelow(string path, string basePath)
        {
            if (basePath == "/")
                return path.Length > 1;
            return path.Length > basePath.Length
                && path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
                && path[basePath.Length] == '/';
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "/";
            string trimmed = path.TrimEnd('/');
            return trimmed.Length == 0 ? "/" : trimmed;
        }

        private static string AllowHeader(IServiceHttpEndpoint endpoint)
        {
            List<string> methods = endpoint.Methods.Select(m => m.ToUpperInvariant()).Distinct().ToList();
            if (methods.Contains("GET") && !methods.Contains("HEAD"))
                methods.Add("HEAD");
            return string.Join(", ", methods);
        }

        private ServiceHttpResponse NotFound(string path)
        {
            JsonWriter writer = new JsonWriter();
            writer.BeginObject();
            writer.Property("error", "No endpoint at " + path);
            writer.PropertyName("endpoints");
            writer.BeginArray();
            foreach (IServiceHttpEndpoint endpoint in Endpoints)
                writer.Value(string.Join("/", endpoint.Methods) + " " + endpoint.Path);
            writer.EndArray();
            writer.EndObject();
            return ServiceHttpResponse.Json(HttpStatusCode.NotFound, writer.ToString());
        }

        private static void Write(HttpListenerResponse response, ServiceHttpResponse result, bool headOnly)
        {
            byte[] body = result.Body ?? new byte[0];

            response.StatusCode = result.StatusCode;
            if (!string.IsNullOrEmpty(result.ContentType))
                response.ContentType = result.ContentType;
            response.ContentLength64 = body.Length;
            response.KeepAlive = false;
            response.AddHeader("Cache-Control", "no-store");

            if (result.HasHeaders)
            {
                foreach (string name in result.Headers.AllKeys)
                    response.Headers[name] = result.Headers[name];
            }

            if (!headOnly && body.Length > 0)
                response.OutputStream.Write(body, 0, body.Length);
        }
    }
}

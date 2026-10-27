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
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace Alachisoft.NCache.SocketServer.Http
{
    /// <summary>
    /// The request handed to an <see cref="IServiceHttpEndpoint"/>. Wraps the listener request so
    /// endpoints do not depend on <see cref="HttpListener"/> types.
    /// </summary>
    public sealed class ServiceHttpRequest
    {
        private readonly HttpListenerRequest _request;
        private string _body;

        internal ServiceHttpRequest(HttpListenerRequest request, string path, string pathSuffix)
        {
            _request = request;
            Path = path;
            PathSuffix = pathSuffix;
        }

        /// <summary>HTTP method in upper case, e.g. "GET". A HEAD request is presented as "GET".</summary>
        public string Method
        {
            get { return string.Equals(_request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase) ? "GET" : _request.HttpMethod.ToUpperInvariant(); }
        }

        /// <summary>True when the client sent HEAD; the server then discards the response body.</summary>
        public bool IsHead
        {
            get { return string.Equals(_request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>Normalized request path (no trailing slash), e.g. "/health".</summary>
        public string Path { get; private set; }

        /// <summary>
        /// For endpoints registered with a "/*" path: the part of the request path below the endpoint
        /// path, without a leading slash (e.g. "demoCache" for "/caches/demoCache"). Empty otherwise.
        /// </summary>
        public string PathSuffix { get; private set; }

        public Uri Url { get { return _request.Url; } }
        public NameValueCollection Query { get { return _request.QueryString; } }
        public NameValueCollection Headers { get { return _request.Headers; } }
        public IPEndPoint RemoteEndPoint { get { return _request.RemoteEndPoint; } }
        public string ContentType { get { return _request.ContentType; } }
        public long ContentLength { get { return _request.ContentLength64; } }

        /// <summary>Reads the request body as text (once; subsequent calls return the same string).</summary>
        public string ReadBodyAsString()
        {
            if (_body != null)
                return _body;

            if (!_request.HasEntityBody)
                return _body = string.Empty;

            Encoding encoding = _request.ContentEncoding ?? Encoding.UTF8;
            using (StreamReader reader = new StreamReader(_request.InputStream, encoding))
                _body = reader.ReadToEnd();

            return _body;
        }
    }
}

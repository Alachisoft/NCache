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
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace Alachisoft.NCache.SocketServer.Http
{
    /// <summary>
    /// The response an <see cref="IServiceHttpEndpoint"/> returns. Build one with the static factories.
    /// </summary>
    public sealed class ServiceHttpResponse
    {
        public const string JsonContentType = "application/json; charset=utf-8";
        public const string TextContentType = "text/plain; charset=utf-8";

        private NameValueCollection _headers;

        public ServiceHttpResponse(int statusCode, string contentType, byte[] body)
        {
            StatusCode = statusCode;
            ContentType = contentType;
            Body = body ?? new byte[0];
        }

        public int StatusCode { get; set; }
        public string ContentType { get; set; }
        public byte[] Body { get; set; }

        /// <summary>Additional response headers; created on first access.</summary>
        public NameValueCollection Headers
        {
            get { return _headers ?? (_headers = new NameValueCollection()); }
        }

        public bool HasHeaders
        {
            get { return _headers != null && _headers.Count > 0; }
        }

        public ServiceHttpResponse WithHeader(string name, string value)
        {
            Headers[name] = value;
            return this;
        }

        public static ServiceHttpResponse Json(int statusCode, string json)
        {
            return new ServiceHttpResponse(statusCode, JsonContentType, Encoding.UTF8.GetBytes(json ?? string.Empty));
        }

        public static ServiceHttpResponse Json(HttpStatusCode statusCode, string json)
        {
            return Json((int)statusCode, json);
        }

        public static ServiceHttpResponse Text(int statusCode, string text)
        {
            return new ServiceHttpResponse(statusCode, TextContentType, Encoding.UTF8.GetBytes(text ?? string.Empty));
        }

        public static ServiceHttpResponse Text(HttpStatusCode statusCode, string text)
        {
            return Text((int)statusCode, text);
        }

        public static ServiceHttpResponse Empty(int statusCode)
        {
            return new ServiceHttpResponse(statusCode, null, null);
        }

        public static ServiceHttpResponse Empty(HttpStatusCode statusCode)
        {
            return Empty((int)statusCode);
        }
    }
}

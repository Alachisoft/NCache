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
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Http
{
    /// <summary>
    /// One HTTP endpoint hosted by <see cref="ServiceHttpServer"/> inside the NCache Service.
    ///
    /// To add an endpoint: implement this interface and register an instance with
    /// <see cref="ServiceHttpServer.Register"/> where the server is built (see ServiceHost.StartServiceHttpServer).
    /// The server does the routing, method checks, HEAD handling and error responses; an endpoint only
    /// turns a <see cref="ServiceHttpRequest"/> into a <see cref="ServiceHttpResponse"/>.
    /// </summary>
    public interface IServiceHttpEndpoint
    {
        /// <summary>
        /// Absolute request path this endpoint serves, e.g. "/health". Matching is case-insensitive and
        /// ignores a trailing slash. End the path with "/*" (e.g. "/caches/*") to also receive every
        /// request below it; the remainder is then available as <see cref="ServiceHttpRequest.PathSuffix"/>.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// HTTP methods this endpoint accepts, e.g. { "GET" }. HEAD is served automatically for
        /// endpoints that accept GET; any other method gets a 405 from the server.
        /// </summary>
        IEnumerable<string> Methods { get; }

        /// <summary>
        /// Handles one request. May block; each request runs on its own thread-pool thread.
        /// Exceptions are turned into a 500 response by the server.
        /// </summary>
        ServiceHttpResponse Handle(ServiceHttpRequest request);
    }
}

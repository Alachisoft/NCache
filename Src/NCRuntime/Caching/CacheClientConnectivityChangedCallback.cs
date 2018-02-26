// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License

using System;
using System.Collections;

namespace Alachisoft.NCache.Runtime.Caching
{

    /// <summary>
    /// Defines a callback method for notifying application about cache client connectivity
    /// </summary>
    /// <param name="cacheId">Name of the cache</param>
    /// <param name="client">Client</param>
    /// <param name="status">Client status</param>
    public delegate void CacheClientConnectivityChangedCallback(string cacheId, ClientInfo client, ConnectivityStatus status);

}
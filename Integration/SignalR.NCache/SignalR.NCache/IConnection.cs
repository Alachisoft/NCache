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
// limitations under the License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.SignalR
{
    public interface ICacheProvider : IDisposable
    {
        Task ConnectAsync(string cacheName, TraceSource trace);
        
        void Close();

        Task SubscribeAsync(string _eventKey, Action<int, NCacheMessage> OnMessage);

        Task PublishAsync(string key, byte[] messageArguments);

        ulong GetUniqueID();        

        event Action<Exception> CacheStopped;

    }
}

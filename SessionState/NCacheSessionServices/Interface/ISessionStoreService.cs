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
using Microsoft.AspNetCore.Http;

namespace Alachisoft.NCache.Web.SessionState.Interface
{
    public interface ISessionStoreService: ISessionKeyManager
    {
        void CreateUninitializedItem(HttpContext context, string id, int timeOut);
        void SetAndReleaseItemExclusive(HttpContext context, string id, object items, object lockId,
            bool newItem, int timeout);
        object GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId,
            out SessionInitializationActions action);
        object GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge,
            out object lockId, out SessionInitializationActions action);
        void RemoveItem(HttpContext context, string id, object lockId);
        void ReleaseItemExclusive(HttpContext context, string id, object lockId);
        object CreateNewStoreData(HttpContext context, int timeOut);
        void LogError(string message, string sessionId);
        void LogError(Exception ex, string sessionId);
        void LogInfo(string message, string sessionId);
        void LogDebug(string message, string sessionId);
    }
}

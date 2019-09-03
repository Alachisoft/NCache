
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

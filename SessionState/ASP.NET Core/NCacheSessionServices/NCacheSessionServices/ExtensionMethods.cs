using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Extensions.Caching.Distributed;

namespace Alachisoft.NCache.Web.SessionState
{
    public static class ExtensionMethods
    {
        public static bool TryGetValue(this IDistributedCache cache, string key, out object value)
        {
            try
            {
                byte[] objectBytes = cache.Get(key);
                if (objectBytes != null)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    using (MemoryStream stream = new MemoryStream(objectBytes))
                    {
                        value = formatter.Deserialize(stream);
                        return true;
                    }
                }
                else
                {
                    value = null;
                    return false;
                }
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        public static void SetObject(this IDistributedCache cache, string key, object value, DistributedCacheEntryOptions options)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, value);
                //Remove it from cache if not retrieved in last 10 minutes
                cache.Set(
                    key,
                    stream.ToArray(),
                    new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10)));
            }
        }
    }
}

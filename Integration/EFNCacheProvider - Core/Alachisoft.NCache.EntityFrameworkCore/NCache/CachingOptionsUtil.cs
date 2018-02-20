using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Dependencies;

namespace Alachisoft.NCache.EntityFrameworkCore.NCache
{
    internal class CachingOptionsUtil
    {
        internal static void CopyMetadata(ref Alachisoft.NCache.Web.Caching.CacheItem cacheItem, CachingOptions options, CacheDependency cacheDependency = null)
        {
            Logger.Log(
                "Copying options '" + options.ToLog() + "' into cache item metadata with CacheDependency '" + cacheDependency + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );

            // Set Expiration
            if (options.ExpirationType == ExpirationType.Absolute)
            {
                cacheItem.AbsoluteExpiration = options.AbsoluteExpirationTime;
            }
            else if (options.ExpirationType == ExpirationType.Sliding)
            {
                cacheItem.SlidingExpiration = options.SlidingExpirationTime;
            }

            // Set Priority
            cacheItem.Priority = options.Priority;

            // Set Cache Dependency

            if (options.CreateDbDependency)
            {
                cacheItem.Dependency = cacheDependency;
            }

            // Set Tags
            if (options.QueryIdentifier != null)
            {
                cacheItem.Tags = new Tag[] { options.QueryIdentifier };
            }
        }

        internal static Tag[] GetTags(string[] stringTags)
        {
            Tag[] tags = new Tag[stringTags.Length];
            for (int i = 0; i < stringTags.Length; i++)
            {
                tags[i] = new Tag(stringTags[i]);
            }
            return tags;
        }
    }
}

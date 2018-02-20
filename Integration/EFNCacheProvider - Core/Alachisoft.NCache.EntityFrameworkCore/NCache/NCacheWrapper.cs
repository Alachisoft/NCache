using Alachisoft.NCache.EntityFrameworkCore.NCache;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Caching;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    internal class NCacheWrapper
    {
        Alachisoft.NCache.Web.Caching.Cache _nCache;
        private DefaultKeyGenerator _defaultKeyGen;

        internal Alachisoft.NCache.Web.Caching.Cache NCacheInstance => _nCache;

        public DefaultKeyGenerator DefaultKeyGen => _defaultKeyGen;

        public NCacheWrapper(MemoryCacheOptions options)
        {
            if (!NCacheConfiguration.IsConfigured)
                throw new Exception("NCache initialization configuration is not provided. Please use Alachisoft.NCache.EntityFrameworkCore.NCacheConfiguration.Configure() method to configure NCache before using it.");
            if (NCacheConfiguration.InitParams != null)
                _nCache = Web.Caching.NCache.InitializeCache(NCacheConfiguration.CacheId, NCacheConfiguration.InitParams);
            else
                _nCache = Web.Caching.NCache.InitializeCache(NCacheConfiguration.CacheId);

            _defaultKeyGen = new DefaultKeyGenerator();
        }

        internal CacheEntry CreateEntry(object key)
        {
            Logger.Log(
                "Creating cache entry against key '" + key + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            var cacheEntry = new CacheEntry(key, _nCache);

            return cacheEntry;
        }

        public void Insert(object key, CacheItem value)
        {
            Logger.Log(
                "Inserting item '" + value + "' against key '" + key + "'.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            _nCache.Insert(key.ToString(), value);
        }

        internal void InsertBulk(object[] keys, CacheItem[] values)
        {
            Logger.Log(
                "Inserting items in bulk against respective keys.",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            if (keys.Length > 0)
            {
                string[] strKeys = new string[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    strKeys[i] = keys[i].ToString();
                }
                IDictionary issues = _nCache.InsertBulk(strKeys, values);
                if (issues.Count > 0)
                {
                    _nCache.RemoveBulk(strKeys);

                    IDictionaryEnumerator enumerator = issues.GetEnumerator();
                    enumerator.MoveNext();
                    throw (Exception)enumerator.Entry.Value;
                }
            }
        }

        public new void Remove(object key)
        {
            Logger.Log(
                "Removing item against key '" + key + "'",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            _nCache.Remove(key.ToString());
        }

        public bool TryGetValue(object key, out object value)
        {
            Logger.Log(
                "Trying to get item against key '" + key + "'",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            value = _nCache.Get(key.ToString());
            return value != null;
        }

        public bool GetByTags(Tag tag, out Hashtable value)
        {
            Logger.Log(
                "Trying to get item against tag '" + tag + "'",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );
            Hashtable resultSet = _nCache.GetByTag(tag);
            value = resultSet;
            return resultSet.Count > 0;
        }

        public void Dispose()
        {
            _nCache = null;
        }
    }
}

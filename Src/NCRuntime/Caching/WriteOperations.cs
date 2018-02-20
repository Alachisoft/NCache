using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.Caching
#else
namespace Alachisoft.NCache.Runtime.Caching
#endif
{
    public class WriteOperations
    {
        private string _key;
        private ProviderCacheItem _cacheItem;
        private WriteOperationType _opType;
        private bool _updateInCache=false;

        public WriteOperations(){}
        public WriteOperations(string key,ProviderCacheItem cacheItem,WriteOperationType opType,bool updateInCache) 
        {
            this._key = key;
            this._cacheItem = cacheItem;
            this._opType = opType;
            this._updateInCache=updateInCache;
        }
        /// <summary>
        /// Gets/Sets the key of cache item.
        /// </summary>
        public string Key
        {
            get { return _key; }
        }
        /// <summary>
        /// Gets/Sets the cache item.
        /// </summary>
        public ProviderCacheItem ProviderCacheItem
        {
            get { return _cacheItem; }
            set { _cacheItem = value; }
        }
        /// <summary>
        /// Gets/Sets the type of Write operation.
        /// </summary>
        public WriteOperationType OperationType
        {
            get { return _opType; }
        }
        /// <summary>
        /// Gets/Sets the key of cache item.
        /// </summary>
        public bool UpdateInCache
        {
            get { return _updateInCache; }
            set { _updateInCache = value; }
        }
    }
    /// <summary>
    /// Used to log the operations type in Write behind
    /// </summary>
    public enum WriteOperationType
    {
        Add,
        Update,
        Remove,
        Clear
    }
}

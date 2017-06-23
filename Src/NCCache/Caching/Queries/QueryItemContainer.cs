using System.Collections;

namespace Alachisoft.NCache.Caching.Queries
{
    public class QueryItemContainer
    {
        private CacheEntry _item;

        public CacheEntry Item
        {
            get { return _item; }
            set { _item = value; }
        }
        private Hashtable _itemArrtribs;

        public Hashtable ItemArrtributes
        {
            get { return _itemArrtribs; }
            set { _itemArrtribs = value; }
        }

        public QueryItemContainer(CacheEntry item, Hashtable itemAttributes)
        {
            _item = item;
            _itemArrtribs = itemAttributes;
        }
    }
}

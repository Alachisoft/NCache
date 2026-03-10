using System;
using System.Globalization;

namespace Alachisoft.NCache.Common.Enum
{
    public enum StoreType
    {
        DistributedCache,
        PubSubMessaging
    }

    public class StoreTypeUtil
    {
        public const string DISTRIBUTED_CACHE = "distributed-cache";
        public const string PUB_SUB_MESSAGING = "pub/sub-messaging";

        public const string DISTRIBUTED_CACHE_DISPLAY = "distributed-cache";
        public const string PUB_SUB_MESSAGING_DISPLAY = "pub/sub-messaging";


        public static StoreType GetStore(string storeType)
        {
            if (!String.IsNullOrEmpty(storeType))
            {
                if (storeType.Equals(DISTRIBUTED_CACHE, StringComparison.InvariantCultureIgnoreCase))
                    return StoreType.DistributedCache;
                else if (storeType.Equals(PUB_SUB_MESSAGING, StringComparison.InvariantCultureIgnoreCase))
                return StoreType.PubSubMessaging;
            }
            return StoreType.DistributedCache;
        }
        public static string GetStoreDisplayName(string storeType, bool isLocal = false)
        {
            string name = DISTRIBUTED_CACHE;
            if (!String.IsNullOrEmpty(storeType))
            {
                if (storeType.Equals(DISTRIBUTED_CACHE, StringComparison.InvariantCultureIgnoreCase))
                    name = DISTRIBUTED_CACHE_DISPLAY;
                else if (storeType.Equals(PUB_SUB_MESSAGING, StringComparison.InvariantCultureIgnoreCase))
                    name = PUB_SUB_MESSAGING_DISPLAY;
            }
            if (isLocal)
            {
                name = name.Replace("distributed-cache", "local-cache");
            }
            name = ((new CultureInfo("en-US", false).TextInfo).ToTitleCase(name)).Replace('-', ' ').Replace("With", "with");
            return name;
        }

        public static string GetStore(StoreType storeType)
        {
            switch (storeType)
            {
                case StoreType.DistributedCache: return DISTRIBUTED_CACHE;
                case StoreType.PubSubMessaging: return PUB_SUB_MESSAGING;
            }
            return DISTRIBUTED_CACHE;
        }

        public static string GetSessionIdentifierString(string storeType)
        {
            StoreType store = GetStore(storeType);
            switch (store)
            {
                case StoreType.DistributedCache: return "NCache";
                case StoreType.PubSubMessaging: return "NCache";
                default: return "NCache";
            }
        }

    }
}

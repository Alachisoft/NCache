using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.CacheHealthAlerts
{
    class ResourceName
    {
        public const string MEMORY = "memory";
        public const string NETWORK = "network";
        public const string CPU = "cpu";
        public const string REQUESTPERSEC = "request-per-sec";
        public const string CLIENTCONNECTION = "client-connections";
        public const string BRIDGQQUEUE = "bridge-queue";
        public const string MIORRORQUEUEACTIVE = "mirror-queue";
        public const string MIORRORQUEUEREPLICA = "mirror-queue-replica";
        public const string WRITEBEHINDQUEUE = "write-behind-queue";
        public const string AERAGECACHEOPERATIONS = "average-cache-operation";

    }
}

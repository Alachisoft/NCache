using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Client
{
    internal static class ConnectionStrings
    {
        internal const string CLIENTREQUESTOPTIME = "opTimeOut";
        internal const string CONNECTIONTIMEOUT = "connectionTimeOut";
        internal const string CONNECTIONRETRIES = "connectionRetries";
        internal const string RETRYINTERVAL = "retryInterval";
        internal const string LOADBALANCE = "balanceNodes";
        internal const string PORT = "port";
        internal const string CACHESYNCMODE = "cacheSyncMode";
        internal const string RETRYCONNECTIONDELAY = "retryConnectionDelay"; //[KS: for connection retry delay]
        internal const string DEFAULTREADTHRUPROVIDER = "defaultReadThruProvider";
        internal const string DEFAULTWRITETHRUPROVIDER = "defaultWriteThruProvider";
        internal const string SERVERLIST = "serverlist";
        internal const string BINDIP = "bindIP";
        internal const string APPNAME = "applicationName";
        internal const string ENABLECLIENTLOGS = "enableClientLogs";
        internal const string LOGLEVEL = "logLevel";
        //internal const string ENABLEDETAILEDCLIENTLOGS = "enableDetailedClientLogs";
        internal const string ENABLEPIPELINING = "enablePipelining";
        internal const string PIPELININGTIMEOUT = "pipeliningTimeout";
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Caching.Topologies.Clustered;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// this class mantains the information of cluster members list at any time.
    /// on the basis of this members list, it decides whether a client can connect 
    /// to the cache or not.
    /// </summary>
    internal class ClientsManager
    {
        private ArrayList _activeClusterMbrs = ArrayList.Synchronized(new ArrayList());
        private ArrayList _tentativeClusterMbrs = ArrayList.Synchronized(new ArrayList());
        
        private ClusterService _cluster;

        internal ClientsManager(ClusterService cluster)
        {
            _cluster = cluster;
        }


        internal bool AcceptClient(System.Net.IPAddress clientAddress)
        {
            return false;
        }

        internal void OnMemberJoined(Address address)
        {
            if (!_activeClusterMbrs.Contains(address.IpAddress))
            {
                _activeClusterMbrs.Add(address.IpAddress);
            }
        }

        internal void OnMemberLeft(Address address)
        {
        }
    }
}

using Alachisoft.NCache.Common.Net;
using System;
using System.Text;

namespace Alachisoft.NGroups
{
    public interface MessageResponder
    {
        /// <summary>
        /// Gets the HashMaps of Distribution Manager for Data Distribution and Mirror Maps for 
        /// Dynamic Mirroring within Partitioned Replica Topology. This is required at the time of joining.
        /// </summary>
        /// <param name="data">object array containg the ArrayList of Address of nodes, 
        /// true in node joining, string subGroupID, and true if node started as Mirror.</param>
        /// <returns>Returns object array containg DistributionMap and the MirrorMapping. 
        /// First object is the DistributionMap and second is the MirrorMapping table.</returns>
        object GetDistributionAndMirrorMaps(object data);
        
    }
}

using Alachisoft.NCache.Common.DataStructures;
using System.Collections;

namespace Alachisoft.NCache.Management.Management.ModuleManagement
{
    public interface IMapMerger
    {

        bool IsMergeAllow(DistributionInfo alreadyExistedMap, ArrayList affectedNodes, string cacheName, string cacheTopology);
        DistributionInfo MapUpdate(DistributionInfo alreadyExistedMap, int clusterPort, string cacheTopology);
    }
}
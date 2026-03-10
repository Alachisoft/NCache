using Alachisoft.NCache.Common.Mirroring;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class DistributionInfo: ICompactSerializable, ICloneable
    {
        private Hashtable bucketsStats;
        private DistributionMaps _distributionMaps;
        private CacheNode[] nodes = null;
        private Hashtable partitionNodesInfo = null;
        private string _mapId;
        private string _cacheName;
        private int _mapVersion;
        private long _totalBuckets;

        public DistributionMaps DistributionMaps
        {
            get => _distributionMaps;
            set => _distributionMaps = value;
        }

        public Hashtable BucketsStats
        {
            get => bucketsStats;
            set => bucketsStats = value;
        }

        public CacheNode[] CacheNodes
        {
            get => nodes;
            set => nodes = value;
        }

        public Hashtable PartitionNodesInfo
        {
            get => partitionNodesInfo;
            set => partitionNodesInfo = value;
        }
        public int MapVersion
        {
            get => _mapVersion;
            set => _mapVersion = value;
        }

        public string MapId
        {
            get => _mapId;
            set => _mapId = value;
        }

        public string CacheName
        {
            get => _cacheName;
            set => _cacheName = value;
        }

        public long TotalBuckets
        {
            get { return _totalBuckets; }
            set { _totalBuckets = value; }
        }

        public void Deserialize(CompactReader reader)
        {
            nodes = reader.ReadObject() as CacheNode[];
            bucketsStats = reader.ReadObject() as Hashtable;
            partitionNodesInfo = reader.ReadObject() as Hashtable;
            _mapVersion = reader.ReadInt32();
            _mapId = reader.ReadString();
            _cacheName = reader.ReadString();
            _distributionMaps = reader.ReadObject() as DistributionMaps;
            _totalBuckets = reader.ReadInt64();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(nodes);
            writer.WriteObject(bucketsStats);
            writer.WriteObject(partitionNodesInfo);
            writer.Write(_mapVersion);
            writer.Write(_mapId);
            writer.Write(_cacheName);
            writer.WriteObject(_distributionMaps);
            writer.Write(_totalBuckets);
        }

        #region ICloneable Members

        public object Clone()
        {
            DistributionInfo info = new DistributionInfo();
            if(nodes!= null)  info.nodes= nodes.Clone() as CacheNode[];
            if(bucketsStats!= null) info.BucketsStats = bucketsStats.Clone() as Hashtable;
            if(partitionNodesInfo!= null) info.PartitionNodesInfo=partitionNodesInfo.Clone() as Hashtable;
            if(_distributionMaps!= null) info._distributionMaps= _distributionMaps.Clone() as DistributionMaps;
            info.MapVersion= _mapVersion;
            info.MapId = _mapId;
            info._cacheName = _cacheName;
            info.TotalBuckets = _totalBuckets;

            return info;
        }

        #endregion
    }
}


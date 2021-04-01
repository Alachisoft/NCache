//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using System;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Net;


namespace Alachisoft.NCache.Caching.Statistics
{
	/// <summary>
	/// Info class that holds statistics related to cluster.
	/// </summary>
	[Serializable]
	public class ClusterCacheStatistics : CacheStatistics, ICloneable,ICompactSerializable
	{
		/// <summary> The name of the group participating in the cluster. </summary>
		private string			_groupName;
		/// <summary> The name of the group participating in the cluster. </summary>
		private string			_channelType;
		/// <summary> The number of nodes in the cluster that are providers or consumers, i.e., group members. </summary>
		private int				_memberCount;
		/// <summary> The number of nodes in the cluster that are storage enabled. </summary>
		private int				_serverCount;
		/// <summary> The number of nodes in the cluster that are unknown, i.e., different cache scheme. </summary>
		private int				_otherCount;
		/// <summary> The statistics of the local node. </summary>
		private NodeInfo		_localNode;
		/// <summary> The statistics of individual nodes. </summary>
		private ArrayList		_nodeInfos;

        ///<summary>Data affinity of the entire cluster.</summary>
        private ArrayList    _clusterDataAffinity = new ArrayList();

		///<summary>A map that gives the list of all data groups mapped at the node.</summary>
		private Hashtable _datagroupsAtPartition = new Hashtable();

		///<summary>A map that gives the list of all nodes that have mapping for a group.</summary>
		private Hashtable _partitionsHavingDatagroup = new Hashtable();

		///<summary>A map that gives the list of all nodes belonging to the same subgroup i.e. partition.</summary>
		private Hashtable _subgroupNodes = new Hashtable();



		/// <summary>
		/// Constructor.
		/// </summary>
		public ClusterCacheStatistics()
		{}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="stat"></param>
		protected ClusterCacheStatistics(ClusterCacheStatistics stat):base(stat)
		{
			lock(stat)
			{
				this._groupName = stat._groupName;
				this._channelType = stat._channelType;
				this._memberCount = stat._memberCount;
				this._serverCount = stat._serverCount;
				this._otherCount = stat._otherCount;
				this._localNode = stat._localNode == null ? null:stat._localNode.Clone() as NodeInfo;
				if(stat._nodeInfos != null)
				{
					this._nodeInfos = new ArrayList(stat._nodeInfos.Count);
					for(int i=0; i<stat._nodeInfos.Count; i++)
					{
						this._nodeInfos.Add(((NodeInfo)stat._nodeInfos[i]).Clone() as NodeInfo);
					}
				}
                if (stat.ClusterDataAffinity != null)
                    this.ClusterDataAffinity = (ArrayList)stat.ClusterDataAffinity.Clone();

				if (stat.PartitionsHavingDatagroup != null)
					this.PartitionsHavingDatagroup = (Hashtable)stat.PartitionsHavingDatagroup.Clone();

				if (stat.DatagroupsAtPartition != null)
					this.DatagroupsAtPartition = (Hashtable)stat.DatagroupsAtPartition.Clone();

                if (stat.SubgroupNodes != null)
                    this.SubgroupNodes = (Hashtable)stat.SubgroupNodes.Clone();

			}
		}

        public override long MaxSize
        {
            get
            {
				if (LocalNode != null)
					return LocalNode.Statistics.MaxSize;
				else
					return 0;
            }
            set
            {
                if(this.LocalNode != null)
                    this.LocalNode.Statistics.MaxSize = value;
            }
        }

		/// <summary>
		/// The name of the group participating in the cluster.
		/// </summary>
		public string GroupName
		{
			get { return _groupName; }
			set { _groupName = value; }  
		}

		/// <summary>
		/// The clustering scheme.
		/// </summary>
		public string ChannelType
		{
			get { return _channelType; }
			set { _channelType = value; }  
		}

		/// <summary>
		/// The total number of nodes in the cluster.
		/// </summary>
		public int ClusterSize
		{
			get { return MemberCount + OtherCount; }
		}

		/// <summary>
		/// The number of nodes in the cluster that are providers or consumers, i.e., group members.
		/// </summary>
		public int MemberCount
		{
			get { return _memberCount; }
			set { _memberCount = value; }  
		}

		/// <summary>
		/// The number of nodes in the cluster that are storage enabled.
		/// </summary>
		public int ServerCount
		{
			get { return _serverCount; }
			set { _serverCount = value; }  
		}

		/// <summary>
		/// The number of nodes in the cluster that are unknown, i.e., different cache scheme.
		/// </summary>
		public int OtherCount
		{
			get { return _otherCount; }
			set { _otherCount = value; }  
		}

		/// <summary>
		/// The statistics of the local node.
		/// </summary>
		public NodeInfo LocalNode
		{
			get { return _localNode; }
			set { _localNode = value; }  
		}

		/// <summary>
		/// The statistics of the local node.
		/// </summary>
		public ArrayList Nodes
		{
			get { return _nodeInfos; }
			set { _nodeInfos = value; }  
		}

        /// <summary>
        /// Gets/Sets teh data affinity of the cluster.
        /// </summary>
        public ArrayList ClusterDataAffinity
        {
            get { return _clusterDataAffinity; }
            set { _clusterDataAffinity = value; } 
        }

		public Hashtable DatagroupsAtPartition
		{
			get { return _datagroupsAtPartition; }
			set { _datagroupsAtPartition = value; }
		}

		public Hashtable PartitionsHavingDatagroup
		{
			get { return _partitionsHavingDatagroup; }
			set { _partitionsHavingDatagroup = value; }
		}

		public Hashtable SubgroupNodes
		{
			get { return _subgroupNodes; }
			set { _subgroupNodes = value; }
		}

		#region	/                 --- ICloneable ---           /

		/// <summary>
		/// Creates a new object that is a copy of the current instance.
		/// </summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		public override object Clone()
		{
            return new ClusterCacheStatistics(this);
		}

		#endregion

		/// <summary>
		/// Adds a new node to the node list
		/// </summary>
		/// <param name="info"></param>
		protected internal NodeInfo GetNode(Address address)
		{
			lock(this)
			{
				if(_nodeInfos != null)
				for(int i=0 ;i<_nodeInfos.Count; i++)
				{
					if(((NodeInfo)_nodeInfos[i]).Address.CompareTo(address) == 0)
						return _nodeInfos[i] as NodeInfo;
				}
			}
			return null;
		}

	    internal NodeInfo[] GetNodesClone()
	    {
	        NodeInfo[] nodes = null;

            lock (this)
	        {
	            if (_nodeInfos != null)
	            {
	                nodes = new NodeInfo[_nodeInfos.Count];
                    _nodeInfos.CopyTo(nodes);
	            }
	        }
	        return nodes;

	    }
		
		/// <summary>
		/// Sets the values of the server/member/other counts
		/// </summary>
		/// <param name="server"></param>
		/// <param name="member"></param>
		/// <param name="other"></param>
		protected internal void SetServerCounts(int servCnt, int memCnt, int otherCnt)
		{
			lock(this)
			{
				_serverCount = servCnt;
				_memberCount = memCnt;
				_otherCount = otherCnt;
			}
		}

		/// <summary>
		/// returns the string representation of the statistics.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			lock(this)
			{
				System.Text.StringBuilder ret = new System.Text.StringBuilder();
				ret.Append("Cluster[" + base.ToString() + ", Nm:" + GroupName + ", ");
				ret.Append("S:" + ServerCount.ToString() + ", ");
				ret.Append("M:" + MemberCount.ToString() + ", ");
				ret.Append("O:" + OtherCount.ToString() + ", ");
				if(_localNode != null)
					ret.Append("Local" + _localNode.ToString());
				foreach(NodeInfo i in _nodeInfos)
				{
					ret.Append(", " + i.ToString());
				}
				ret.Append("]");
				return ret.ToString();
			}
		}

		#region	/                 --- ICompactSerializable ---           /

       public override void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            _groupName = reader.ReadObject() as string;
            _channelType = reader.ReadObject() as string;
            _memberCount = reader.ReadInt32();
            _serverCount = reader.ReadInt32();
            _otherCount = reader.ReadInt32();
            _localNode = NodeInfo.ReadNodeInfo(reader);
            _nodeInfos = (ArrayList)reader.ReadObject();
        }

        public override void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.WriteObject(_groupName);
            writer.WriteObject(_channelType);
            writer.Write(_memberCount);
            writer.Write(_serverCount);
            writer.Write(_otherCount);
            NodeInfo.WriteNodeInfo(writer, _localNode);
            writer.WriteObject(_nodeInfos);
        }

		#endregion
	}
}



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
using System.Collections.Generic;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections.Concurrent;

namespace Alachisoft.NCache.Caching.Statistics
{
    /// <summary>
    /// The class that contains information specific to a single node in the cluster.
    /// Contains the address as well as statistics of the local cache.
    /// </summary>
    [Serializable]
    public class NodeInfo : ICloneable, IComparable, ICompactSerializable
    {
        /// <summary> The IP address, port tuple;  uniquely identifying the node. </summary>
        private Address _address;

        /// <summary> The name of the sub-cluster this node belongs to. </summary>
        private string _subgroupName;

        /// <summary> The statistics of the node. </summary>
        public CacheStatistics _stats;

        /// <summary> Up status of node. </summary>
        private BitSet _status = new BitSet();

        /// <summary>Data groups associated with this node</summary>
        private DataAffinity _dataAffinity;

        private ArrayList _connectedClients = ArrayList.Synchronized(new ArrayList());

        /// <summary>Client/Server address of the node.</summary>
        private Address _rendererAddress;

        /// <summary>For sequencing stats replication, we will keep node guid to identify if node is restarted</summary>
        private string _nodeGuid = System.Guid.NewGuid().ToString();

        /// <summary>For sequencing stats replication </summary>
        private int _statsReplicationCounter = 0;

        private bool _isInproc;

        private bool _isStartedAsMirror;

        private ArrayList _localConnectedClientsInfo = ArrayList.Synchronized(new ArrayList());
        /// <summary>
        /// Constructor.
        /// </summary>
        public NodeInfo()
        {
        }

        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="address"></param>
        public NodeInfo(Address address)
        {
            _address = address;
        }

        public NodeInfo(Address address, bool isStartedAsMirror)
        {
            _address = address;
            _isStartedAsMirror = isStartedAsMirror;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="info"></param>
        protected NodeInfo(NodeInfo info)
        {
            this._address = info._address == null ? null : info._address.Clone() as Address;
            this._rendererAddress = info._rendererAddress != null ? info._rendererAddress.Clone() as Address : null;
            this._stats = info._stats == null ? null : info._stats.Clone() as CacheStatistics;
            this._status = info._status;
            this._subgroupName = info._subgroupName;
            this._isInproc = info._isInproc;
            this._dataAffinity = info._dataAffinity == null ? null : info._dataAffinity.Clone() as DataAffinity;
            _isStartedAsMirror = info.IsStartedAsMirror;
            this._nodeGuid = info._nodeGuid;
            _statsReplicationCounter = info._statsReplicationCounter;
            this.CacheNodeStatus = info.CacheNodeStatus;
            if (info._connectedClients != null)
            {
                lock (info._connectedClients.SyncRoot)
                {
                    this._connectedClients = info._connectedClients.Clone() as ArrayList;
                    this._localConnectedClientsInfo = info._localConnectedClientsInfo.Clone() as ArrayList;

                }
            }
        }

        /// <summary>
        /// The IP address, port tuple;  uniquely identifying the node.
        /// </summary>
        public Address Address
        {
            get { return _address; }
            set { _address = value; }
        }

       

        public Address RendererAddress
        {
            get { return _rendererAddress; }
            set { _rendererAddress = value; }
        }

        public bool IsStartedAsMirror
        {
            get { return _isStartedAsMirror; }
            set { _isStartedAsMirror = value; }
        }

        /// <summary>
        /// Gets/sets the status of the node whether a node is InProc or OutProc.
        /// </summary>
        public bool IsInproc
        {
            get { return _isInproc; }
            set { _isInproc = value; }
        }

        public string NodeGuid
        {
            get { return _nodeGuid; }
            set { _nodeGuid = value; }
        }

        public int StatsReplicationCounter
        {
            get { return _statsReplicationCounter; }
            set { _statsReplicationCounter = value; }
        }

        /// <summary> 
        /// The name of the sub-cluster this node belongs to. 
        /// </summary>
        public string SubgroupName
        {
            get { return _subgroupName; }
            set { _subgroupName = value; }
        }

        /// <summary> 
        /// The data groups settings for the node. 
        /// </summary>
        public DataAffinity DataAffinity
        {
            get { return _dataAffinity; }
            set { _dataAffinity = value; }
        }

        /// <summary>
        /// The number of nodes in the cluster that are providers or consumers, i.e., group members.
        /// </summary>
        public CacheStatistics Statistics
        {
            get { return _stats; }
            set { _stats = value; }
        }

        /// <summary>
        /// The runtime status of node.
        /// </summary>
        internal BitSet Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public ArrayList ConnectedClients
        {
            get { return _connectedClients; }
            set { _connectedClients = value; }
        }

        public ArrayList OldConnectedClientsInfo
        {
            get { return _localConnectedClientsInfo; }
            set { _localConnectedClientsInfo = value; }
        }
        /// <summary>
        /// Get/Set the value indicating whether this node is active or not.
        /// This property is valid only for Mirror Cache Topology.
        /// </summary>
        public bool IsActive
        {
            get { return Status.IsAnyBitSet(NodeStatus.Coordinator); }
        }

        #region	/                 --- IComparable ---           /

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>A 32-bit signed integer that indicates the relative order of the comparands.</returns>
        public int CompareTo(object obj)
        {
            return _address.CompareTo(((NodeInfo) obj).Address);
        }

        #endregion

        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        #region	/                 --- ICloneable ---           /

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public object Clone()
        {
            return new NodeInfo(this);
        }

        #endregion

        /// <summary>
        /// returns the string representation of the statistics.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            System.Text.StringBuilder ret = new System.Text.StringBuilder();
            try
            {
                ret.Append("Node[Adr:" + _address);
                if (_stats != null)
                    ret.Append(", " + _stats);
                ret.Append("]");
            }
            catch (Exception e)
            {
            }
            return ret.ToString();

        }

        #region	/                 --- ICompactSerializable ---           /

        public void Deserialize(CompactReader reader)
        {
            _address = Address.ReadAddress(reader);
            _subgroupName = reader.ReadObject() as string;
            _stats = CacheStatistics.ReadCacheStatistics(reader);
            _status = reader.ReadObject() as BitSet;
            _dataAffinity = DataAffinity.ReadDataAffinity(reader);
            _connectedClients = (ArrayList) reader.ReadObject();
            _isInproc = reader.ReadBoolean();
            _rendererAddress = Address.ReadAddress(reader);
            _isStartedAsMirror = reader.ReadBoolean();
            _nodeGuid = reader.ReadObject() as string;
            _statsReplicationCounter = reader.ReadInt32();
            this.CacheNodeStatus =(Alachisoft.NCache.Common.Monitoring.CacheNodeStatus) reader.ReadByte();
            _localConnectedClientsInfo = (ArrayList)reader.ReadObject();

        }

        public void Serialize(CompactWriter writer)
        {
            Address.WriteAddress(writer, _address);
            writer.WriteObject(_subgroupName);
            CacheStatistics.WriteCacheStatistics(writer, _stats);
            writer.WriteObject(_status);
            DataAffinity.WriteDataAffinity(writer, _dataAffinity);
            writer.WriteObject(_connectedClients);
            writer.Write(_isInproc);
            Address.WriteAddress(writer, _rendererAddress);
            writer.Write(_isStartedAsMirror);
            writer.WriteObject(_nodeGuid);
            writer.Write(_statsReplicationCounter);
            writer.Write((byte)CacheNodeStatus);
            writer.WriteObject(_localConnectedClientsInfo);

        }

        #endregion

        public static NodeInfo ReadNodeInfo(CompactReader reader)
        {
            byte isNull = reader.ReadByte();
            if (isNull == 1)
                return null;
            NodeInfo newInfo = new NodeInfo();
            newInfo.Deserialize(reader);
            return newInfo;
        }

        public static void WriteNodeInfo(CompactWriter writer, NodeInfo nodeInfo)
        {
            byte isNull = 1;
            if (nodeInfo == null)
                writer.Write(isNull);
            else
            {
                isNull = 0;
                writer.Write(isNull);
                nodeInfo.Serialize(writer);
            }
            return;
        }
        
        public Common.Monitoring.CacheNodeStatus CacheNodeStatus { get; set; }
    }
}
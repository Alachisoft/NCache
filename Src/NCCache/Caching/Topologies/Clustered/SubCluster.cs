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
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NGroups.Util;
using Alachisoft.NGroups.Blocks;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    internal class SubCluster
    {
        /// <summary> identifier of group </summary>
        private string _groupid;

        private ClusterService _service;

        //private NewTrace nTrace;
        //private string loggerName;
        private ILogger _ncacheLog;
        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        /// <summary> keeps track of all group members </summary>
        protected ArrayList _members = ArrayList.Synchronized(new ArrayList(11));

        /// <summary> keeps track of all server members </summary>
        protected ArrayList _servers = ArrayList.Synchronized(new ArrayList(11));


        /// <summary> The hashtable that contains members and their info. </summary>
        public ArrayList Members { get { return _members; } }

        /// <summary> The hashtable that contains members and their info. </summary>
        public ArrayList Servers { get { return _servers; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gpid"></param>
        public SubCluster(string gpid, ClusterService service)
        {
            _groupid = gpid;
            _service = service;
            _ncacheLog = _service.NCacheLog;
           
        }

        /// <summary>
        /// Returns name of the current sub-cluster.
        /// </summary>
        public string Name
        {
            get { return _groupid; }
        }

        /// <summary>
        /// returns true if the node is operating in coordinator mode. 
        /// </summary>
        public bool IsCoordinator
        {
            get
            {
                Address address = Coordinator;
                if (address != null && _service.LocalAddress.CompareTo(address) == 0)
                    return true;
                return false;
            }
        }

        /// <summary>
        /// check if the given address exists in this cluster
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool IsMember(Address node)
        {
            if (Members.Contains(node) || _servers.Contains(node))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// returns the coordinator in the cluster. 
        /// </summary>
        public Address Coordinator
        {
            get
            {
                lock (_servers.SyncRoot)
                {
                    if (_servers.Count > 0)
                        return _servers[0] as Address;
                }
                return null;
            }
        }

        /// <summary>
        /// Called when a new member joins the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <param name="identity">additional identity information</param>
        /// <returns>true if the node joined successfuly</returns>
        internal virtual int OnMemberJoined(Address address, NodeIdentity identity)
        {
            if (identity.SubGroupName != _groupid)
            {
                return -1;
            }

            NCacheLog.Warn("SubCluster.OnMemberJoined()",   "Memeber " + address + " added to sub-cluster " + _groupid);

            _members.Add(address);
            if (identity.HasStorage && !identity.IsStartedAsMirror)
                _servers.Add(address);
            return _members.Count;
        }

        /// <summary>
        /// Called when an existing member leaves the group.
        /// </summary>
        /// <param name="address">address of the joining member</param>
        /// <returns>true if the node left successfuly</returns>
        internal virtual int OnMemberLeft(Address address, Hashtable bucketsOwnershipMap)
        {
            if (_members.Contains(address))
            {
                NCacheLog.Warn("SubCluster.OnMemberJoined()",   "Memeber " + address + " left sub-cluster " + _groupid);
                _members.Remove(address);
                _servers.Remove(address);
                return _members.Count;
            }
            return -1;
        }

        public override bool Equals(object obj)
        {
            bool result = false;

            if (obj is SubCluster)
            {
                SubCluster other = obj as SubCluster;
                result = this._groupid.CompareTo(other._groupid) == 0;
                
                if (result)
                {
                    result = this._members.Count == other._members.Count;
                    
                    if (result)
                    {
                        foreach (Address mbr in this._members)
                        {
                            if (!other._members.Contains(mbr))
                            {
                                result = false;
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        #region	/                 --- Messages ---           /

        /// <summary>
        /// Sends message to every member of the group. Returns its reponse as well.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="mode"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public RspList BroadcastMessage(object msg, byte mode, long timeout)
        {
            return _service.BroadcastToMultiple(_members, msg, mode, timeout);
        }

       

        /// <summary>
        /// Send a broadcast no reply message to a specific partition
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="part"></param>
        public void BroadcastNoReplyMessage(Object msg)
        {
            _service.BroadcastToMultiple(_members, msg, GroupRequest.GET_NONE, -1);
        }

        #endregion
    }
}
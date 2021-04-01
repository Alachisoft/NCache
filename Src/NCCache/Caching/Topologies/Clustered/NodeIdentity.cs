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
using System.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// An info object that is passed as identity of the members, i.e., additional data with the
    /// Address object. This will help the partition determine legitimate members as well as
    /// gather useful information about member configuration. Load balancer might be a good
    /// consumer of this information.
    /// </summary>
    [Serializable]
    internal class NodeIdentity : ICompactSerializable
    {
        /// <summary> Up status of node. </summary>
        private string _groupname;
        /// <summary> Up status of node. </summary>        

        private int _rendererPort = -1;
        private IPAddress _rendererAddress;

        /// <summary>
        /// True if this cache instance is started as mirror cache. otherwise false.
        /// </summary>
        private bool _isStartedAsMirror = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hasStorage"></param>
        public NodeIdentity(bool hasStorage, int renderPort, IPAddress renderAddress)
        {
            HasStorage = hasStorage;
            _rendererPort = renderPort;
            _rendererAddress = renderAddress;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hasStorage"></param>
        public NodeIdentity(bool hasStorage, int renderPort, IPAddress renderAddress, bool isStartedAsMirror)
        {
            HasStorage = hasStorage;
            _rendererPort = renderPort;
            _rendererAddress = renderAddress;
            _isStartedAsMirror = isStartedAsMirror;
        }

        /// <summary>
        /// The number of backup caches configured with this instance.
        /// </summary>
        public bool HasStorage
        {
            //There was an old concept of stroage nodes and non storage nodes that is removed now
            set { }
            get { return true; }

        }

        /// <summary>
        /// Gets or sets the cache renderer port.
        /// </summary>
        public int RendererPort
        {
            get { return _rendererPort; }
        }

        public IPAddress RendererAddress
        {
            get { return _rendererAddress; }
        }

        public string SubGroupName
        {
            get { return _groupname; }
            set { _groupname = value; }
        }

        /// <summary>
        /// Get or Sets the value indicating weather this instance started as Mirror or not.
        /// True if started as mirror otherwise false.
        /// </summary>
        public bool IsStartedAsMirror { get { return _isStartedAsMirror; } set { _isStartedAsMirror = value; } }

        #region	/                 --- ICompactSerializable ---           /

        public void Deserialize(CompactReader reader)
        {
            _groupname = reader.ReadObject() as string;
            _rendererPort = reader.ReadInt32();
            //TODO: NETCORE (IPAddress is not serialize)
#if NETCORE
            string ipAddress = reader.ReadObject() as String;
            if (ipAddress == null)
                _rendererAddress = null;
            else
                _rendererAddress = IPAddress.Parse(ipAddress);
#elif !NETCORE
            _rendererAddress =  reader.ReadObject() as IPAddress;
#endif
            _isStartedAsMirror = reader.ReadBoolean();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_groupname);
            writer.Write(_rendererPort);
            //TODO: NETCORE (IPAddress is not serialize)
#if NETCORE
            writer.WriteObject(_rendererAddress == null? null : _rendererAddress.ToString());
#elif !NETCORE
             writer.WriteObject(_rendererAddress);
#endif
            writer.Write(_isStartedAsMirror);
        }

#endregion

        public override string ToString()
        {
            return "NodeIdentity: (" + _groupname + ", " 
                   + (IsStartedAsMirror? "IsMirror":"IsActive")
                   + ", " + (HasStorage? "HasStorage":"NoStorage");
        }
    }
}
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
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.Monitoring
{
	[Serializable]
	public class ServerNode :Node, ICloneable, ICompactSerializable
	{
		private bool _isRepica;
		private bool _inProcInstance;
		private int _clientPort;
        private string _nodeAt;
        

		public ServerNode()
		{
		}
		public ServerNode(string name, Address address):base(name, address)
		{
		}

		/// <summary>
		/// Gets/Sets the identity of the node in case of Partitioned-of-replica.
		/// </summary>
		public bool IsReplica
		{
			get { return _isRepica; }
			set { _isRepica = value; }
		}

		/// <summary>
		/// Gets/Sets the status of the node whether it is running as
		/// inproc or outproc.
		/// </summary>
		public bool InProcInstance
		{
			get { return _inProcInstance; }
			set { _inProcInstance = value;  }
		}

		/// <summary>
		/// Gets/Sets the socket server port of this node.
		/// </summary>
		public int ClientPort
		{
			get { return _clientPort; }
			set { _clientPort = value; }
		}

        /// <summary>
        /// Gets/Sets node ip address where replica residing.
        /// </summary>
        public string NodeAt
        {
            get { return _nodeAt; }
            set { _nodeAt = value; }
        }

		#region ICloneable Members

		public object Clone()
		{
			ServerNode node = new ServerNode();
			node.InProcInstance = this.InProcInstance;
			node.IsReplica = this.IsReplica;
            node.NodeAt = this.NodeAt;
			node.Name = this.Name;
			node.Address = this.Address != null ? (Address)this.Address : null;

			return node;
		}

        #endregion

        #region ICompactSerializable Members

        public new void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _isRepica = reader.ReadBoolean();
            _inProcInstance = reader.ReadBoolean();
            _clientPort = reader.ReadInt32();
            _nodeAt = reader.ReadObject() as string;
            base.Deserialize(reader);
        }

        public new void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_isRepica);
            writer.Write(_inProcInstance);
            writer.Write(_clientPort);
            writer.WriteObject(_nodeAt);
            base.Serialize(writer);
        }

        #endregion
    }
}

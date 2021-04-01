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
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class ClientNode: Node ,ICompactSerializable 
    {
        private string _clientId;
        private RtContextValue _clientContext;
        /// <summary>
        /// Gets/Sets the client-id. Each client connecting to
        /// a cache server has unique id. If client opens multiple
        /// connections with the server, this id remains some.
        /// </summary>
        public string ClientID
        {
            get { return _clientId; }
            set { _clientId = value; }
        }
        public RtContextValue ClientContext
        {
            get { return _clientContext; }
            set { _clientContext = value; }
        }
        public override bool Equals(object obj)
        {
            ClientNode other = obj as ClientNode;
            if (other != null)
            {
                if (other.ClientID == ClientID)
                    return true;
            }
            return false;   
        }

        #region ICompactSerializable Members

        public new void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _clientId = reader.ReadObject() as string;
            _clientContext = reader.ReadObject() as string == "1" ? RtContextValue.JVCACHE : RtContextValue.NCACHE;
            base.Deserialize(reader);
        }

        public new void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_clientId);
            writer.WriteObject(_clientContext== RtContextValue.JVCACHE ? "1" : "0");
            base.Serialize(writer);
        }

        #endregion
    }
}

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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class ClientNode : ICloneable,ICompactSerializable
    {
        string name;
        ClientNodeStatus status;
        RtContextValue clientRuntimeContext= RtContextValue.NCACHE;
        
        public ClientNodeStatus Status
        {
            get { return status; }
            set { status = value; }
        }

        [ConfigurationAttribute("ip")]//Changes for New Dom from name
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        
        public RtContextValue ClientRuntimeContext
        {
            get { return clientRuntimeContext; }
            set { clientRuntimeContext = value; }
        }
    
        public string RuntimeContextString
        {
            get
            {
                string contextString = string.Empty;
                
                switch (clientRuntimeContext)
                {
                    case RtContextValue.JVCACHE:
                        contextString = "JVCACHE";
                        break;
                    case RtContextValue.NCACHE:
                        contextString = "NCACHE";
                        break;
                }

                return contextString;
            }
            set
            {
                switch (value)
                {
                    case "NCACHE":
                        clientRuntimeContext = RtContextValue.NCACHE;
                        break;
                    case "JVCACHE":
                        clientRuntimeContext = RtContextValue.JVCACHE;
                        break;
                }
            }        
        }

        public override int GetHashCode()
        {
            if (name == null) return String.Empty.GetHashCode(); 
                return name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ClientNode)
            {
                ClientNode clientNode = obj as ClientNode;
                if (clientNode.name != null)
                {
                    return clientNode.name.ToLower().CompareTo(name.ToLower()) == 0 && clientNode.ClientRuntimeContext == clientRuntimeContext;
                }
                else
                    return false;
            }

            return false;
        }

        #region ICloneable Members

        public object Clone()
        {
            ClientNode node = new ClientNode();
            node.name = name;
            node.status = status;
            node.clientRuntimeContext = clientRuntimeContext;
            return node;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            name = reader.ReadObject() as string;
            Status = (ClientNodeStatus)reader.ReadInt32();
            clientRuntimeContext = reader.ReadObject() as string == "1" ? RtContextValue.JVCACHE : RtContextValue.NCACHE;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(name);
            writer.Write((int)status);
            writer.WriteObject(clientRuntimeContext == RtContextValue.JVCACHE ? "1" : "0");
        }

        #endregion
    }
}

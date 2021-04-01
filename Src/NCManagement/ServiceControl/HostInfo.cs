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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common;
using UnixServiceController;

namespace Alachisoft.NCache.Management.ServiceControl
{
    [Serializable]
    [ConfigurationRoot("hosts")]
    public class HostInfo : Runtime.Serialization.ICompactSerializable
    {
        private static Dictionary<string, NodeOSInfo> s_hostInfoMap;        
        private NodeOSInfo[] _nodeinfo;

        static HostInfo()        
        {
            s_hostInfoMap = new Dictionary<string, NodeOSInfo>();
        }

        public HostInfo()
        {

        }

        public static void Clear()
        {
            s_hostInfoMap.Clear();            
        }

        #region Instance Methods
        public static void AddHostInfo(string nodeIP, NodeOSInfo nodeInfo)
        {
            s_hostInfoMap[nodeIP] = nodeInfo; 
        }

        public Dictionary<string, NodeOSInfo> HostInfoMap
        {
            get { return s_hostInfoMap; }
        }

        public void UpdateHostInfo(string nodeIP, NodeOSInfo nodeInfo)
        {
            s_hostInfoMap[nodeIP] = nodeInfo;   
        }

        public static NodeOSInfo GetHostInfo(string nodeIP)
        {
            NodeOSInfo nodeInfo;
            if (s_hostInfoMap.TryGetValue(nodeIP, out nodeInfo))
            {
                return s_hostInfoMap[nodeIP];
            }
            else
            {                
                OSInfo osInfo = OSDetector.DetectOS(nodeIP);
                
                if (OSDetector.hostIP == null)
                    return null;

                var detectedIP = OSDetector.hostIP;
                if (detectedIP != null)
                    nodeIP = detectedIP;

                if (osInfo == OSInfo.Linux)
                {
                    nodeInfo = new NodeOSInfo(nodeIP, OSInfo.Linux);
                    AddHostInfo(nodeIP, nodeInfo);
                    return nodeInfo;
                }
                else if (osInfo == OSInfo.Windows)
                {
                    nodeInfo = new NodeOSInfo(nodeIP, OSInfo.Windows);
                    AddHostInfo(nodeIP, nodeInfo);
                    return nodeInfo;
                }
                else
                {
                    nodeInfo = new NodeOSInfo(nodeIP, OSInfo.Unknown);
                    AddHostInfo(nodeIP, nodeInfo);
                    return nodeInfo;
                }
            }
        }
        #endregion        

        [ConfigurationSection("host-info")]
        public NodeOSInfo[] NodeInfo
        {
            get { return _nodeinfo; }
            set { _nodeinfo = value; } 
        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            s_hostInfoMap = reader.ReadObject() as Dictionary<string, NodeOSInfo>; //Common.Util.SerilizationUtility.DeserializeDictionary<string, NodeOSInfo>(reader);
         
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                int length = reader.ReadInt32();
                _nodeinfo = new NodeOSInfo[length];

                for (int i = 0; i < length; i++)
                {
                    _nodeinfo[i] = (NodeOSInfo)reader.ReadObject();
                }
            }
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {

            writer.WriteObject(s_hostInfoMap);

            if (_nodeinfo != null)
            {
                writer.Write(true);
                writer.Write(_nodeinfo.Length);

                for (int i = 0; i < _nodeinfo.Length; i++)
                {
                    writer.WriteObject(_nodeinfo[i]);
                }
            }

            else
            {
                writer.Write(false);
            }
        } 
        #endregion
    }
}

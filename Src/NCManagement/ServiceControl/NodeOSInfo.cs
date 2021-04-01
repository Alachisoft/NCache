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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using UnixServiceController;

namespace Alachisoft.NCache.Management.ServiceControl
{
    [Serializable]
    public class NodeOSInfo : Runtime.Serialization.ICompactSerializable
    {
        private string _hostIp;
        private OSInfo _hostOS;
        private string _hostOSString;

        public NodeOSInfo() { }

        public NodeOSInfo(string hostIP, OSInfo hostOS)
        {
            _hostIp = hostIP;
            _hostOS = hostOS;
        }

        [ConfigurationAttribute("host-ip")]
        public string HostIP
        {
            get { return _hostIp; }
            set { _hostIp = value; }
        }
        
        public OSInfo HostOS
        {
            get { return _hostOS; }
            set { _hostOS = value; }
        }

        [ConfigurationAttribute("host-os")]
        public string HostOSString
        {
            get 
            {
                string _hostOSValue = string.Empty;
                switch (HostOS)
                {
                    case OSInfo.Linux:
                        _hostOSString = "Linux";
                        break;
                    case OSInfo.Windows:
                        _hostOSString = "Windows";
                        break;
                    case OSInfo.Unknown:
                        _hostOSString = "Unknown";
                        break;                
                }
                return _hostOSString;
            }
            set
            {
                switch (value)
                {
                    case "Linux":
                        HostOS = OSInfo.Linux;
                        break;
                    case "Windows":
                        HostOS = OSInfo.Windows;
                        break;
                    case "UnKnown":
                        HostOS = OSInfo.Unknown;
                        break;
                }
            }
        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _hostIp = (string)reader.ReadObject();
            _hostOSString = (string)reader.ReadObject();
            _hostOS =(OSInfo) reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_hostIp);
            writer.WriteObject(_hostOSString);
            writer.WriteObject(_hostOS);

        } 
        #endregion
    }
}
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
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config
{
    [Serializable]
    public class Mapping : ICloneable, ICompactSerializable
    {
        private string _privateIp;
        private int _privatePort;
        private string _publicIP;
        private int _publicPort;

       

        [ConfigurationAttribute("public-ip")]
        public string PublicIP
        {
            get { return _publicIP; }
            set { _publicIP = value; }
        }

        [ConfigurationAttribute("public-port")]
        public int PublicPort
        {
            get { return _publicPort; }
            set { _publicPort = value; }
        }
        [ConfigurationAttribute("private-ip")]
        public string PrivateIP
        {
            get { return _privateIp; }
            set { _privateIp = value; }
        }

        [ConfigurationAttribute("private-port")]
        public int PrivatePort
        {
            get { return _privatePort; }
            set { _privatePort = value; }
        }
     

        #region ICloneable Members

        public object Clone()
        {
            Mapping map = new Mapping();
            map.PrivateIP = _privateIp;
            map.PrivatePort = _privatePort;
            map.PublicIP = _publicIP;
            map.PublicPort = _publicPort;

            return map;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _privateIp=reader.ReadObject()as string;
            _privatePort=reader.ReadInt32();
            _publicIP=reader.ReadObject()as string;
            _publicPort=reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(this._privateIp);
            writer.Write(this._privatePort);
            writer.WriteObject(this._publicIP);
            writer.Write(this._publicPort);          
        }

        #endregion
    }
}

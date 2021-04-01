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
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class PerfCounters: ICloneable,ICompactSerializable
    {
        private int snmpPort = 0;
        bool enabled = true;

        public PerfCounters() { }

        [ConfigurationAttribute("enable-counters")]//Changes for New Dom from enabled
        public bool Enabled
        {
            get { return enabled; }
            set { /*enabled = value;*/ }
        }

        [ConfigurationAttribute("snmp-port")]
        public int SnmpPort
        {
            get { return snmpPort; }
            set { snmpPort = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            PerfCounters perfCounters = new PerfCounters();
            perfCounters.Enabled = Enabled;
            perfCounters.SnmpPort = SnmpPort;
            return perfCounters;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            enabled = reader.ReadBoolean();
            SnmpPort = reader.ReadInt32();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(enabled);
            writer.Write(SnmpPort);
        }

        #endregion
    }
}

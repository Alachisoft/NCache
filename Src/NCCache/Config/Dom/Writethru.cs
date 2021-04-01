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
using Alachisoft.NCache.Config.Dom;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Writethru: ICloneable,ICompactSerializable
    {
        bool enabled;
        
        Provider[] provider;
        WriteBehind writeBehind;

        public Writethru() { }

        

        [ConfigurationSection("provider")]
        public Provider[] Providers
        {
            get { return provider; }
            set { provider = value; }
        }

        [ConfigurationAttribute("enable-write-thru")]
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        [ConfigurationSection("write-behind")]
        public WriteBehind WriteBehind
        {
            get { return writeBehind; }
            set { writeBehind = value; }
        }
        #region ICloneable Members

        public object Clone()
        {
            Writethru writethru = new Writethru();
            writethru.enabled = enabled;
            writethru.Providers = Providers != null ? Providers.Clone() as Provider[]: null;
            writethru.WriteBehind = WriteBehind != null ? WriteBehind.Clone() as WriteBehind : null;
            return writethru;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            provider = reader.ReadObject() as Provider[];
            enabled = reader.ReadBoolean();
            writeBehind = reader.ReadObject() as WriteBehind;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(provider);
            writer.Write(enabled);
            writer.WriteObject(writeBehind);
        }

        #endregion
    }
}

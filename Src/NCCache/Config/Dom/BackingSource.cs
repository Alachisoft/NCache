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
    public class BackingSource: ICloneable,ICompactSerializable
    {
        Readthru readthru;
        Writethru writehtru;

        public BackingSource() { }

        [ConfigurationSection("read-thru")]
        public Readthru Readthru
        {
            get { return readthru; }
            set { readthru = value; }
        }

        [ConfigurationSection("write-thru")]
        public Writethru Writethru
        {
            get { return writehtru; }
            set { writehtru = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            BackingSource store = new BackingSource();
            store.Readthru = Readthru != null ? (Readthru) Readthru.Clone(): null;
            store.Writethru = Writethru != null ? (Writethru) Writethru.Clone(): null;
            return store;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            readthru = reader.ReadObject() as Readthru;
            writehtru = reader.ReadObject() as Writethru;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(readthru);
            writer.WriteObject(writehtru);
        }

        #endregion
    }
}

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
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class AttributeListUnion : ICloneable,ICompactSerializable
    {
        List<PortableAttribute> portableAttributeList;

        public AttributeListUnion()
        {
        }

        [ConfigurationSection("attribute")]
        public PortableAttribute[] PortableAttributes
        {
            get
            {
                if (portableAttributeList != null)
                    return portableAttributeList.ToArray();
                return null;
            }
            set
            {
                if (portableAttributeList == null)
                    portableAttributeList = new List<PortableAttribute>();

                portableAttributeList.Clear();
                if (value != null)
                {
                    portableAttributeList.AddRange(value);
                }
            }
        }

        #region ICloneable Members

        public object Clone()
        {
            AttributeListUnion attributeList = new AttributeListUnion();
            attributeList.PortableAttributes = PortableAttributes != null ? (PortableAttribute[])PortableAttributes.Clone() : null;
            return attributeList;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            PortableAttributes = reader.ReadObject() as PortableAttribute[];
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(PortableAttributes);
        }

        #endregion
    }
}

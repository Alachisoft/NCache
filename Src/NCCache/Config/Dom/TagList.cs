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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class TagList : ICloneable, ICompactSerializable
    {
        ArrayList nodetags;

        public TagList ()
        {
            nodetags = new ArrayList();
        }

       [ConfigurationSection("hint")]
        public LoaderTag[] DistributionHints
        {
            get { 
                LoaderTag[] loaderTags = new LoaderTag[nodetags.Count];
                nodetags.CopyTo(loaderTags,0);
                return loaderTags;
            }
            set
            {
                nodetags.Clear();
                foreach (LoaderTag tag in value)
                {
                    nodetags.Add(tag);
                }
            }
        }

       #region ICompact Serializable members

       public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
       {
           if (this.nodetags==null)
           {
               this.nodetags = new ArrayList();
           }
           this.nodetags = reader.ReadObject() as ArrayList;
       }

       public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
       {
           writer.WriteObject(this.nodetags);
  
       }

       #endregion

       #region ICloneable Members
       public object Clone()
       {
           TagList list = new TagList();

           list.DistributionHints = DistributionHints.Clone() as LoaderTag[];

           return list;
       }
       #endregion
    }
}

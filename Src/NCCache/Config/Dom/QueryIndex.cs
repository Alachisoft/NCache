// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class QueryIndex : ICloneable,ICompactSerializable
    {
        Class[] classes;

        public QueryIndex() { }

        [ConfigurationSection("query-class")]//Changes for New Dom from class
        public Class[] Classes
        {
            get {  return classes; }
            set { classes = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            QueryIndex indexes = new QueryIndex();
            //indexes.IndexForAll = IndexForAll;
            indexes.Classes = Classes != null ? (Class[])Classes.Clone(): null;
            return indexes;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            classes = reader.ReadObject() as Class[];
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(classes);
        }

        #endregion
    }
}

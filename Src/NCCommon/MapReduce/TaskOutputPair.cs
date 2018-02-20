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
// limitations under the License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.MapReduce
{
    public class TaskOutputPair : ICompactSerializable
    {
        private object key = null;
        private object value = null;

        public object Key
        {
            get { return key; }
            set { key = value; }
        }
        public object Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        public TaskOutputPair() { }

        public TaskOutputPair(object key, object value)
        {
            this.key = key;
            this.value = value;
        }

        #region ICompactSerializable Methods

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this.Key = reader.ReadObject();
            this.Value = reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(this.Key);
            writer.WriteObject(this.Value);
        }

        #endregion
    }
}

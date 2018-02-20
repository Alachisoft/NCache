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
using System.Collections;

namespace Alachisoft.NCache.Common.MapReduce
{
    public class TaskEnumeratorResult : ICompactSerializable
    {
        private TaskEnumeratorPointer pointer = null;
 
        private DictionaryEntry recordSet = new DictionaryEntry();
        private string nodeAddress = null;
        private bool isLastResult;

        public TaskEnumeratorResult() { }

        public TaskEnumeratorPointer Pointer
        {
            get { return pointer; }
            set { pointer = value; }
        }
        public DictionaryEntry RecordSet
        {
            get { return recordSet; }
            set { recordSet = value; }
        }
        public string NodeAddress
        {
            get { return nodeAddress; }
            set { nodeAddress = value; }
        }
        public bool IsLastResult
        {
            get { return isLastResult; }
            set { isLastResult = value; }
        }

        #region ICompactSerializable Methods

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this.Pointer = (TaskEnumeratorPointer) reader.ReadObject();
            this.RecordSet = (DictionaryEntry)reader.ReadObject();
            this.NodeAddress= reader.ReadString();
            this.IsLastResult = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(this.Pointer);
            writer.WriteObject(this.RecordSet);
            writer.Write(this.NodeAddress);
            writer.Write(this.IsLastResult);
        }

        #endregion
    }
}

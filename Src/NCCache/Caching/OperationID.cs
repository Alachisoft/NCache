// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class OperationID :ICompactSerializable
    {
        
        private string _opID;
        private long _opCounter;
        public OperationID()
        {
        }
        public OperationID(String opID, long opCounter)
        {
            _opID = opID;
            _opCounter = opCounter;
        }
        public string OperationId
        {
            get { return _opID; }
            set { _opID = value; }
        }
        public long OpCounter
        {
            get { return _opCounter; }
            set { _opCounter = value; }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this._opCounter = reader.ReadInt64();
            this._opID = (string)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(this._opCounter);
            writer.WriteObject(this._opID);
        }
    }
}

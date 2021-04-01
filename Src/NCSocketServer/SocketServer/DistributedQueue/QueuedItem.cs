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
//using System.Linq;
using System.Text;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.SocketServer
{
    [Serializable]
    class QueuedItem:Runtime.Serialization.ICompactSerializable
    {
        private string _slaveId;
        private long _count;
        private object _item;
        private string _registeredClientId;

        public string SlaveId
        {
            get { return _slaveId; }
            set { _slaveId = value; }
        }

        public long Count
        {
            get { return _count; }
            set { _count = value; }
        }

        public object Item
        {
            get { return _item; }
            set { _item = value; }
        }

        public string RegisteredClientId
        {
            get { return _registeredClientId; }
            set { _registeredClientId = value; }
        }

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _slaveId=(string) reader.ReadObject();
            _count = reader.ReadInt64();
            _item = reader.ReadObject();
            _registeredClientId = (string)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_slaveId);
            writer.Write(_count);
            writer.WriteObject(_item);
            writer.WriteObject(_registeredClientId);
        }
        #endregion
    }
}

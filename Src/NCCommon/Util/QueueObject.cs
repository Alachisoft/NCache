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

namespace Alachisoft.NCache.Common.Util
{
    [Serializable]
    public class QueueObject : Runtime.Serialization.ICompactSerializable
    {
        private string _key;
        private string _command;
        private long _index;

        public QueueObject(string key, string command)
        {
            _key = key;
            _command = command;
        }

        public long Index
        {
            get { return _index; }
            set { _index = value; }
        }

        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public string Command
        {
            get { return _command; }
            set { _command = value; }
        }

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _key = (string)reader.ReadObject();
            _command = (string)reader.ReadObject();
            _index = reader.ReadInt64();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_key);
            writer.WriteObject(_command);
            writer.Write(_index);
        } 
        #endregion
    }
}

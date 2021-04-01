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
using Alachisoft.NCache.Runtime.Serialization;
using System;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class Parameters : ICompactSerializable
    {
        string _name;
        string _type;
        int _sequenceNumber;

        public string ParameterName
        {
            get { return _name; }
            set { _name = value; }
        }

        public string ParameterType
        {
            get { return _type; }
            set { _type = value; }
        }

        public int ParameterSequence
        {
            get { return _sequenceNumber; }
            set { _sequenceNumber = value; }
        }

        #region ICompact Serializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _sequenceNumber = (int)reader.ReadObject();
            _name = (string)reader.ReadObject();
            _type =  (string)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_sequenceNumber);
            writer.WriteObject(_name);
            writer.WriteObject(_type);
        }
        #endregion

    }
}

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

namespace Alachisoft.NCache.Common.DataStructures
{
    [Serializable]
    public class DomainInfo : Runtime.Serialization.ICompactSerializable
    {
        private string _domainName;
        private ArrayList _users;
        private Hashtable _distiguishNames;

        public DomainInfo()
        { }

        public string DomainName
        {
            set { _domainName = value; }
            get { return _domainName; }
        }

        public ArrayList Users
        {
            set { _users = value; }
            get { return _users; }
        }

        public Hashtable DistiguishNames
        {
            set { _distiguishNames = value; }
            get { return _distiguishNames; }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _domainName = (string)reader.ReadObject();
            _distiguishNames = (Hashtable)reader.ReadObject();
            _users = (ArrayList)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_domainName);
            writer.WriteObject(_distiguishNames);
            writer.WriteObject(_users);
           
        }
    }
}

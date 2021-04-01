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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;
namespace Alachisoft.NCache.Persistence
{
    [Serializable]
    public class EventInfo :ICompactSerializable
    {

        string _key;
        List<string> _clientIds = new List<string>();
        object _value;
        BitSet _flag;
        ItemRemoveReason _reason;
        System.Collections.ArrayList _cbInfoList;
       
        public ArrayList CallBackInfoList
        {
            get
            {
                return _cbInfoList;
            }
            set
            {
                _cbInfoList = value;
            }
        }
  
        public string Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }

        public List<string> ClientIds
        {
            get
            {
                return _clientIds;
            }
            set
            {
                _clientIds = value;
            }
        }

        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public BitSet Flag
        {
            get
            {
                return _flag;
            }
            set
            {
                _flag = value;
            }
        }

        public ItemRemoveReason Reason
        {
            get
            {
                return _reason;
            }
            set
            {
                _reason = value;
            }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _cbInfoList = (ArrayList)reader.ReadObject();
            _clientIds = (List<string>)reader.ReadObject();
            _flag = (BitSet)reader.ReadObject();
            _key = (string)reader.ReadObject();
            _reason = (ItemRemoveReason)reader.ReadObject();
            _value = (ArrayList)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_cbInfoList);
            writer.WriteObject(_clientIds);
            writer.WriteObject(_flag);
            writer.WriteObject(_key);
            writer.Write((int)_reason);
            writer.WriteObject(_value);
        }
    }
}

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
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class KeyValuesContainer
    {
        public KeyValuesContainer()
        {
            _valuesDictionary = new Dictionary<string, object>();
        }

        string _key;
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        //Hashtable containing attribute-value pair of current key
        Dictionary<string, object> _valuesDictionary;
        public Dictionary<string, object> Values
        {
            get { return _valuesDictionary; }
        }

        public int Count
        {
            get { return _valuesDictionary.Count; }
        }
    }
}
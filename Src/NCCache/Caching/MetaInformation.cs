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
using System.Text;
using System.Collections;
using Alachisoft.NCache.Common.Util;


namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Contains an object's attribute names and corresponding values. This class is used to retrieve attribute
    /// values if required during query execution because we cannot retrieve these values from actual object that
    /// that is stored in cache in binary form.
    /// </summary>
    [Serializable]
    public class MetaInformation
    {
        private Hashtable _attributeValues;
        private string _cacheKey;
        private string _type;

        internal MetaInformation(Hashtable attributeValues)
        {
            _attributeValues = attributeValues;
        }

        public string CacheKey
        {
            get { return _cacheKey; }
            set { _cacheKey = value; }
        }

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public object this[string key]
        {
            get 
            {
                return _attributeValues == null ? null : _attributeValues[key];
            }

            set 
            {
                if (_attributeValues == null)
                    _attributeValues = new Hashtable();

                _attributeValues[key] = value;
            }
        }

        public override bool Equals(object obj)
        {
            bool result = false;
            MetaInformation other = obj as MetaInformation;
            
            if (other != null)
                result = this.CacheKey.Equals(other.CacheKey);

            return result;
        }

        public override int GetHashCode()
        {
            return this.CacheKey.GetHashCode();
        }

        public bool IsAttributeIndexed(string attribName)
        {
            if(_attributeValues.ContainsKey(attribName))
            {
                return true;
            }
            return false;
        }

        public void Add(Hashtable attributeValues)
        {
            foreach (DictionaryEntry entry in attributeValues)
            {
                _attributeValues.Add(entry.Key, entry.Value);
            }
        }

        public Hashtable AttributeValues
        {
            get
            {
                if (_attributeValues != null)
                {
                    Hashtable result = new Hashtable();
                    
                    foreach (DictionaryEntry entry in _attributeValues)
                    {
                        if (entry.Value is String)
                            result.Add(entry.Key, ((string)entry.Value).ToLower());
                        else
                            result.Add(entry.Key, entry.Value);
                    }

                    return result;
                }

                return _attributeValues;
            }
        }
    }
}

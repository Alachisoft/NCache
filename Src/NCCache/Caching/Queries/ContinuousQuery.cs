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
// limitations under the License.

using System;
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization;
namespace Alachisoft.NCache.Caching.Queries
{
    [Serializable]
    public class ContinuousQuery : ICompactSerializable
    {
        private string _cmdText;
        private IDictionary _attributeValues;
       
         
        private string _uniqueId;

        public string UniqueId
        {
            get { return _uniqueId; }
            set { _uniqueId = value; }
        }

        public ContinuousQuery(string commandText, IDictionary attributeValues)
        {
            _cmdText = commandText;
            _attributeValues = attributeValues;
        }

        internal string CommandText
        {
            get { return _cmdText; }
            set { _cmdText = value; }
        }

        internal IDictionary AttributeValues
        {
            get { return _attributeValues; }
            set { _attributeValues = value; }
        }
        
        public override bool Equals(object obj)
        {
            bool equal = false;
            ContinuousQuery other = obj as ContinuousQuery;

            if (other != null)
            {
                equal = _cmdText.Replace(" ", String.Empty) == other.CommandText.Replace(" ", String.Empty);

                if (equal)
                {
                    equal = _attributeValues.Count == other.AttributeValues.Count;

                    if (equal)
                    {
                        IDictionaryEnumerator ide = _attributeValues.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            object key = ide.Key;
                            object value = ide.Value;

                            if (!other.AttributeValues.Contains(key))
                            {
                                equal = false;
                                break;
                            }
                            else if (value is ArrayList && other.AttributeValues[key] is ArrayList)
                            {
                                ArrayList vals = (ArrayList)value;
                                ArrayList otherVals = (ArrayList)other.AttributeValues[key];
                                foreach (object item in vals)
                                {
                                    if (!otherVals.Contains(item))
                                    {
                                        equal = false;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (!(value.Equals(other.AttributeValues[key])))
                                {
                                    equal = false;
                                    break;
                                }
                            }
                        }

                        if (equal)
                        {
                            if (!String.IsNullOrEmpty(this.UniqueId) && !String.IsNullOrEmpty(other.UniqueId))
                            {
                                equal = this.UniqueId.Equals(other.UniqueId);
                            }
                        }
                    }
                }
            }

            return equal;
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _cmdText =(string) reader.ReadObject();
            
            _attributeValues = (IDictionary)reader.ReadObject();
            _uniqueId = (string)reader.ReadObject();
        }

        void ICompactSerializable.Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_cmdText);

            writer.WriteObject(_attributeValues);
            writer.WriteObject(UniqueId);
        }

        #endregion
    }

}

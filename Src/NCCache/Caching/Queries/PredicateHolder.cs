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
using Alachisoft.NCache.Caching.Queries.Filters;
using System.Collections;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Parser;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Caching.Queries
{
    [Serializable]
    internal class PredicateHolder: Runtime.Serialization.ICompactSerializable /*: ICompactSerializable*/
    {
        private string _cmdText;
        private string _objectType;
        private string _queryId;
        private IDictionary _attributeValues;

        // this member is not serialzable.
        [NonSerialized]
        private Predicate _predicate;

        /// <summary>
        /// Initializes the predicate from command text.
        /// </summary>
        internal void Initialize(ILogger NCacheLog)
        {
    
            ParserHelper parser = new ParserHelper(NCacheLog);

            if (_predicate == null)
            {
                if (parser.Parse(_cmdText) == ParseMessage.Accept)
                {
                    Reduction reduction = parser.CurrentReduction;
                    _predicate = reduction.Tag as Predicate;
                }
            }
        }

        internal string CommandText
        {
            get { return _cmdText; }
            set { _cmdText = value; }
        }

        internal string ObjectType
        {
            get { return _objectType; }
            set { _objectType = value; }
        }

        internal Predicate Predicate
        {
            get { return _predicate; }
            set { _predicate = value; }
        }

        internal IDictionary AttributeValues
        {
            get 
            {
                return MiscUtil.DeepClone(_attributeValues); 
            }
            set { _attributeValues = value; }
        }

        internal string QueryId
        {
            get { return _queryId; }
            set { _queryId = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _cmdText =(string) reader.ReadObject();
            _objectType = (string) reader.ReadObject();
            _queryId = (string) reader.ReadObject();
            _attributeValues =(IDictionary) reader.ReadObject();
            
            
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_cmdText);
            writer.WriteObject(_objectType);
            writer.WriteObject(_queryId);
            writer.WriteObject(_attributeValues);
            
        }
         

        #endregion


        public override bool Equals(object obj)
        {
            PredicateHolder other = obj as PredicateHolder;
            if (other != null)
            {
                if (this.QueryId.Equals(other.QueryId))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.DataStructures.Clustered;
namespace Alachisoft.NCache.Caching.Queries.Continuous
{
    [Serializable]
    public class ContinuousQueryStateInfo : ICompactSerializable
    {
        private bool _isPartial;
        private HashVector _typeSpecificPredicates;
        private ClusteredList<string> _registeredTypes;
        private ContinuousQueryManagerState _cqManagerState;
        private HashVector _typeSpecificRegisteredPredicates;
        private HashVector _typeSpecificEvalIndexes; 

        internal bool IsPartial
        {
            get { return _isPartial; }
            set { _isPartial = value; }
        }
        
        internal HashVector TypeSpecificPredicates
        {
            get { return _typeSpecificPredicates; }
            set { _typeSpecificPredicates = value; }
        }

        internal HashVector TypeSpecificRegisteredPredicates
        {
            get { return _typeSpecificRegisteredPredicates; }
            set { _typeSpecificRegisteredPredicates = value; }
        }

        internal HashVector TypeSpecificEvalIndexes
        {
            get { return _typeSpecificEvalIndexes; }
            set { _typeSpecificEvalIndexes = value; }
        }

        internal ClusteredList<string> RegisteredTypes
        {
            get { return _registeredTypes; }
            set { _registeredTypes = value; }
        }

        internal ContinuousQueryManagerState CQManagerState
        {
            get { return _cqManagerState; }
            set { _cqManagerState = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _isPartial = reader.ReadBoolean();
            _typeSpecificPredicates = reader.ReadObject() as HashVector;
            _registeredTypes = reader.ReadObject() as ClusteredList<string>;
            _cqManagerState = reader.ReadObject() as ContinuousQueryManagerState;
            _typeSpecificRegisteredPredicates = reader.ReadObject() as HashVector;
            _typeSpecificEvalIndexes = reader.ReadObject() as HashVector;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(_isPartial);
            writer.WriteObject(_typeSpecificPredicates);
            writer.WriteObject(_registeredTypes);
            writer.WriteObject(_cqManagerState);
            writer.WriteObject(_typeSpecificRegisteredPredicates);
            writer.WriteObject(_typeSpecificEvalIndexes);
        }

        #endregion
    }
}

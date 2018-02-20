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
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Queries
{
    [Serializable]
    internal class ContinuousQueryManagerState : ICompactSerializable
    {
        private ClusteredList<ContinuousQuery> _registeredQueries;
        private HashVector _clientRefs;
        private HashVector _addNotifications;
        private HashVector _updateNotifications;
        private HashVector _removeNotifications;

        /// <summary>
        /// Will keep datafilters in the following order
        /// QueryID -> clientID -> dataFilters
        /// 
        /// Keeps the max datafilter to be raised
        /// </summary>
        private HashVector maxAddDFAgainstCID;
        private HashVector maxUpdateDFAgainstCID;
        private HashVector maxRemoveDFAgainstCID;

        /// <summary>
        /// Will keep updated clientUID to be updated at unregistration
        /// ClientID -> ClientQueryUniqueID -> DataFilter
        /// </summary>
        private HashVector addDFAgainstCUniqueID;
        private HashVector updateDFAgainstCUniqueID;
        private HashVector removeDFAgainstCUniqueID;

        internal ClusteredList<ContinuousQuery> RegisteredQueries
        {
            get { return _registeredQueries; }
            set { _registeredQueries = value; }
        }

        internal HashVector ClientRefs
        {
            get { return _clientRefs; }
            set { _clientRefs = value; }
        }

        internal HashVector AddNotifications
        {
            get { return _addNotifications; }
            set { _addNotifications = value; }
        }

        internal HashVector UpdateNotifications
        {
            get { return _updateNotifications; }
            set { _updateNotifications = value; }
        }

        internal HashVector RemoveNotifications
        {
            get { return _removeNotifications; }
            set { _removeNotifications = value; }
        }


        internal HashVector MaxAddDFAgainstCID
        {
            get { return maxAddDFAgainstCID; }
            set { maxAddDFAgainstCID = value; }
        }

        internal HashVector MaxUpdateDFAgainstCID
        {
            get { return maxUpdateDFAgainstCID; }
            set { maxUpdateDFAgainstCID = value; }
        }

        internal HashVector MaxRemoveDFAgainstCID
        {
            get { return maxRemoveDFAgainstCID; }
            set { maxRemoveDFAgainstCID = value; }
        }

        internal HashVector AddDFAgainstCUID
        {
            get { return addDFAgainstCUniqueID; }
            set { addDFAgainstCUniqueID = value; }
        }

        internal HashVector UpdateDFAgainstCUID
        {
            get { return updateDFAgainstCUniqueID; }
            set { updateDFAgainstCUniqueID = value; }
        }

        internal HashVector RemoveDFAgainstCUID
        {
            get { return removeDFAgainstCUniqueID; }
            set { removeDFAgainstCUniqueID = value; }
        }

        #region ICompactSerializable Members
        
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _registeredQueries=Common.Util.SerializationUtility.DeserializeClusteredList<ContinuousQuery>(reader);
            _clientRefs = reader.ReadObject() as HashVector;

            _addNotifications = reader.ReadObject() as HashVector;
            _updateNotifications = reader.ReadObject() as HashVector;
            _removeNotifications = reader.ReadObject() as HashVector;

            maxAddDFAgainstCID = reader.ReadObject() as HashVector;
            maxUpdateDFAgainstCID = reader.ReadObject() as HashVector;
            maxRemoveDFAgainstCID = reader.ReadObject() as HashVector;

            addDFAgainstCUniqueID = reader.ReadObject() as HashVector;
            updateDFAgainstCUniqueID = reader.ReadObject() as HashVector;
            removeDFAgainstCUniqueID = reader.ReadObject() as HashVector;
           
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
       
            Common.Util.SerializationUtility.SerializeClusteredList<ContinuousQuery>(_registeredQueries, writer);
            writer.WriteObject(_clientRefs);

            writer.WriteObject(_addNotifications);
            writer.WriteObject(_updateNotifications);
            writer.WriteObject(_removeNotifications);

            writer.WriteObject(maxAddDFAgainstCID);
            writer.WriteObject(maxUpdateDFAgainstCID);
            writer.WriteObject(maxRemoveDFAgainstCID);
            
            writer.WriteObject(addDFAgainstCUniqueID);
            writer.WriteObject(updateDFAgainstCUniqueID);
            writer.WriteObject(removeDFAgainstCUniqueID);
        }
        
        #endregion
    }
}
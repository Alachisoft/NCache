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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary>
    /// Represents the actual status of the state transfer.
    /// </summary>
    public class StateTransferInfo : ICompactSerializable
    {
        private StateTransferStatus _stateTransferStatus = StateTransferStatus.NO_NEED_FOR_STATE_TRANSFER; //Changed the default status to No NEED for state transfer becoz only coordinator source cache will do state transfer
        private ArrayList _keyList;
        private object _syncRoot = new object();

        /// <summary>
        /// Gets/sets the status of the state transfer.
        /// </summary>
        public StateTransferStatus Status
        {
            get { return _stateTransferStatus; }
            set { _stateTransferStatus = value; }
        }

        /// <summary>
        /// Gets the synchronization object.
        /// </summary>
        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        /// <summary>
        /// Gets the list of keys for the items which are not transfered yet.
        /// </summary>
        public ArrayList TransferableKeys
        {
            get { return _keyList; }
            set { _keyList = value; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _stateTransferStatus = (StateTransferStatus)reader.ReadByte();
            _keyList = reader.ReadObject() as ArrayList;
            _syncRoot = new object();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write((byte)_stateTransferStatus);
            writer.WriteObject(_keyList);
        }

        #endregion
    }
}
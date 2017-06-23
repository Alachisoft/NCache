// Copyright (c) 2017 Alachisoft
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
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.DataStructures;
using System.Net;
using Alachisoft.NCache.Common.DataReader;
using Alachisoft.NCache.Common.Queries;

namespace Alachisoft.NCache.Common.DataReader
{
    public class ReaderResultSet : ICompactSerializable
    {
        private string _readerId = null;
        private IRecordSet _recordSet = null;
        private string _nodeAddress = null;
        private int _chunkSize = 0;
        private bool _getData = true;
        private List<OrderByArgument> _orderbyArguments = null;
        private bool _isGrouped = false;
        private string _clientID = null;
        private int _nextIndex = 0;

        public ReaderResultSet() { }
        public IRecordSet RecordSet
        {
            get { return _recordSet; }
            set { _recordSet = value; }
        }

        public string NodeAddress
        {
            get { return _nodeAddress; }
            set { _nodeAddress = value; }
        }

        public string ReaderID
        {
            get { return _readerId; }
            set { _readerId = value; }
        }

        public string ClientID
        {
            get { return _clientID; }
            set { _clientID = value; }
        }

        public int ChunkSize
        {
            get { return _chunkSize; }
            set { _chunkSize = value; }
        }
        public bool GetData
        {
            get { return _getData; }
            set { _getData = value; }
        }
        

        public List<OrderByArgument> OrderByArguments
        {
            get { return _orderbyArguments; }
            set { _orderbyArguments = value; }
        }

        public bool IsGrouped
        {
            get { return _isGrouped; }
            set { _isGrouped = value; }
        }
        public int NextIndex
        {
            get { return _nextIndex; }
            set { _nextIndex = value; }
        }

        #region---------------ICompactSerializable------------------------
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _readerId = reader.ReadObject() as string;
            _recordSet = reader.ReadObject() as IRecordSet;
            _nodeAddress = reader.ReadObject() as string;
            _nextIndex = reader.ReadInt32();
            _orderbyArguments = (List<OrderByArgument>)reader.ReadObject();
            if (_orderbyArguments != null)
            {
                int noOfArgs = reader.ReadInt32();
                _orderbyArguments = new List<OrderByArgument>();
                for (int i = 0; i < noOfArgs; i++)
                {
                    _orderbyArguments.Add(reader.ReadObject() as OrderByArgument);
                }
            }
            _isGrouped = reader.ReadBoolean();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_readerId);
            writer.WriteObject(_recordSet);
            writer.WriteObject(_nodeAddress);
            writer.Write(_nextIndex);
            writer.WriteObject(_orderbyArguments);
            if (_orderbyArguments != null)
            {
                writer.Write(_orderbyArguments.Count);
                foreach (OrderByArgument oba in _orderbyArguments)
                {
                    writer.WriteObject(oba);
                }
            }
            writer.Write(_isGrouped);
        }
        #endregion
    }
}

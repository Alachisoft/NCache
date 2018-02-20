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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    internal class SequencedReplicationOperation
    {
        Address _source;
        Address _destination;
        ulong _sequenceId;
        long _viewId;
        List<ReplicationOperation> _operations;
        IList _opCodes;
        IList _infos;
        Array _userPayloads;
        IList _compilationInfos;
        OperationContext _operationContext;

        public SequencedReplicationOperation(ulong sequenceId)
        {
            _sequenceId = sequenceId;
            _operations = new List<ReplicationOperation>();
        }

        public ulong SequenceId
        {
            get { return _sequenceId; }
        }

        public IList OpCodes
        {
            get { return _opCodes; }
            set { _opCodes = value; }
        }

        public IList Infos
        {
            get { return _infos; }
            set { _infos = value; }
        }

        public Array UserPayload
        {
            get { return _userPayloads; }
            set { _userPayloads = value; }
        }

        public IList CompilationInfo
        {
            get { return _compilationInfos; }
            set { _compilationInfos = value; }
        }

        public Address Source
        {
            get { return _source; }
            set { _source = value; }
        }

        public Address Destination
        {
            get { return _destination; }
            set { _destination = value; }
        }

        public long ViewId
        {
            get { return _viewId; }
            set { _viewId = value; }
        }

        public OperationContext OperationContext
        {
            get { return _operationContext; }
            set { _operationContext = value; }
        }

        public void Add(ReplicationOperation operation)
        {
            _operations.Add(operation);
        }

        public void Compile(out IList opCodes, out IList info, out IList userPayload, out IList compilationInfo)
        {

            ClusteredArrayList payLoad = new ClusteredArrayList();
            IList opCodesToBeReplicated = new ClusteredArrayList();
            IList infoToBeReplicated = new ClusteredArrayList();
            compilationInfo = new ClusteredArrayList();            

            foreach (ReplicationOperation operation in _operations)
            {
                DictionaryEntry entry = (DictionaryEntry)operation.Data;
                opCodesToBeReplicated.Add(entry.Key);
                infoToBeReplicated.Add(entry.Value);

                if (operation.UserPayLoad != null)
                {
                    if(payLoad==null) payLoad = new ClusteredArrayList();
                    for (int j = 0; j < operation.UserPayLoad.Length; j++)
                    {
                        payLoad.Add(operation.UserPayLoad.GetValue(j));
                    }
                }
                compilationInfo.Add(operation.PayLoadSize);                
            }

            userPayload = payLoad;
            opCodes = opCodesToBeReplicated;
            info = infoToBeReplicated;
        }
    }
}
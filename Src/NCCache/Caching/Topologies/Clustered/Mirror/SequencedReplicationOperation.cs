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
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    internal class SequencedReplicationOperation
    {
        Address _source;
        Address _destination;
        ulong _sequenceId;
        long _viewId;
        List<ReplicationOperation> _operations;
        Array _opCodes;
        Array _infos;
        Array _userPayloads;
        ArrayList _compilationInfos;
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

        public Array OpCodes
        {
            get { return _opCodes; }
            set { _opCodes = value; }
        }

        public Array Infos
        {
            get { return _infos; }
            set { _infos = value; }
        }

        public Array UserPayload
        {
            get { return _userPayloads; }
            set { _userPayloads = value; }
        }

        public ArrayList CompilationInfo
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

        public void Compile(out Array opCodes, out Array info, out Array userPayload, out ArrayList compilationInfo)
        {
            ArrayList opCodesToBeReplicated = new ArrayList();
            ArrayList infoToBeReplicated = new ArrayList();
            ArrayList payLoad = new ArrayList();
            compilationInfo = new ArrayList();            

            foreach (ReplicationOperation operation in _operations)
            {
                DictionaryEntry entry = (DictionaryEntry)operation.Data;
                opCodesToBeReplicated.Add(entry.Key);
                infoToBeReplicated.Add(entry.Value);

                if (operation.UserPayLoad != null)
                {
                    for (int j = 0; j < operation.UserPayLoad.Length; j++)
                    {
                        payLoad.Add(operation.UserPayLoad.GetValue(j));
                    }
                }
                compilationInfo.Add(operation.PayLoadSize);                
            }

            opCodes = opCodesToBeReplicated.ToArray();
            info = infoToBeReplicated.ToArray();
            userPayload = payLoad.ToArray();
        }
    }
}

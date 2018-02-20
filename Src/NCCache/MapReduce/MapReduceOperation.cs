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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.MapReduce.Notifications;

namespace Alachisoft.NCache.MapReduce
{
    public class MapReduceOperation : ICompactSerializable
    {
        private MapReduceOpCodes _opCode = MapReduceOpCodes.SubmitMapReduceTask;
        private string _taskId = "";
        private object _data = null;
        private Filter _filter = null;
        private Address _source;
        private long _sequenceId;
        private OperationContext _operationContext;
        private TaskCallbackInfo _callbackInfo;

        public MapReduceOperation() { }


        public MapReduceOpCodes OpCode
        {
            get { return _opCode; }
            set { _opCode = value; }
        }

        public string TaskID
        {
            get { return _taskId; }
            set { _taskId = value; }
        }

        public object Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public Filter Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public Address Source
        {
            get { return _source; }
            set { _source = value; }
        }

        public long SequenceID
        {
            get { return _sequenceId; }
            set { _sequenceId = value; }
        }

        public OperationContext OperationContext
        {
            get { return _operationContext; }
            set { _operationContext = value; }
        }

        public TaskCallbackInfo CallbackInfo
        {
            get { return _callbackInfo; }
            set { _callbackInfo = value; }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _opCode = (MapReduceOpCodes)reader.ReadObject();
            _taskId = reader.ReadString();
            _data = reader.ReadObject();
            _source = (Address)reader.ReadObject();
            _sequenceId = reader.ReadInt64();
            _operationContext = (OperationContext)reader.ReadObject();
            _callbackInfo = (TaskCallbackInfo)reader.ReadObject();
            _filter = (Filter)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_opCode);
            writer.Write(_taskId);
            writer.WriteObject(_data);
            writer.WriteObject(_source);
            writer.Write(_sequenceId);
            writer.WriteObject(_operationContext);
            writer.WriteObject(_callbackInfo);
            writer.WriteObject(_filter);
        }
    }
}

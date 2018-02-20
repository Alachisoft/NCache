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
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    internal class DSWriteBehindOperation : DSWriteOperation, IComparable,ICompactSerializable
    {
        private DateTime enqueueTime; //enqueue time
        private WriteBehindAsyncProcessor.OperationState operationState= new WriteBehindAsyncProcessor.OperationState();
        /// <summary>task id</summary>
        private string _taskId;
        /// <summary></summary>
        private string _source;
        private long _delayInterval;
        /// <summary></summary>
        private WriteBehindAsyncProcessor.TaskState _state=new WriteBehindAsyncProcessor.TaskState();

        private OperationResult.Status _dsOpState=new OperationResult.Status();

        private Exception _exception;

        public DSWriteBehindOperation(CacheRuntimeContext context, Object key,  CacheEntry entry, OpCode opcode, string providerName, long operationDelay, string taskId, string source, WriteBehindAsyncProcessor.TaskState taskState) :
            base(context, key, entry, opcode, providerName)
        {
            this._taskId = taskId;
            this._state = taskState;
            this._source = source;
            this._delayInterval = operationDelay;

        }

        public WriteBehindAsyncProcessor.OperationState OperationState
        {
            get { return this.operationState; }
            set { this.operationState = value; }
        }

        public DateTime EnqueueTime
        {
            get { return this.enqueueTime; }
            set { this.enqueueTime = value; }
        }

        public long OperationDelay
        {
            set { this._delayInterval = value; }
        }
        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }

        public string TaskId
        {
            get { return _taskId; }
            set { _taskId = value; }
        }

        public WriteBehindAsyncProcessor.TaskState State
        {
            get { return _state; }
            set { _state = value; }
        }
        public long Size
        {
            get
            {
                if (this._entry != null) return this._entry.Size;
                return 0;
            }
        }
        internal OperationResult.Status DSOpState
        {
            get { return this._dsOpState; }
            set { this._dsOpState = value; }
        }

        internal Exception Exception
        {
            get { return this._exception; }
            set { this._exception = value; }
        }
      
        public bool OperationDelayExpired
        {
            get 
            {
                DateTime expireTime= this.enqueueTime.AddMilliseconds(this._delayInterval);
                if (expireTime <= DateTime.Now)
                    return true;
                return false;
            }
        }


        #region IComparable Members

        public int CompareTo(object obj)
        {
            DSWriteBehindOperation dsOp = (DSWriteBehindOperation)obj;
            return this._retryCount.CompareTo(dsOp._retryCount);
        }

        #endregion

        public void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            enqueueTime = reader.ReadDateTime();
            operationState = (WriteBehindAsyncProcessor.OperationState)reader.ReadInt32();
            _taskId = reader.ReadObject() as string;
            _source = reader.ReadObject() as string;
            _delayInterval = reader.ReadInt64();
            _state = (WriteBehindAsyncProcessor.TaskState)reader.ReadByte();
        }

        public void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(enqueueTime);
            writer.Write((int)operationState);
            writer.WriteObject(_taskId);
            writer.WriteObject(_source);
            writer.Write(_delayInterval);
            writer.Write((byte)_state);
        }

        
    }
}
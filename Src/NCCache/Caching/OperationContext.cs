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

using System;
using System.Threading;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// make it serializable coz cache operations performed through remoting will fail 
    /// otherwise.
    /// </summary>
    [Serializable]
    public class OperationContext : SimpleLease, ICompactSerializable, ICloneable
    {
        private static readonly string s_operationUniqueID;
        private static readonly OperationID s_operationID;

        private static readonly int _fieldCount = Enum.GetValues(typeof(OperationContextFieldName)).Length;

        private static long s_operationCounter;
        private object[] _fieldValueTable = new object[_fieldCount];
        
        [NonSerialized]
        private CancellationToken _cancelationToken;

        [ThreadStatic]
        private static bool s_isReplicationOperaton;

        public static bool IsReplicationOperation
        {
            get { return s_isReplicationOperaton; }
            set { s_isReplicationOperaton = value; }
        }
        
        static OperationContext()
        {
            s_operationUniqueID = Guid.NewGuid().ToString().Substring(0, 4);
            s_operationID = new OperationID() { OpCounter = s_operationCounter, OperationId = s_operationUniqueID };
        }

        public OperationContext()
        {
            NeedUserPayload = true;
            CloneCacheEntry = true;
            UseObjectPool = true;
            //CreateOperationId();
        }

        public OperationContext(OperationContextFieldName fieldName, object fieldValue) : this()
        {
            Add(fieldName, fieldValue);
        }

        /// <summary>
        /// This flag indicates weather we need user payload when a CachEntry is deep cloned or not
        /// WARNGING : This flag MUST be set only when there is a genuine need to user payload and
        /// Set it for only Get Operation.  
        /// </summary>
        public bool NeedUserPayload
        {
            get { return Contains(OperationContextFieldName.NeedUserPayload); }
            set
            {
                if (value) Add(OperationContextFieldName.NeedUserPayload, true);
                else RemoveValueByField(OperationContextFieldName.NeedUserPayload);
            }
        }

        /// <summary>
        /// This flag indicates weather deep cloning of CachEntry is required or not
        /// WARNGING : This flag MUST be Set for only Get Operation.  
        /// </summary>
        public bool CloneCacheEntry
        {
            get { return Contains(OperationContextFieldName.CloneCacheEntry); }
            set
            {
                if (value) Add(OperationContextFieldName.CloneCacheEntry, true);
                else RemoveValueByField(OperationContextFieldName.CloneCacheEntry);
            }
        }

        /// <summary>
        /// This flag indicates weather to use pool for returning objects like CachEntry etc.
        /// If set False, objects are created through normal new operator 
        /// </summary>
        public bool UseObjectPool
        {
            get { return Contains(OperationContextFieldName.UseObjectPool); }
            set
            {
                if (value) Add(OperationContextFieldName.UseObjectPool, true);
                else RemoveValueByField(OperationContextFieldName.UseObjectPool);
            }
        }

        #region Creating OperationContext

        public static OperationContext Create(PoolManager poolManager)
        {
            return poolManager.GetOperationContextPool()?.Rent(true);
        }

        public static OperationContext CreateAndMarkInUse(PoolManager poolManager, int moduleRefId)
        {
            var instance = Create(poolManager);
            instance.MarkInUse(moduleRefId);
            return instance;
        }

        public static OperationContext Create(PoolManager poolManager, OperationContextFieldName fieldName, object fieldValue)
        {
            var instance = Create(poolManager);
            instance.Add(fieldName, fieldValue);
            return instance;
        }

        public static OperationContext CreateAndMarkInUse(PoolManager poolManager, int moduleRefId, OperationContextFieldName fieldName, object fieldValue)
        {
            var instance = Create(poolManager, fieldName, fieldValue);
            instance.MarkInUse(moduleRefId);
            return instance;
        }

        #endregion

        public OperationID OperatoinID
        {
            get => s_operationID;
        }

        public bool IsRemoveQueryOperation
        {
            get => (GetValueByField(OperationContextFieldName.RemoveQueryOperation) as bool?) ?? false;
        }

        public CancellationToken CancellationToken
        {
            set { _cancelationToken = value; }
            get { return _cancelationToken; }
        }

        public void Add(OperationContextFieldName fieldName, object fieldValue)
        {
           _fieldValueTable[(int)fieldName] = fieldValue;
        }

        public object GetValueByField(OperationContextFieldName fieldName)
        {
            return _fieldValueTable[(int)fieldName];
        }

        public bool Contains(OperationContextFieldName fieldName)
        {
            return _fieldValueTable[(int)fieldName] != null;
        }

        public long ClientOperationTimeout
        {
            get => (GetValueByField(OperationContextFieldName.ClientOperationTimeout) as long?) ?? -1;
        }

        public void RemoveValueByField(OperationContextFieldName fieldName)
        {
           _fieldValueTable[(int)fieldName] = null;
        }

        public bool IsOperation(OperationContextOperationType operationType)
        {
            return (OperationContextOperationType)GetValueByField(OperationContextFieldName.OperationType) == operationType;
        }

        private void CreateOperationId()
        {
            long opCounter = Interlocked.Increment(ref s_operationCounter);
            var operationId = IsFromPool
                ? OperationID.Create(PoolManager, s_operationUniqueID, opCounter)
                : new OperationID() { OpCounter = opCounter, OperationId = s_operationUniqueID };

            Add(OperationContextFieldName.OperationId, operationId);
        }

        #region ILeasable



        public override void MarkFree(int moduleRefId)
        {
            
        }

        public override void ResetLeasable()
        {
            Array.Clear(_fieldValueTable, 0, _fieldCount);
            _cancelationToken = default(CancellationToken);

            s_isReplicationOperaton = default(bool);
            NeedUserPayload = true;
            CloneCacheEntry = true;
            UseObjectPool = true;
        }

        public override void ReturnLeasableToPool()
        {
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _fieldValueTable = (object[])reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_fieldValueTable);
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return DeepClone(this.PoolManager);
        }

        public OperationContext DeepClone(PoolManager poolManager)
        {
            OperationContext operationContext = null;

            if (poolManager == null)
                operationContext = new OperationContext();
            else
                operationContext = poolManager.GetOperationContextPool()?.Rent(initialize: true);

            for (int i = 0; i < _fieldValueTable.Length; i++)
            {
                var value = _fieldValueTable[i];
                var cloned = (value as ICloneable)?.Clone();

                operationContext._fieldValueTable[i] = cloned ?? value;
            }
            return operationContext;
        }

        #endregion
    }
}

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
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Caching.Pooling;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Caching
{
    [Serializable]
    public class OperationID : SimpleLease, ICompactSerializable
    {
        private string _opID;
        private long _opCounter;

        public OperationID()
        {
        }

        public static OperationID Create(PoolManager poolManager, string opId, long opCounter)
        {
            // All the data that can mess with consistency is being overwritten anyway.
            // Therefore, there is no need to fetch a clean instance from the pool.
            var instance = poolManager.GetOperationIdPool()?.Rent(initialize: false);
            instance.OperationId = opId;
            instance.OpCounter = opCounter;
            return instance;
        }

        public static OperationID CreateAndMark(PoolManager poolManager, int moduleRefId, string opId, long opCounter)
        {
            var instance = Create(poolManager, opId, opCounter);
            instance.MarkInUse(moduleRefId);
            return instance;
        }

        public string OperationId
        {
            get { return _opID; }
            set { _opID = value; }
        }
        public long OpCounter
        {
            get { return _opCounter; }
            set { _opCounter = value; }
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this._opCounter = reader.ReadInt64();

            var opIdStr = (string)reader.ReadObject();
            this._opID =  opIdStr;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(this._opCounter);
            writer.WriteObject(this._opID);
        }

        #region ILeasable

        public override void MarkFree(int moduleRefId)
        {

        }

        public override sealed void ResetLeasable()
        {

        }

        public override sealed void ReturnLeasableToPool()
        {
        }

        #endregion
    }
}

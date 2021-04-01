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
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling;

namespace Alachisoft.NCache.Common.Caching
{
    /// <summary>
    /// Encapsulates the actual user payload in byte array form. This class is 
    /// designed to keep user payload in chunks of size not greater than 80 KB.
    /// It is designed to handle the large objects.
    /// </summary>
    [Serializable]
    public abstract class UserBinaryObject : BookKeepingLease, ICompactSerializable, IStreamItem, ISizable
    {
        public static int LARGE_OBJECT_SIZE = 79 * 1024;
        public static int BYTE_ARRAY_MEMORY_OVERHEAD = 24;

        #region Creating UserBinaryObject

        public static UserBinaryObject CreateUserBinaryObject(ICollection data, PoolManager poolManager = null)
        {
            if (data == null)
                return null;

            if (data.Count > 1)
                return LargeUserBinaryObject.CreateUserBinaryObject(data, poolManager);

            return SmallUserBinaryObject.CreateUserBinaryObject(data, poolManager);
        }

        /// <summary>
        /// Creates a UserBinaryObject from a byte array
        /// </summary>
        /// <param name="data"></param>
        public static UserBinaryObject CreateUserBinaryObject(byte[] byteArray, PoolManager poolManager = null)
        {
            if (byteArray == null)
                return null;

            float noOfChunks = (float)byteArray.Length / LARGE_OBJECT_SIZE;

            if (noOfChunks > 1)
                return LargeUserBinaryObject.CreateUserBinaryObject(byteArray, poolManager);

            return SmallUserBinaryObject.CreateUserBinaryObject(byteArray, poolManager);
        }

        #endregion

        public abstract Array Data { get; }

        public abstract List<byte[]> DataList { get; }
        /// <summary>
        /// Re-assemle the individual binary chunks into a byte array.
        /// This method should not be called unless very necessary.
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetFullObject();

        public abstract ClusteredArray<byte> GetClusteredArray();

        /// <summary>
        /// This method always creates a new instance of user's payload and copies the data 
        /// to it. Keep in mind that the instance created is not from pool.
        /// </summary>
        /// <returns>The cloned payload.</returns>
        public abstract Array ClonePayload();

        #region ISizable Members

        public abstract int Size { get; }

        public abstract int InMemorySize { get; }

        #endregion

        #region ICompactSerializable Members

        public abstract void Deserialize(CompactReader reader);
        public abstract void Serialize(CompactWriter writer);

        #endregion

        #region IStreamItem Members

        public abstract VirtualArray Read(int offset, int length);

        public abstract void Write(VirtualArray vBuffer, int srcOffset, int dstOffset, int length);

        public abstract int Length{get;set;}

        #endregion

        #region - [Deep Cloning] -

        public abstract UserBinaryObject DeepClone(PoolManager poolManager);

        #endregion
    }
}

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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Encapsulates the actual user payload in byte array form. This class is 
    /// designed to keep user payload in chunks of size not greater than 80 KB.
    /// It is designed to handle the large objects.
    /// </summary>
    [Serializable]
    public abstract class UserBinaryObject : ICompactSerializable,ISizable
    {
        public static int LARGE_OBJECT_SIZE = 79 * 1024;
        public static int BYTE_ARRAY_MEMORY_OVERHEAD = 24;
       
        /// <summary>
        /// Creates a UserBinaryObject from a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static UserBinaryObject CreateUserBinaryObject(byte[] byteArray)
        {
            UserBinaryObject binaryObject = null;
            if (byteArray != null)
            {
                float noOfChunks = (float)byteArray.Length / (float)LARGE_OBJECT_SIZE;

                if (noOfChunks < 1.0)
                {
                    binaryObject = SmallUserBinaryObject.CreateUserBinaryObject(byteArray);
                }
                else
                {
                    binaryObject = LargeUserBinaryObject.CreateUserBinaryObject(byteArray);
                }
            }

            return binaryObject;           
        }

        public static UserBinaryObject CreateUserBinaryObject(Array data)
        {         
            UserBinaryObject binaryObject = null;
            if (data != null)
            {
                if (data.Length > 1)
                {
                    binaryObject = LargeUserBinaryObject.CreateUserBinaryObject(data);
                }
                else
                {
                    binaryObject = SmallUserBinaryObject.CreateUserBinaryObject(data);
                }
            }

            return binaryObject;
        }
      
        public abstract Array Data{get;}

        public abstract List<byte[]> DataList{get;}
        /// <summary>
        /// Re-assemle the individual binary chunks into a byte array.
        /// This method should not be called unless very necessary.
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetFullObject();

        
        #region ISizable Members

        public abstract int Size { get; }

        public abstract int InMemorySize { get; }

        #endregion


        #region ICompactSerializable Members

        public abstract void Deserialize(CompactReader reader);

        public abstract void Serialize(CompactWriter writer);
        
        #endregion

        public abstract int Length{get;}
    }
}

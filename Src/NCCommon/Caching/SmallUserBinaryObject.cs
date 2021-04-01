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
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.Pooling.Extension;

namespace Alachisoft.NCache.Common.Caching
{
    /// <summary>
    /// Encapsulates the actual user payload in byte array form. This class is 
    /// designed to keep user payload in chunks of size not greater than 80 KB.
    /// It is designed to handle the large objects.
    /// </summary>
    [Serializable]
    public class SmallUserBinaryObject : UserBinaryObject
    {
        private byte[] _data;
        private int _actualDataLength;

        public SmallUserBinaryObject() { }

        #region Creating SmallUserBinaryObject

        public static new SmallUserBinaryObject CreateUserBinaryObject(byte[] byteArray, PoolManager poolManager = null)
        {
            if (byteArray == null)
                return null;

            var binaryObject = poolManager.GetSmallUserBinaryObjectPool()?.Rent(true) ?? new SmallUserBinaryObject();
            binaryObject.InitializeUserBinaryObject(byteArray);

            return binaryObject;
        }

        /// <summary>
        /// Creates a UserBinaryObject from a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static new SmallUserBinaryObject CreateUserBinaryObject(ICollection data, PoolManager poolManager = null)
        {
            if (data == null)
                return null;

            var binaryObject = poolManager.GetSmallUserBinaryObjectPool()?.Rent(true) ?? new SmallUserBinaryObject();
            binaryObject.InitializeUserBinaryObject(data);

            return binaryObject;
        }

        #endregion

        public override Array Data
        {
            get
            {
                //ArrayList data = new ArrayList();
                //data.Add(_data);
                //return data.ToArray();
                return DataList.ToArray();
            }
        }

        public override List<byte[]> DataList
        {
            get
            {
                var bytes = _data;
                List<byte[]> byteList = new List<byte[]>(1);

                if (_data.Length != Length)
                {
                    bytes = new byte[Length];
                    Buffer.BlockCopy(_data, 0, bytes, 0, Length);
                }
                byteList.Add(bytes);
                return byteList;
            }
        }

        /// <summary>
        /// Re-assemle the individual binary chunks into a byte array.
        /// This method should not be called unless very necessary.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetFullObject()
        {
            byte[] fullByteArray = null;

            if (Length > 0)
            {
                fullByteArray = new byte[Length];
                Buffer.BlockCopy(_data, 0, fullByteArray, 0, Length);
            }
            return fullByteArray;
        }

        public override ClusteredArray<byte> GetClusteredArray()
        {
            ClusteredArray<byte> clusterByteArray = new ClusteredArray<byte>(Length);
            clusterByteArray.CopyFrom(_data, 0, 0, Length);
            return clusterByteArray;
        }

        public override Array ClonePayload()
        {
            var length = Length;

            if (length > 0)
            {
                var clonedPayload = new byte[length];
                Buffer.BlockCopy(_data, 0, clonedPayload, 0, length);

                return new List<byte[]>(1) { clonedPayload }.ToArray();
            }
            return null;
        }

        #region ISizable Members

        public override int Size
        {
            get { return Length; }
        }

        public override int InMemorySize
        {
            get
            {
                //24 is .net object overhead in memory due to object header which should be included with size of object in memory
                return Length + BYTE_ARRAY_MEMORY_OVERHEAD;
            }
        }

        #endregion

        #region ICompactSerializable Members

        public override void Deserialize(CompactReader reader)
        {
            _data = reader.ReadObject() as byte[];
            _actualDataLength = reader.ReadInt32();
        }

        public override void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_data);
            writer.Write(_actualDataLength);
        }

        #endregion

        #region IStreamItem Members

        public override VirtualArray Read(int offset, int length)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Write(VirtualArray vBuffer, int srcOffset, int dstOffset, int length)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override int Length
        {
            get
            {
                return _actualDataLength;
            }
            set 
            {
            }
        }

        #endregion

        #region ILeasable

        public override void ResetLeasable()
        {
            _data = null;
            _actualDataLength = 0;
        }

        public override void ReturnLeasableToPool()
        {
            if (_data != null && IsFromPool)
                PoolManager.GetByteArrayPool()?.Return(_data);

            //PoolManager.GetSmallUserBinaryObjectPool()?.Return(this);
        }

        #endregion

        private void InitializeUserBinaryObject(byte[] byteArray)
        {
            _data = byteArray;
            _actualDataLength = _data.Length;

            if (!IsFromPool)
                return;

            var buffer = PoolManager.GetByteArrayPool()?.Rent(_actualDataLength) ?? new byte[_actualDataLength];
            Buffer.BlockCopy(_data, 0, buffer, 0, _actualDataLength);
            _data = buffer;
        }

        private void InitializeUserBinaryObject(ICollection data)
        {
            if (data is IList<byte[]> buffers)
            {
                if (buffers.Count > 0)
                {
                    InitializeUserBinaryObject(buffers[0]);
                }
            }
            else
            {
                foreach (byte[] buffer in data)
                {
                    InitializeUserBinaryObject(buffer);
                    break;
                }
            }
        }

        #region - [Deep Cloning] -

        public sealed override UserBinaryObject DeepClone(PoolManager poolManager)
        {
            var clonedData = default(byte[]);
            var clonedUserBinaryObject = poolManager.GetSmallUserBinaryObjectPool()?.Rent(false) ?? new SmallUserBinaryObject();

            if (_data != null)
            {
                clonedData = poolManager.GetByteArrayPool()?.Rent(_data.Length) ?? new byte[_data.Length];
                Buffer.BlockCopy(_data, 0, clonedData, 0, _actualDataLength);
            }

            clonedUserBinaryObject._data = clonedData;
            clonedUserBinaryObject._actualDataLength = _actualDataLength;
            return clonedUserBinaryObject;
        }

        #endregion
    }
}

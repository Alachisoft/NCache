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
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.Pooling.Extension;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.Caching
{
    /// <summary>
    /// Encapsulates the actual user payload in byte array form. This class is 
    /// designed to keep user payload in chunks of size not greater than 80 KB.
    /// It is designed to handle the large objects.
    /// </summary>
    [Serializable]
    public class LargeUserBinaryObject : UserBinaryObject
    {
        private int _index;
        private int _noOfChunks;
        private int _lastChunkSize;
        private ArrayList _data = new ArrayList();

        public LargeUserBinaryObject()
        {
        }

        private void AddDataChunk(byte[] dataChunk)
        {
            if (_data != null && _index < _noOfChunks)
            {
                _data.Insert(_index, dataChunk);
                _index++;
            }
        }

        #region Creating LargeUserBinaryObject

        /// <summary>
        /// Creates a UserBinaryObject from a byte array, which may be a large object.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static new LargeUserBinaryObject CreateUserBinaryObject(ICollection data, PoolManager poolManager = null)
        {
            if (data == null)
                return null;

            var binaryObject = poolManager.GetLargeUserBinaryObjectPool()?.Rent(true) ?? new LargeUserBinaryObject();
            binaryObject.InitializeUserBinaryObject(data);

            return binaryObject;
        }

        public static new LargeUserBinaryObject CreateUserBinaryObject(byte[] byteArray, PoolManager poolManager = null)
        {
            if (byteArray == null)
                return null;

            var binaryObject = poolManager.GetLargeUserBinaryObjectPool()?.Rent(true) ?? new LargeUserBinaryObject();
            binaryObject.InitializeUserBinaryObject(byteArray);

            return binaryObject;
        }

        #endregion

        public override Array Data
        {
            get { return DataList.ToArray(); }
        }

        public override List<byte[]> DataList
        {
            get
            {
                var i = 0;
                var byteList = new List<byte[]>(_noOfChunks);

                if (Length > 0)
                {
                    // _noOfChunks --> _data.Count
                    for (i = 0; i < _noOfChunks - 1; i++)
                        byteList.Add((byte[])_data[i]);

                    var buffer = (byte[])_data[i];

                    if (buffer.Length != _lastChunkSize)
                    {
                        buffer = new byte[_lastChunkSize];
                        Buffer.BlockCopy((byte[])_data[i], 0, buffer, 0, _lastChunkSize);
                    }
                    byteList.Add(buffer);
                }
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
                int nextIndex = 0;
                byte[] binarChunk = null;
                for (int i = 0; i < _data.Count - 1; i++)
                {
                    binarChunk = (byte[])_data[i];
                    if (binarChunk != null)
                    {
                        binarChunk.CopyTo(fullByteArray, nextIndex);
                        nextIndex += binarChunk.Length;
                    }
                }
                Buffer.BlockCopy((byte[])_data[_noOfChunks - 1], 0, fullByteArray, nextIndex, _lastChunkSize);
            }
            return fullByteArray;
        }

        public override ClusteredArray<byte> GetClusteredArray()
        {
            ClusteredArray<byte> clusterByteArray = new ClusteredArray<byte>(Length);

            int nextIndex = 0;
            byte[] binarChunk = null;

            if (Length > 0)
            {
                for (int i = 0; i < _data.Count - 1; i++)
                {
                    binarChunk = (byte[])_data[i];
                    if (binarChunk != null)
                    {
                        clusterByteArray.CopyFrom(binarChunk, 0, nextIndex, binarChunk.Length);
                        nextIndex += binarChunk.Length;
                    }
                }
                clusterByteArray.CopyFrom((byte[])_data[_noOfChunks - 1], 0, nextIndex, _lastChunkSize);
            }
            return clusterByteArray;
        }

        public override Array ClonePayload()
        {
            if (_data == null || Length <= 0)
                return null;

            int i = 0;
            var numChunks = _data.Count;
            var clonedBuffers = new List<byte[]>(numChunks);

            for (i = 0; i < numChunks - 1; i++)
            {
                if (_data[i] is byte[] buffer)
                {
                    var clonedBuffer = new byte[buffer.Length];
                    Buffer.BlockCopy(buffer, 0, clonedBuffer, 0, buffer.Length);

                    clonedBuffers.Add(clonedBuffer);
                }
            }
            if (_data[i] is byte[] lastBuffer)
            {
                var clonedLastBuffer = new byte[_lastChunkSize];
                Buffer.BlockCopy(lastBuffer, 0, clonedLastBuffer, 0, _lastChunkSize);

                clonedBuffers.Add(clonedLastBuffer);
            }
            return clonedBuffers.ToArray();
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
                return Length + (_noOfChunks * BYTE_ARRAY_MEMORY_OVERHEAD);
            }
        }
        #endregion

        #region ICompactSerializable Members

        public override void Deserialize(CompactReader reader)
        {
            _lastChunkSize = reader.ReadInt32();
            _noOfChunks = reader.ReadInt32();
            _index = reader.ReadInt32();

            if (_noOfChunks > 0)
            {
                _data = new ArrayList(_noOfChunks);
                for (int i = 0; i < _noOfChunks; i++)
                {
                    _data.Insert(i, reader.ReadObject() as byte[]);
                }
            }
        }

        public override void Serialize(CompactWriter writer)
        {
            writer.Write(_lastChunkSize);
            writer.Write(_noOfChunks);
            writer.Write(_index);
            for (int i = 0; i < _noOfChunks; i++)
            {
                writer.WriteObject(_data[i]);
            }
        }

        #endregion

        #region IStreamItem Members

        public override VirtualArray Read(int offset, int length)
        {
            VirtualArray vBuffer = null;
            int streamLength = Length;

            if (offset >= streamLength) return new VirtualArray(0);
            if (offset + length > streamLength)
            {
                length -= (offset + length - streamLength);
            }

            VirtualArray vSrc = new VirtualArray(_data);
            vBuffer = new VirtualArray(length);
            VirtualIndex vSrcIndex = new VirtualIndex(offset);
            VirtualIndex vDstIndex = new VirtualIndex();
            VirtualArray.CopyData(vSrc, vSrcIndex, vBuffer, vDstIndex, length);

            return vBuffer;
        }

        public override void Write(VirtualArray vBuffer, int srcOffset, int dstOffset, int length)
        {
            if (vBuffer == null) return;

            VirtualArray vDstArray = new VirtualArray(_data);
            VirtualArray.CopyData(vBuffer, new VirtualIndex(srcOffset), vDstArray, new VirtualIndex(dstOffset), length, true);

            _noOfChunks = _data.Count;
            //_lastChunkSize = ((byte[])_data[_data.Count - 1]).Length;
            _lastChunkSize += (length < 0 ? 0 : length) % LARGE_OBJECT_SIZE;
        }

        public override int Length
        {
            get
            {
                return _noOfChunks == 0 ? _lastChunkSize : ((_noOfChunks - 1) * LARGE_OBJECT_SIZE) + _lastChunkSize;
            }
            set
            {

            }
        }

        #endregion

        #region ILeasable

        public override void ResetLeasable()
        {
            _index = 0;
            _noOfChunks = 0;
            _lastChunkSize = 0;

            _data.Clear();
        }

        public override void ReturnLeasableToPool()
        {
            if (IsFromPool)
            {
                foreach (byte[] chunk in _data)
                {
                    PoolManager.GetByteArrayPool().Return(chunk);
                }
            }
            //PoolManager.GetLargeUserBinaryObjectPool().Return(this);
        }

        #endregion

        private void InitializeUserBinaryObject(ICollection data)
        {
            _noOfChunks = data.Count;

            // The following code contains duplication of certain code segments. This 
            // note serves the purpose of conveying the fact that the duplication is 
            // intended.

            if (data is IList<byte[]> buffers)
            {
                int i = 0;

                for (; i < buffers.Count && i < _noOfChunks - 1; i++)
                {
                    var chunk = buffers[i];
                    var buffer = chunk;

                    if (IsFromPool)
                    {
                        buffer = PoolManager.GetByteArrayPool().Rent(chunk.Length);
                        Buffer.BlockCopy(chunk, 0, buffer, 0, chunk.Length);
                    }
                    _data.Add(buffer);
                }

                var lastChunk = buffers[i];
                var lastBuffer = lastChunk;

                _lastChunkSize = lastChunk.Length;

                if (IsFromPool)
                {
                    lastBuffer = PoolManager.GetByteArrayPool().Rent(_lastChunkSize);
                    Buffer.BlockCopy(lastChunk, 0, lastBuffer, 0, _lastChunkSize);
                }
                _data.Add(lastBuffer);
            }
            else
            {
                var enumerator = data.GetEnumerator();

                for (var i = 0; enumerator.MoveNext() && i < _noOfChunks - 1; i++)
                {
                    var chunk = (byte[])enumerator.Current;
                    var buffer = chunk;

                    if (IsFromPool)
                    {
                        buffer = PoolManager.GetByteArrayPool().Rent(chunk.Length);
                        Buffer.BlockCopy(chunk, 0, buffer, 0, chunk.Length);
                    }
                    _data.Add(buffer);
                }

                var lastChunk = (byte[])enumerator.Current;
                var lastBuffer = lastChunk;

                _lastChunkSize = lastChunk.Length;

                if (IsFromPool)
                {
                    lastBuffer = PoolManager.GetByteArrayPool().Rent(_lastChunkSize);
                    Buffer.BlockCopy(lastChunk, 0, lastBuffer, 0, _lastChunkSize);
                }
                _data.Add(lastBuffer);
            }
        }

        private void InitializeUserBinaryObject(byte[] byteArray)
        {
            int nextChunk = 0;
            int nextChunkSize = 0;
            int noOfChunks = byteArray.Length / LARGE_OBJECT_SIZE;

            noOfChunks += (byteArray.Length - (noOfChunks * LARGE_OBJECT_SIZE)) != 0 ? 1 : 0;
            _noOfChunks = noOfChunks;

            for (int i = 1; i <= noOfChunks - 1; i++)
            {
                nextChunkSize = byteArray.Length - nextChunk;

                if (nextChunkSize > LARGE_OBJECT_SIZE)
                    nextChunkSize = LARGE_OBJECT_SIZE;

                byte[] binaryChunk = PoolManager.GetByteArrayPool()?.Rent(nextChunkSize) ?? new byte[nextChunkSize];
                Buffer.BlockCopy(byteArray, nextChunk, binaryChunk, 0, nextChunkSize);
                AddDataChunk(binaryChunk);

                nextChunk += nextChunkSize;
            }

            _lastChunkSize = byteArray.Length - nextChunk;

            if (_lastChunkSize > LARGE_OBJECT_SIZE)
                _lastChunkSize = LARGE_OBJECT_SIZE;

            byte[] lastBinaryChunk = PoolManager.GetByteArrayPool()?.Rent(_lastChunkSize) ?? new byte[_lastChunkSize];
            Buffer.BlockCopy(byteArray, nextChunk, lastBinaryChunk, 0, _lastChunkSize);
            AddDataChunk(lastBinaryChunk);
        }

        #region - [Deep Cloning] -

        public sealed override UserBinaryObject DeepClone(PoolManager poolManager)
        {
            var clonedData = new ArrayList(_noOfChunks);
            var clonedUserBinaryObject = poolManager.GetLargeUserBinaryObjectPool()?.Rent(false) ?? new LargeUserBinaryObject();

            if (_data?.Count > 0)
            {
                if (poolManager != null)
                {
                    int i = 0;

                    for (i = 0; i < _data.Count - 1; i++)
                    {
                        var buffer = (byte[])_data[i];
                        var clonedBuffer = poolManager.GetByteArrayPool()?.Rent(buffer.Length) ?? new byte[buffer.Length];

                        Buffer.BlockCopy(buffer, 0, clonedBuffer, 0, buffer.Length);
                        clonedData.Add(clonedBuffer);
                    }
                    var lastBuffer = (byte[])_data[i];
                    var clonedLastBuffer = poolManager.GetByteArrayPool()?.Rent(lastBuffer.Length) ?? new byte[lastBuffer.Length];

                    Buffer.BlockCopy(lastBuffer, 0, clonedLastBuffer, 0, _lastChunkSize);
                    clonedData.Add(clonedLastBuffer);
                }
                else
                {
                    int i = 0;

                    for (i = 0; i < _data.Count - 1; i++)
                    {
                        var buffer = (byte[])_data[i];
                        var clonedBuffer = new byte[buffer.Length];

                        Buffer.BlockCopy(buffer, 0, clonedBuffer, 0, buffer.Length);
                        clonedData.Add(clonedBuffer);
                    }
                    var lastBuffer = (byte[])_data[i];
                    var clonedLastBuffer = new byte[lastBuffer.Length];

                    Buffer.BlockCopy(lastBuffer, 0, clonedLastBuffer, 0, _lastChunkSize);
                    clonedData.Add(clonedLastBuffer);
                }
            }

            clonedUserBinaryObject._index = _index;
            clonedUserBinaryObject._data = clonedData;
            clonedUserBinaryObject._noOfChunks = _noOfChunks;
            clonedUserBinaryObject._lastChunkSize = _lastChunkSize;
            return clonedUserBinaryObject;
        }

        #endregion
    }
}

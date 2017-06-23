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
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Encapsulates the actual user payload in byte array form. This class is 
    /// designed to keep user payload in chunks of size not greater than 80 KB.
    /// It is designed to handle the large objects.
    /// </summary>
    [Serializable]
    public class LargeUserBinaryObject : UserBinaryObject
    {
        ArrayList _data = new ArrayList();
        int _noOfChunks;
        int _index;

        public LargeUserBinaryObject(int noOfChunks)
        {
            _noOfChunks = noOfChunks;
        }

        public LargeUserBinaryObject(Array data)
        {
            _noOfChunks = data.Length;
            foreach (byte[] buffer in data)
                _data.Add(buffer);
        }

        public void AddDataChunk(byte[] dataChunk)
        {
            if (_data != null && _index < _noOfChunks)
            {
                _data.Insert(_index, dataChunk);
                _index++;
            }
        }

        /// <summary>
        /// Creates a UserBinaryObject from a byte array, which may be a large object.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static LargeUserBinaryObject CreateUserBinaryObject(byte[] byteArray)
        {
            LargeUserBinaryObject binaryObject = null;
            
            if (byteArray != null)
            {
                int noOfChunks = byteArray.Length / LARGE_OBJECT_SIZE;
                noOfChunks += (byteArray.Length - (noOfChunks * LARGE_OBJECT_SIZE)) != 0 ? 1 : 0;

                binaryObject = new LargeUserBinaryObject(noOfChunks);

                int nextChunk = 0;
                int nextChunkSize = 0;
                for (int i = 1; i <= noOfChunks; i++)
                {
                    nextChunkSize = byteArray.Length - nextChunk;
                    if (nextChunkSize > LARGE_OBJECT_SIZE)
                        nextChunkSize = LARGE_OBJECT_SIZE;

                    byte[] binaryChunk = new byte[nextChunkSize];
                    Buffer.BlockCopy(byteArray, nextChunk, binaryChunk, 0, nextChunkSize);
                    nextChunk += nextChunkSize;
                    binaryObject.AddDataChunk(binaryChunk);
                }
            }

            return binaryObject;
        }

        public static LargeUserBinaryObject CreateUserBinaryObject(Array data)
        {
            LargeUserBinaryObject binaryObject = null;
            if (data != null)
            {
                binaryObject = new LargeUserBinaryObject(data);
            }
            return binaryObject;
        }

        public override Array Data
        {
            get { return _data.ToArray(); }
        }

        public override List<byte[]> DataList
        {
            get
            {
                List<byte[]> byteList = new List<byte[]>();
                foreach (byte[] buffer in _data)
                    byteList.Add(buffer);

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
                for (int i = 0; i < _data.Count; i++)
                {
                    binarChunk = (byte[])_data[i];
                    if (binarChunk != null)
                    {
                        binarChunk.CopyTo(fullByteArray, nextIndex);
                        nextIndex += binarChunk.Length;
                    }
                }
            }
            return fullByteArray;
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
            writer.Write(_noOfChunks);
            writer.Write(_index);
            for (int i = 0; i < _noOfChunks; i++)
            {
                writer.WriteObject(_data[i]);
            }
        }

        #endregion

        public override int Length
        {
            get
            {
                int dataSize = 0;
                for (int i = 0; i < _noOfChunks; i++)
                {
                    dataSize += ((byte[])_data[i]).Length;
                }
                return dataSize;
            }
            
        }
    }
}

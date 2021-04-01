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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class VirtualArray : ICompactSerializable
    {
        IList _baseArray;
        long _size;
        const int maxSize = 79 * 1024;

        public VirtualArray(long size)
        {
            _size = size;
            int largeObjectSize = 79 * 1024;
            int noOfChunks = (int)(size / largeObjectSize);
            noOfChunks += (size - (noOfChunks * largeObjectSize)) != 0 ? 1 : 0;
            _baseArray = new Array[noOfChunks];

            for (int i = 0; i < noOfChunks; i++)
            {
                byte[] buffer = null;
                if (size >= maxSize)
                {
                    buffer = new byte[maxSize];
                    size -= maxSize;
                }
                else
                {
                    buffer = new byte[size];
                }
                _baseArray[i] = buffer;
                
            }

        }

        public VirtualArray(IList array)
        {
            _baseArray = array;
            for (int i = 0; i < array.Count; i++)
            {
                byte[] tmp = array[i] as byte[];
                if (tmp != null) _size += tmp.Length;
            }
        }

        public byte GetValueAt(VirtualIndex vIndex)
        {
            byte[] arr = _baseArray[vIndex.YIndex] as byte[];
            return (byte) arr[vIndex.XIndex];
        }

        public void SetValueAt(VirtualIndex vIndex,byte value)
        {
            byte[] arr = _baseArray[vIndex.YIndex] as byte[];
            arr[vIndex.XIndex] = value;
        }

        public static void CopyData(VirtualArray src, VirtualIndex srcIndex, VirtualArray dst, VirtualIndex dstIndex, int count)
        {
            CopyData(src, srcIndex, dst, dstIndex, count, false);
        }

        public static void CopyData(VirtualArray src, VirtualIndex srcIndex, VirtualArray dst, VirtualIndex dstIndex, int count,bool allowExpantion)
        {
            if (src == null || dst == null || srcIndex == null || dstIndex == null) return;

            if (src.Size < srcIndex.IndexValue)
                throw new IndexOutOfRangeException();


            srcIndex = srcIndex.Clone();
            dstIndex = dstIndex.Clone();

            while (count > 0)
            {
                Array arr = src._baseArray[srcIndex.YIndex] as Array;
                int copyCount = maxSize - srcIndex.XIndex;
                if (copyCount > count)
                {
                    copyCount = count;
                }

                Array dstArr = null;
                if(dst._baseArray.Count >dstIndex.YIndex) dstArr= dst._baseArray[dstIndex.YIndex] as Array;

                int accomdateble = (maxSize - dstIndex.XIndex);

                if (accomdateble > copyCount)
                {
                    accomdateble = copyCount;
                }
                if ((dstArr == null || accomdateble > (dstArr.Length -dstIndex.XIndex)) && allowExpantion)
                {
                    if (dstArr == null)
                    {
                        dstArr = new byte[accomdateble];
                        dst._baseArray.Add(dstArr);
                    }
                    else
                    {
                        byte[] tmpArray = new byte[accomdateble + dstArr.Length - (dstArr.Length - dstIndex.XIndex)];
                        Buffer.BlockCopy(dstArr, 0, tmpArray, 0, dstArr.Length);
                        dstArr = tmpArray;
                        dst._baseArray[dstIndex.YIndex] = dstArr;
                    }
                    
                }

                Buffer.BlockCopy(arr, srcIndex.XIndex, dstArr, dstIndex.XIndex, accomdateble);
                count -= accomdateble;
                srcIndex.IncrementBy(accomdateble);
                dstIndex.IncrementBy(accomdateble);
            }
        }

        public int CopyData(byte[] buffer, int offset, int length)
        {
            if (offset + length > buffer.Length)
                throw new ArgumentException("Length plus offset is greater than buffer size");

            int dataToCopy = (int)(length >= Size ? Size : length);
            int dataCopied = dataToCopy;
            int i = 0;
            while (dataToCopy > 0)
            {
                byte[] binarChunk = (byte[])_baseArray[i];
                if (binarChunk != null)
                {
                    int copyCount = Math.Min(binarChunk.Length,dataToCopy);
                    Buffer.BlockCopy(binarChunk, 0, buffer, offset, copyCount);
                    offset += copyCount;
                    dataToCopy -= copyCount;
                }
                i++;
            }
            return dataCopied;
        }
        public IList BaseArray
        {
            get { return _baseArray; }
        }

        public long Size
        {
            get { return _size; }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _size = reader.ReadInt64();
            _baseArray = reader.ReadObject() as IList;
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_size);
            writer.WriteObject(_baseArray);
        }

        #endregion
    }
}

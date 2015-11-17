﻿﻿/*
* Copyright (c) 2015, Alachisoft. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/



using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    [Serializable]
    public class ClusteredArray<T> : ICloneable
    {
        private int _sizeOfReference;
        private int _lengthThreshold;
        public T[][] _chunks;
        private int _length = 0;

        public ClusteredArray(int length)
        {
            Initialize(length);
        }

        private void Initialize(int length)
        {
            Type genericType = typeof(T);
            if (genericType.IsValueType)
            {
                _sizeOfReference = System.Runtime.InteropServices.Marshal.SizeOf(genericType);
            }
            else
                _sizeOfReference = IntPtr.Size;
            _lengthThreshold = (81920 / _sizeOfReference);
            _length = length;
            int superLength = (length / _lengthThreshold) + 1;

            //Usman:
            //I still believe we need this exception, if we need to keep the main referencial array in SOH.
            //An array declared with greater supersize than length threshold will be declared in LOH.
            //The exception should be removed if we don't care that the main referencial array is being taken to LOH.
            //Otherwise it should be caught by user, who will then declare a new clustered array structure.

            //if (superLength < 0 || superLength > _lengthThreshold)
            //    throw new ArgumentOutOfRangeException(length.ToString("length"));

            //Your call.
            //Update: Let it grow, care later.

            _chunks = new T[superLength][];
            for (int i = 0; i < superLength; i++)
            {
                _chunks[i] = new T[length < _lengthThreshold ? length : _lengthThreshold];
                length -= _lengthThreshold;
            }
        }

        public int LengthThreshold
        {
            get { return _lengthThreshold; }
        }
        public void Resize(int newLength)
        {
            if (_chunks == null)
            {
                Initialize(newLength);
                return;
            }
            int superLength = (newLength / _lengthThreshold) + 1;
            Array.Resize(ref _chunks, superLength);
            int decrementFactor = newLength;
            for (int i = 0; i < _chunks.Length; i++)
            {
                Array.Resize(ref _chunks[i], decrementFactor < _lengthThreshold ? decrementFactor : _lengthThreshold);
                decrementFactor -= _lengthThreshold;
            }
            _length = newLength;
        }

        public T this[int index]
        {
            set
            {
                //if (index < 0 || index > _chunks.Length*_lengthThreshold)
                //    throw new IndexOutOfRangeException();
                int chunkNumber = (index / _lengthThreshold);
                _chunks[chunkNumber][index % _lengthThreshold] = value;
            }
            get
            {
                //if (index < 0 || index > _chunks.Length*_lengthThreshold)
                //    throw new IndexOutOfRangeException();
                int chunkNumber = (index / _lengthThreshold);
                return _chunks[chunkNumber][index % _lengthThreshold];
            }
        }

        public T Get(int chunkNumber, int chunkIndex)
        {
            return _chunks[chunkNumber][chunkIndex];
        }

        public int Length
        {
            get { return _length; }
        }

        public int MaxArrayLength
        {
            get { return _lengthThreshold * _lengthThreshold; }
        }

        public int IndexOf(T reference)
        {
            int index = -1;
            IList<T> currentChunk;
            for (int i = 0; i < _chunks.Length; i++)
            {
                currentChunk = _chunks[i];
                index = currentChunk.IndexOf(reference);
                if (index > -1)
                {
                    index += i * _lengthThreshold;
                    return index;
                }
            }
            return index;
        }

        public int LastIndexOf(T reference)
        {
            int index = -1;
            for (int i = _chunks.Length - 1; i >= 0; i--)
            {
                index = Array.LastIndexOf(_chunks[i], reference);
                if (index > -1)
                {
                    index += i * _lengthThreshold;
                    return index;
                }
            }
            return index;
        }

        public void CopyFrom(Array sourceArray, int arrayIndex, int index, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (sourceArray.Rank > 1)
                throw new ArgumentException();
            if (arrayIndex > sourceArray.Length)
                throw new IndexOutOfRangeException();
            int currentLength = Length;
            int totalLength = index + length;
            if (currentLength < totalLength)
                throw new IndexOutOfRangeException();
            int superIndexStart = (index / _lengthThreshold);
            int superIndexEnd = (totalLength / _lengthThreshold);
            if (superIndexStart == superIndexEnd)
            {
                Array.Copy(sourceArray, arrayIndex, _chunks[superIndexStart], index % _lengthThreshold, length);
                return;
            }
            for (int i = superIndexStart; i <= superIndexEnd; i++)
            {
                int chunkIndex = i == superIndexStart ? index % _lengthThreshold : 0;
                int apparentLength = i == superIndexStart
                    ? _lengthThreshold - chunkIndex
                    : i == superIndexEnd ? totalLength % _lengthThreshold : _lengthThreshold;
                if (length >= 0)
                    Array.Copy(sourceArray, arrayIndex, _chunks[i], chunkIndex, apparentLength);
                index = 0;
                arrayIndex += apparentLength;
                length -= apparentLength;
            }
        }

        public void CopyTo(Array destinationArray, int arrayIndex, int index, int count)
        {
            if (index < 0 || index > _length) throw new ArgumentOutOfRangeException("index");
            if (count < 0 || (count + index) > _length) throw new ArgumentOutOfRangeException("count");
            if ((destinationArray != null) && (destinationArray.Rank != 1))
                throw new ArgumentException("Array is either null or its rank is not 1. ");
            if (arrayIndex > destinationArray.Length || (arrayIndex + count) > destinationArray.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");
            int superIndexStart = (index / _lengthThreshold);
            int superIndexEnd = ((index + count) / _lengthThreshold);
            if (superIndexStart == superIndexEnd)
            {
                Array.Copy(_chunks[superIndexStart], index % _lengthThreshold, destinationArray, arrayIndex, count);
                return;
            }
            for (int i = superIndexStart; i <= superIndexEnd; i++)
            {
                int toBeCopied = (i == superIndexStart)
                    ? _lengthThreshold - (index % _lengthThreshold)
                    : (i == superIndexEnd) ? (index + count) % _lengthThreshold : _lengthThreshold;
                Array.Copy(_chunks[i], i == superIndexStart ? index % _lengthThreshold : 0, destinationArray, arrayIndex,
                    toBeCopied);
                arrayIndex += toBeCopied;
            }

        }

        public void CopyFrom(T[] sourceArray, int arrayIndex, int index, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (sourceArray.Rank > 1)
                throw new ArgumentException();
            if (arrayIndex > sourceArray.Length)
                throw new IndexOutOfRangeException();
            int currentLength = Length;
            int totalLength = index + length;
            if (currentLength < totalLength)
                throw new IndexOutOfRangeException();
            int superIndexStart = (index / _lengthThreshold);
            int superIndexEnd = (totalLength / _lengthThreshold);
            if (superIndexStart == superIndexEnd)
            {
                Array.Copy(sourceArray, arrayIndex, _chunks[superIndexStart], index % _lengthThreshold, length);
                return;
            }
            for (int i = superIndexStart; i <= superIndexEnd; i++)
            {
                int chunkIndex = i == superIndexStart ? index % _lengthThreshold : 0;
                int apparentLength = i == superIndexStart
                    ? _lengthThreshold - chunkIndex
                    : i == superIndexEnd ? totalLength % _lengthThreshold : _lengthThreshold;
                if (length >= 0)
                    Array.Copy(sourceArray, arrayIndex, _chunks[i], chunkIndex, apparentLength);
                index = 0;
                arrayIndex += apparentLength;
                length -= apparentLength;
            }
        }

        public void CopyTo(T[] destinationArray, int arrayIndex, int index, int count)
        {
            if (index < 0 || index > _length) throw new ArgumentOutOfRangeException("index");
            if (count < 0 || (count + index) > _length) throw new ArgumentOutOfRangeException("count");
            if ((destinationArray != null) && (destinationArray.Rank != 1))
                throw new ArgumentException("Array is either null or its rank is not 1. ");
            if (arrayIndex > destinationArray.Length || (arrayIndex + count) > destinationArray.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");
            int superIndexStart = (index / _lengthThreshold);
            int superIndexEnd = ((index + count) / _lengthThreshold);
            if (superIndexStart == superIndexEnd)
            {
                Array.Copy(_chunks[superIndexStart], index % _lengthThreshold, destinationArray, arrayIndex, count);
                return;
            }
            for (int i = superIndexStart; i <= superIndexEnd; i++)
            {
                int toBeCopied = (i == superIndexStart)
                    ? _lengthThreshold - (index % _lengthThreshold)
                    : (i == superIndexEnd) ? (index + count) % _lengthThreshold : _lengthThreshold;
                Array.Copy(_chunks[i], i == superIndexStart ? index % _lengthThreshold : 0, destinationArray, arrayIndex,
                    toBeCopied);
                arrayIndex += toBeCopied;
            }

        }

        public static void Copy(ClusteredArray<T> source, int sourceIndex, ClusteredArray<T> destination,
            int destinationIndex, int length)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            if (sourceIndex > source.Length) throw new ArgumentOutOfRangeException("sourceIndex");
            if (destinationIndex > destination.Length) throw new ArgumentOutOfRangeException("destinationIndex");
            if ((sourceIndex + length) > source.Length) throw new ArgumentOutOfRangeException("length");
            if ((destinationIndex + length) > destination.Length) throw new ArgumentOutOfRangeException("length");


            int lengthThreshold = source._lengthThreshold;
            VirtualIndex srcIndex = new VirtualIndex(source._lengthThreshold, sourceIndex);
            VirtualIndex dstIndex = new VirtualIndex(source._lengthThreshold, destinationIndex);
            int dataCopied = 0;

            while (dataCopied < length)
            {
                T[] srcArray = source._chunks[srcIndex.YIndex];
                T[] dstArray = destination._chunks[dstIndex.YIndex];

                int dataRemaining = length - dataCopied;
                int dstSpaceLeft = dstArray.Length - dstIndex.XIndex;
                int data2Copy = dataRemaining > dstSpaceLeft ? dstSpaceLeft : dataRemaining;

                int srcSpaceLeft = srcArray.Length - srcIndex.XIndex;
                if (data2Copy > srcSpaceLeft)
                    data2Copy = srcSpaceLeft;

                Array.Copy(srcArray, srcIndex.XIndex, dstArray, dstIndex.XIndex, data2Copy);
                dataCopied += data2Copy;

                srcIndex.IncrementBy(data2Copy);
                dstIndex.IncrementBy(data2Copy);
            }
        }

        public static void Clear(ClusteredArray<T> array, int index, int length)
        {
            if (index < 0 || index > array._length) throw new ArgumentOutOfRangeException("index");
            if (length < 0 || index + length > array._length) throw new ArgumentOutOfRangeException("length");
            int _lengthThreshold = array._lengthThreshold;
            int superStartIndex = (index / _lengthThreshold);
            int superEndIndex = ((index + length) / _lengthThreshold);
            int chunkIndex = index % _lengthThreshold;
            if (superStartIndex == superEndIndex)
            {
                Array.Clear(array._chunks[superStartIndex], index % _lengthThreshold, length);
                return;
            }
            for (int i = superStartIndex; i <= superEndIndex; i++)
            {
                int toBeCleared = i == superStartIndex
                           ? _lengthThreshold - chunkIndex
                           : i == superEndIndex ? length % _lengthThreshold : 0;
                Array.Clear(array._chunks[i], i == superStartIndex ? chunkIndex : 0, toBeCleared);
                length -= toBeCleared;
            }
        }

        public T[][] ToArray()
        {
            T[][] clone = new T[_chunks.Length][];
            for (int i = 0; i < _chunks.Length; i++)
            {
                clone[i] = new T[_chunks[i].Length];
                Array.Copy(_chunks[i], 0, clone[i], 0, _chunks[i].Length);
            }
            return clone;
        }

        public object Clone()
        {
            ClusteredArray<T> newarray = new ClusteredArray<T>(_length);
            ClusteredArray<T>.Copy(this, 0, newarray, 0, _length);
            return newarray;
        }

    }
}

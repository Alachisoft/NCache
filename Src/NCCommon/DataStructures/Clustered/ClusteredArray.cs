// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==
/*============================================================
**
** Class:  ClusteredArrayList
** 
** <OWNER>[....]</OWNER>
**
**
** Purpose: Implements a dynamically sized List as a ClusteredArray,
**          and provides many convenience methods for treating
**          a ClusteredArray as an IList.
**
** 
===========================================================*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using System.Reflection.Emit;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    [Serializable]
    public class ClusteredArray<T> : ICloneable
    {
        private int _sizeOfReference;
        private int _lengthThreshold;
        public T[][] _chunks;
        private int _length = 0;
        private readonly bool customLength = false;

        public ClusteredArray(int length)
        {
            Initialize(length);
        }

        public ClusteredArray(int lengthThreshold,int length)
        {
            customLength = true;
            _lengthThreshold = lengthThreshold;
            Initialize(length);
        }

        private void Initialize(int length)
        {
            if (!customLength)
            {
                Type genericType = typeof (T);
                T defaultOfType = default(T);
                try
                {
                    IMemSizable reference = defaultOfType as IMemSizable;

                    if (reference != null)
                    {
                        _sizeOfReference = reference.Size;
                    }
                    else if (genericType.IsValueType)
                    {
                        _sizeOfReference = System.Runtime.InteropServices.Marshal.SizeOf(defaultOfType);
                    }
                    else
                        _sizeOfReference = IntPtr.Size;
                }
                catch
                {
                    _sizeOfReference = SizeOfType(genericType);
                }

                _lengthThreshold = (81920 / _sizeOfReference);
                _length = length;
                int superLength = (length/_lengthThreshold) + 1;

                //I still believe we need this exception, if we need to keep the main referencial array in SOH.
                //An array declared with greater supersize than length threshold will be declared in LOH.
                //The exception should be removed if we don't care that the main referencial array is being taken to LOH.
                //Otherwise it should be caught by user, who will then declare a new clustered array structure.

                //Your call.
                //Update: Let it grow, care later.

                _chunks = new T[superLength][];
                for (int i = 0; i < superLength; i++)
                {
                    _chunks[i] = new T[length < _lengthThreshold ? length : _lengthThreshold];
                    length -= _lengthThreshold;
                }
            }
        }

        private static int SizeOfType(Type type)
        {
            var dm = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Sizeof, type);
            il.Emit(OpCodes.Ret);
            return (int)dm.Invoke(null, null);
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
                int chunkNumber = (index / _lengthThreshold);
                _chunks[chunkNumber][index % _lengthThreshold] = value;
            }
            get
            {
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
            if (sourceIndex > source.Length ) throw new ArgumentOutOfRangeException("sourceIndex");
            if (destinationIndex > destination.Length) throw new ArgumentOutOfRangeException("destinationIndex");
            if((sourceIndex + length) > source.Length) throw new ArgumentOutOfRangeException("length"); 
            if((destinationIndex + length) > destination.Length) throw new ArgumentOutOfRangeException("length");

            VirtualIndex srcIndex = new VirtualIndex(source._lengthThreshold,sourceIndex);
            VirtualIndex dstIndex = new VirtualIndex(source._lengthThreshold,destinationIndex);
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
                Array.Clear(array._chunks[superStartIndex], chunkIndex, length); 
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

        public static explicit operator ClusteredArray<T>(T[] array)
        {
            ClusteredArray<T> newArray = new ClusteredArray<T>(array.Length);
            newArray.CopyFrom(array, 0, 0, array.Length);
            return newArray;
        }

        public static explicit operator T[](ClusteredArray<T> array)
        {
            T[] newarray = new T[array.Length];
            array.CopyTo(newarray, 0, 0, array.Length);
            return newarray;
        }
        public ClusteredArrayList ToInternalList(long newSize)
        {
            
            ClusteredArrayList list = new ClusteredArrayList();

            long consumed = 0;
            for (int i = 0; i < _chunks.Length; i++)
            {
                long dataLeft = newSize - consumed;
                if (consumed + _chunks[i].Length <= newSize)
                {
                    consumed += _chunks[i].Length;
                    list.Add(_chunks[i]);
                }
                else if (dataLeft > 0)
                {
                    Array.Resize(ref _chunks[i], Convert.ToInt32(newSize - consumed));
                    list.Add(_chunks[i]);
                    break;
                }
                else
                    break;
            }
            
            return list;
        }

      

        public object Clone()
        {
            ClusteredArray<T> newarray = new ClusteredArray<T>(_length);
            Copy(this, 0, newarray, 0, _length);
            return newarray;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// Encapsulates the actual user payload in byte array form. This class is 
    /// designed to keep user payload in chunks of size not greater than 80 KB.
    /// It is designed to handle the large objects.
    /// </summary>
    [Serializable]
    public class SmallUserBinaryObject :UserBinaryObject
    {
        byte[] _data;
        public SmallUserBinaryObject(int noOfChunks)
        {
        }

        public SmallUserBinaryObject(byte[] data)
        {
            _data = data;
        }

        public byte[] Bytes { get { return _data; } }

        /// <summary>
        /// Creates a UserBinaryObject from a byte array
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static SmallUserBinaryObject CreateUserBinaryObject(byte[] byteArray)
        {
            SmallUserBinaryObject binaryObject = null;
            if (byteArray != null)
            {
                binaryObject = new SmallUserBinaryObject(byteArray);
            }
            return binaryObject;
        }

        public static SmallUserBinaryObject CreatePooledUserBinaryObject(ICollection data)
        {
            SmallUserBinaryObject binaryObject = null;
            foreach (byte[] buffer in data)
            {
                binaryObject = ObjectPooling.ObjectPoolManager.PayloadObjectPool.GetObject();

                byte[] pooledBuffer = binaryObject.Bytes;

                Buffer.BlockCopy(buffer, 0, pooledBuffer, 0, pooledBuffer.Length);

                break;
            }

            return binaryObject;
        }

        public static SmallUserBinaryObject CreateUserBinaryObject(ICollection data)
        {
            SmallUserBinaryObject binaryObject = null;
            foreach (byte[] buffer in data)
            {
                binaryObject = new SmallUserBinaryObject(buffer);
                break;
            }
            return binaryObject;
        }

        public override Array Data
        {
            get 
            {
                ArrayList data = new ArrayList();
                data.Add(_data);
                return data.ToArray(); 
            }
        }

        public override List<byte[]> DataList
        {
            get
            {
                List<byte[]> byteList = new List<byte[]>();               
                byteList.Add(_data);

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
                _data.CopyTo(fullByteArray, 0);
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
                return Length + BYTE_ARRAY_MEMORY_OVERHEAD;
            }
        }

        #endregion
        
        #region ICompactSerializable Members

        public override void Deserialize(CompactReader reader)
        {
            _data = reader.ReadObject() as byte[];
        }

        public override void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_data);
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
                return _data.Length;
            }
            set
            {
                //throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion

    }
}
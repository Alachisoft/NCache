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
using System.Text;
using System.Threading;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;


namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary>
    /// Each key based on the hashcode belongs to a bucket.
    /// This class keeps the overall stats for the bucket and points to 
    /// its owner.
    /// </summary>


    public class HashMapBucket : ICompactSerializable, ICloneable
    {
        private int _bucketId;
        private Address _tempAddress;
        private Address _permanentAddress;

        private Latch _stateTxfrLatch = new Latch(BucketStatus.Functional);
        private object _status_wait_mutex = new object();

        public HashMapBucket(Address address, int id)
        {
            _tempAddress = _permanentAddress = address;
            _bucketId = id;
            _stateTxfrLatch = new Latch(BucketStatus.Functional);
        }

        public HashMapBucket(Address address, int id, byte status)
            : this(address, id)
        {
            Status = status;
        }

        public int BucketId
        {
            get { return _bucketId; }
        }

        public Address TempAddress
        {
            get { return _tempAddress; }
            set { _tempAddress = value; }
        }

        public Address PermanentAddress
        {
            get { return _permanentAddress; }
            set { _permanentAddress = value; }
        }

        public void WaitForStatus(Address tmpOwner, byte status)
        {
            if (tmpOwner != null)
            {

                while (tmpOwner == _tempAddress)
                {
                    if (_stateTxfrLatch.IsAnyBitsSet(status)) return;
                    lock (_status_wait_mutex)
                    {
                        if ((tmpOwner == _tempAddress) || _stateTxfrLatch.IsAnyBitsSet(status))
                            return;
                        Monitor.Wait(_status_wait_mutex);
                    }
                }
            }
        }

        public void NotifyBucketUpdate()
        {
            lock (_status_wait_mutex)
            {
                Monitor.PulseAll(_status_wait_mutex);
            }
        }
        /// <summary>
        /// Sets the status of the bucket. A bucket can have any of the following status
        /// 1- Functional
        /// 2- UnderStateTxfr
        /// 3- NeedStateTransfer.
        /// </summary>
        public byte Status
        {
            get { return _stateTxfrLatch.Status.Data; }
            set
            {
                switch (value)
                {
                    case BucketStatus.Functional:
                    case BucketStatus.NeedTransfer:
                    case BucketStatus.UnderStateTxfr:
                        //these are valid status,we allow them to be set.
                        byte oldStatus = _stateTxfrLatch.Status.Data;
                        if (oldStatus == value) return;
                        _stateTxfrLatch.SetStatusBit(value, oldStatus);
                        break;
                }
            }
        }
        public Latch StateTxfrLatch
        {
            get { return _stateTxfrLatch; }
        }

        public override bool Equals(object obj)
        {
            HashMapBucket bucket = obj as HashMapBucket;
            if (bucket != null)
            {
                return this.BucketId == bucket.BucketId;
            }
            return false;
        }

        public object Clone()
        {
            HashMapBucket hmBucket = new HashMapBucket(_permanentAddress, _bucketId);
            hmBucket.TempAddress = _tempAddress;
            hmBucket.Status = Status;
            return hmBucket;
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            _bucketId = reader.ReadInt32();
            _tempAddress = (Address)reader.ReadObject();
            _permanentAddress = (Address)reader.ReadObject();
            byte status = reader.ReadByte();
            _stateTxfrLatch = new Latch(status);
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            writer.Write(_bucketId);
            writer.WriteObject(_tempAddress);
            writer.WriteObject(_permanentAddress);
            writer.Write(_stateTxfrLatch.Status.Data);
        }

        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Bucket[" + _bucketId + " ; ");
            sb.Append("owner = " + _permanentAddress + " ; ");
            sb.Append("temp = " + _tempAddress + " ; ");
            string status = null;
            //object can be zero object(initialization without default values), which may cause exception.
            if (_stateTxfrLatch != null)
                status = BucketStatus.StatusToString(_stateTxfrLatch.Status.Data);
            sb.Append(status + " ]");
            return sb.ToString();

        }
    }
}

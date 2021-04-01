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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;


namespace Alachisoft.NCache.Common.Locking
{
    /// <summary>
    /// LockManager is responsible for maintaining locks for cache items.
    /// </summary>
    public class LockManager :ICompactSerializable
    {

        #region /                       --- LockHandle (Inner Class)---                 /

        class LockHandle : ICompactSerializable
        {
            private string _lockId;
            private DateTime _lockTime;

            public LockHandle(string lockId)
            {
                _lockId = lockId;
                _lockTime = DateTime.Now;
            }

            public string LockId { get { return _lockId; } }

            public DateTime LockTime { get { return _lockTime; } }

            public override bool Equals(object obj)
            {
                LockHandle other = obj as LockHandle;
                if (other != null && other._lockId == _lockId)
                    return true;
                else if (obj is string)
                    return _lockId == (string)obj;
                else
                    return false;
            }


            #region ICompactSerializable Members

            public void Deserialize(CompactReader reader)
            {
                _lockId = reader.ReadObject() as string;
            }

            public void Serialize(CompactWriter writer)
            {
                writer.WriteObject(_lockId);
            }

            #endregion
        }

        #endregion

        private List<LockHandle> _readerLocks = new List<LockHandle>();
        private LockHandle _writerLock;
        private LockMode _lockMode = LockMode.None;

        private string GenerateLockId()
        {
            return Guid.NewGuid().ToString() + DateTime.Now.Ticks; ;
        }

        /// <summary>
        /// Gets the current locking mode.
        /// </summary>
        public LockMode Mode
        {
            get { return _lockMode; }
        }
        /// <summary>
        /// Acquires the reader lock for a cache item. Reader lock is assigned
        /// only if no writer lock exists. Multiple reader locks can be acquired.
        /// </summary>
        /// <returns>Unique lock id</returns>
        public bool AcquireReaderLock(string lockHandle)
        {   
            lock (this)
            {
                if (_lockMode == LockMode.None || _lockMode == LockMode.Reader)
                {
                    _readerLocks.Add(new LockHandle(lockHandle));
                    _lockMode = LockMode.Reader;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Releases a reader lock on a cache item.
        /// </summary>
        /// <param name="lockId"></param>
        public void ReleaseReaderLock(string lockId)
        {
            if (lockId == null) return;
            lock (this)
            {
                if (_lockMode == LockMode.Reader)
                {
                    if (_readerLocks.Contains(new LockHandle(lockId)))
                    {
                        _readerLocks.Remove(new LockHandle(lockId));
                        if (_readerLocks.Count == 0) _lockMode = LockMode.None;
                    }
                }
            }
        }

        /// <summary>
        /// Acquires the writer lock on a cache item. Writer lock is acquired only if
        /// no reader or wirter lock exists on the item.
        /// </summary>
        /// <returns>Lockid against which lock is acquired.</returns>
        public bool AcquireWriterLock(string lockHandle)
        {
            lock (this)
            {
                if (_lockMode == LockMode.None)
                {
                    _writerLock = new LockHandle(lockHandle);
                    _lockMode = LockMode.Write;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Releases a writer lock on the cache item.
        /// </summary>
        /// <param name="lockId"></param>
        public void ReleaseWriterLock(string lockId)
        {
            lock (this)
            {
                if (_lockMode == LockMode.Write )
                {
                    if (_writerLock.Equals(lockId))
                    {
                        _writerLock = null;
                        _lockMode = LockMode.None;
                    }
                }
            }
        }

        /// <summary>
        /// Validates whether a valid lock is acquired by a lock holder.
        /// </summary>
        /// <param name="mode">Locking mode.</param>
        /// <param name="lockId">LockId for which lock is to be validated.</param>
        /// <returns>Returns true if a lock holder still holds lock on the item.</returns>
        public bool ValidateLock(LockMode mode, string lockId)
        {
            if (lockId == null && mode != LockMode.None) return false;

            lock (this)
            {
                switch (mode)
                {
                    case LockMode.Reader:
                        return _readerLocks.Contains(new LockHandle(lockId));

                    case LockMode.Write:
                        return _writerLock.Equals(lockId);

                    case LockMode.None:
                        return true;
                }
            }
            return false;   
        }

        public bool ValidateLock(string lockId)
        {
            return ValidateLock(_lockMode, lockId);
        }


        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _lockMode = (LockMode)reader.ReadByte();
            string writerLockId = reader.ReadObject() as string;
            if (!string.IsNullOrEmpty(writerLockId))
                _writerLock = new LockHandle(writerLockId);

            int readLockCount = reader.ReadInt32();
            _readerLocks = new List<LockHandle>();
            for (int i = 0; i < readLockCount; i++)
                _readerLocks.Add(new LockHandle(reader.ReadObject() as string));
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write((byte)_lockMode);
            
            if (_writerLock != null)
                writer.WriteObject(_writerLock.LockId);
            else
                writer.WriteObject(null);

            writer.Write(_readerLocks.Count);
            foreach(LockHandle handle in _readerLocks)
                writer.WriteObject(handle.LockId);
        }

        #endregion
    }
}

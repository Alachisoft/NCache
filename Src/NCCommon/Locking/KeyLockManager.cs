// Copyright (c) 2018 Alachisoft
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

using System.Collections.Generic;
using System.Threading;

namespace Alachisoft.NCache.Common.Locking
{
    /// <summary>
    /// A responsible class for management of reader/writer locks of cache-item keys.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class KeyLockManager<TKey>
    {
        private readonly LockRecursionPolicy _policy;
        private readonly IDictionary<TKey, SlimLockWrapper> _keyLockTable;

        private readonly object _mutex = new object();

        public KeyLockManager(LockRecursionPolicy policy)
        {
            _policy = policy;
            _keyLockTable = new Dictionary<TKey, SlimLockWrapper>();
        }

        public void GetReaderLock(TKey key)
        {
            GetLock(key, LockMode.Reader);
        }

        public void ReleaseReaderLock(TKey key)
        {
            ReleaseLock(key, LockMode.Reader);
        }

        public void GetWriterLock(TKey key)
        {
            GetLock(key, LockMode.Write);
        }

        public void ReleaseWriterLock(TKey key)
        {
            ReleaseLock(key, LockMode.Write);
        }

        private void GetLock(TKey key, LockMode lockMode)
        {
            SlimLockWrapper slimlock = BorrowLockObject(key, true);

            switch (lockMode)
            {
                case LockMode.Reader:
                    slimlock.GetReaderLock();
                    return;
                case LockMode.Write:
                    slimlock.GetWriterLock();
                    return;
            }
        }

        private void ReleaseLock(TKey key, LockMode lockMode)
        {
            SlimLockWrapper lockObject = GetLockObject(key);

            if (lockObject == null)
                return;

            switch (lockMode)
            {
                case LockMode.Reader:
                    lockObject.ReleaseReaderLock();
                    break;
                case LockMode.Write:
                    lockObject.ReleaseWriterLock();
                    break;
            }

            ReturnLockObject(key, lockObject);
        }

        /// <summary>
        /// This method borrows a lock from the lowest layer (lock store).
        /// </summary>
        /// <param name="key">Generic key against which the lock is maintained</param>
        /// <returns></returns>
        private SlimLockWrapper BorrowLockObject(TKey key, bool createNew)
        {
            SlimLockWrapper retInstance = null;
            lock (_keyLockTable)
            {
                if (_keyLockTable.ContainsKey(key))
                {
                    retInstance = _keyLockTable[key];
                }
                if (retInstance == null && !createNew)
                {
                    return null;
                }
                if (retInstance == null)
                {
                    retInstance = _keyLockTable[key] = new SlimLockWrapper(_policy);
                }
                else if (retInstance.MarkedDeleted)
                {
                    retInstance = _keyLockTable[key] = new SlimLockWrapper(_policy);
                }
                retInstance.IncrementRef();
                return retInstance;
            }
        }

        private SlimLockWrapper GetLockObject(TKey key)
        {
            SlimLockWrapper retInstance = null;
            lock (_keyLockTable)
            {
                if (_keyLockTable.ContainsKey(key))
                {
                    retInstance = _keyLockTable[key];
                }
                return retInstance;
            }
        }

        /// <summary>
        /// This method returns a lock object to the lowest layer (lock store).
        /// </summary>
        /// <param name="key">Generic key against which the lock is maintained.</param>
        /// <param name="lockObject">The lock value stored.</param>
        private void ReturnLockObject(TKey key, SlimLockWrapper lockObject)
        {
            lock (_keyLockTable)
            {
                lockObject.DecrementRef();
                if (lockObject.MarkedDeleted)
                {
                    SlimLockWrapper outValue;
                    if (_keyLockTable.TryGetValue(key, out outValue)
                        && lockObject == outValue)
                    {
                        _keyLockTable.Remove(key);
                    }
                }
            }
        }
    }
}

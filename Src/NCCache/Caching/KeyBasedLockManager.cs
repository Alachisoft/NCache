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
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Caching
{
    public class KeyBasedLockManager
    {
        #region /                   --- LockInfo Class ---                      /

        class LockInfo
        {
            Queue _queue = new Queue(5);
            bool _lastPulsed;

            public bool AddWaitingThread()
            {
                object syncObject = new object();
                lock (syncObject)
                {
                    lock (_queue.SyncRoot)
                    {
                        if (_lastPulsed)
                            return false;
                        _queue.Enqueue(syncObject);
                    }
                    Monitor.Wait(syncObject);
                }
                return true;
            }

            /// <summary>
            /// Pulse any thread waiting for queue.
            /// </summary>
            /// <returns>true if thread was waiting otherwise false</returns>
            public bool PulseWaitingThread()
            {
                object syncObject = null;
                lock (_queue.SyncRoot)
                {
                    if (_queue.Count == 0)
                    {
                        _lastPulsed = true;
                        return false;
                    }
                    syncObject = _queue.Dequeue();
                }

                lock (syncObject)
                {
                    Monitor.Pulse(syncObject);
                }
                return true;
            }

            public int Count { get { return _queue.Count; } }
        }

        #endregion

        #region/            --- LockContext Class ---                   /

        class LockingContext
        {
            Thread _currentThread;
            int _refCount;

            public LockingContext()
            {
                _currentThread = Thread.CurrentThread;
            }

            public bool IsCurrentContext
            {
                get
                {
                    bool isCurrentThread = _currentThread.Equals(Thread.CurrentThread);
                    return isCurrentThread;
                }
            }

            public void IncrementRefCount()
            {
                _refCount++;
            }

            public bool DecrementRefCount()
            {
                _refCount--;
                return _refCount == 0 ? true : false;
            }
        }

        #endregion

        private Hashtable _lockTable = new Hashtable();
        private object _sync_mutex = new object();
        private bool _globalLock;
        private bool _waiting4globalLock;
        private LockInfo _globalLockInfo;
        private LockingContext _globalLockingContext;

        public void AcquireLock(object key)
        {
            LockInfo info = null;
            lock (_sync_mutex)
            {
                while (_globalLock || _waiting4globalLock)
                {
                    if (_globalLock && _globalLockingContext != null && _globalLockingContext.IsCurrentContext)
                        break;
                    Monitor.Wait(_sync_mutex);
                }

                if (!_lockTable.Contains(key))
                {
                    _lockTable.Add(key, new LockInfo());
                }
                else
                {
                    info = _lockTable[key] as LockInfo;

                }
            }
            if (info != null)
            {
                bool lockAcquired = info.AddWaitingThread();
                //retry
                if (!lockAcquired) AcquireLock(key);
            }

        }

        public void ReleaseLock(object key)
        {
            lock (_sync_mutex)
            {
                if (_lockTable.Contains(key))
                {
                    LockInfo info = _lockTable[key] as LockInfo;
                    if (!info.PulseWaitingThread()) _lockTable.Remove(key);
                }
                if (_waiting4globalLock && _lockTable.Count == 0)
                {
                    _globalLock = true;
                    _waiting4globalLock = false;
                    _globalLockInfo.PulseWaitingThread();
                }
            }
        }

        public void AcquireGlobalLock()
        {
            lock (_sync_mutex)
            {
                //wait untill global lock is released.
                if (_globalLockingContext != null && _globalLockingContext.IsCurrentContext)
                {
                    _globalLockingContext.IncrementRefCount();
                    return;
                }
                while (_globalLock || _waiting4globalLock)
                {
                    Monitor.Wait(_sync_mutex);
                }

                if (_lockTable.Count == 0)
                {
                    //global lock is acquired.
                    _globalLock = true;

                }
                else
                {
                    _waiting4globalLock = true;
                    _globalLockInfo = new LockInfo();
                }
            }

            if (_globalLockInfo != null && !_globalLock)
            {
                _globalLockInfo.AddWaitingThread();
            }

            _globalLockingContext = new LockingContext();
            _globalLockingContext.IncrementRefCount();
        }

        public void ReleaseGlobalLock()
        {
            lock (_sync_mutex)
            {
                if (_globalLockingContext.IsCurrentContext && _globalLockingContext.DecrementRefCount())
                {
                    _globalLock = false;
                    _globalLockInfo = null;
                    _globalLockingContext = null;
                    Monitor.PulseAll(_sync_mutex);
                }
            }
        }
    }
}

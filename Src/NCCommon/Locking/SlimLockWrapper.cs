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
using System.Threading;

namespace Alachisoft.NCache.Common.Locking
{
    internal class SlimLockWrapper
    {
        private bool _isDeleted;
        private int _refCount;



        private readonly object _mutex = new object();

        internal SlimLockWrapper() //LockRecursionPolicy policy)
        {

        }
        
        internal bool MarkedDeleted
        {
            get
            {
                lock (_mutex)
                {

                    if (_isDeleted)
                    {
                        return true;
                    }

                    if (_refCount <= 0)
                    {
                        return _isDeleted = true;
                    }

                    return false;
                }
            }
        }

        internal void IncrementRef()
        {
            lock (_mutex)
            {
                _refCount++;
            }
        }

        internal void DecrementRef()
        {
            lock (_mutex)
            {
                _refCount --;
            }
        }

        internal void GetLock()
        {
            IncrementRef();

            Monitor.Enter(this);
        }

        internal void ReleaseLock()
        {
            Monitor.Exit(this);

            DecrementRef();
        }

    }
}

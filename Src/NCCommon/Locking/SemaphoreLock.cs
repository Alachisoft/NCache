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

using System.Threading;

namespace Alachisoft.NCache.Common.Locking
{
﻿    public sealed class SemaphoreLock : IDisposable
    {

        //The following fast semaphore could be considered too instead of getting a handle from the OS...
        readonly FastSemaphore _semaphore = new FastSemaphore();
        const int OwnedFlag = unchecked((int)0x80000000);

        // the high bit is set if the lock is held. the lower 31 bits hold the number of threads waiting
        int _lockState;

        public void Enter()
        {
            while (true)
            {
                int state = _lockState;
                if ((state & OwnedFlag) == 0) // if the lock is not owned...
                {
                    // try to acquire it. if we succeed, then we're done
                    if (Interlocked.CompareExchange(ref _lockState, state | OwnedFlag, state) == state) return;
                }
                // the lock is owned, so try to add ourselves to the count of waiting threads
                else if (Interlocked.CompareExchange(ref _lockState, state + 1, state) == state)
                {
                    _semaphore.Wait();
                }
            }
        }

        public void Exit()
        {
            // throw an exception if Exit() is called when the lock is not held
            if ((_lockState & OwnedFlag) == 0) throw new SynchronizationLockException();

            // we want to free the lock by clearing the owned flag. if the result is not zero, then
            // another thread is waiting, and we'll release it, so we'll subtract one from the wait count
            int state, freeState;
            do
            {
                state = _lockState;
                freeState = state & ~OwnedFlag;
            }
            while (Interlocked.CompareExchange(ref _lockState, freeState == 0 ? 0 : freeState - 1, state) != state);

            if (freeState != 0) _semaphore.Release(); // if other threads are waiting, release one of them
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}

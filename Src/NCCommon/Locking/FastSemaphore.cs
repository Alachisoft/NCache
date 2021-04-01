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
using System.Linq;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Common.Locking
{
    sealed class FastSemaphore : IDisposable
    {
        uint _count;

        public void Release()
        {
            lock (this)
            {
                if (_count == uint.MaxValue) throw new InvalidOperationException();
                _count++;
                Monitor.Pulse(this);
            }
        }

        public void Release(uint count)
        {
            if (count != 0)
            {
                lock (this)
                {
                    this._count += count;
                    if (this._count < count) // if it overflowed, undo the addition and throw an exception
                    {
                        this._count -= count;
                        throw new InvalidOperationException();
                    }

                    if (count == 1) Monitor.Pulse(this);
                    else Monitor.PulseAll(this);
                }
            }
        }

        public void Wait()
        {
            lock (this)
            {
                while (_count == 0) Monitor.Wait(this);
                _count--;
            }
        }

        public void Close()
        {

        }

        public void Dispose()
        {

        }
    }
}


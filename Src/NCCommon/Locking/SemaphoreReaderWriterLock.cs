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
    public class SemaphoreReaderWriterLock
    {
        // a better spin lock
        static void SpinWait(int spinCount)
        {
            if (spinCount < 10 && MultiProcessor) Thread.SpinWait(20 * (spinCount + 1));
            else if (spinCount < 15) Thread.Sleep(0); // or use Thread.Yield() in .NET 4
            else Thread.Sleep(1);
        }

        static readonly bool MultiProcessor = Environment.ProcessorCount > 1;

        public void EnterRead()
        {
            while (true)
            {
                int state = lockState;
                if ((uint)state < (uint)ReaderMask) // if it's free or owned by a reader, and we're not currently at the reader limit...
                {
                    if (Interlocked.CompareExchange(ref lockState, state + OneReader, state) == state) break; // try to join in
                }
                else if ((state & ReaderWaitingMask) == ReaderWaitingMask) // otherwise, we need to wait. if there aren't any wait slots left...
                {
                    Thread.Sleep(10); // massive contention. sleep while waiting for a slot
                }
                else if (Interlocked.CompareExchange(ref lockState, state + OneReaderWaiting, state) == state) // if we could get a wait slot...
                {
                    readWait.Wait(); // wait until we're awakened
                }
            }
        }

        public void ExitRead()
        {
            if ((lockState & ReaderMask) == 0) throw new SynchronizationLockException();

            int state, newState;
            do
            {
                // if there are other readers, just subtract one reader. otherwise, if we're the only reader and there are no writers waiting, free
                // the lock. otherwise, we'll wake up one of the waiting writers, so subtract one and reserve it for the writer. we must be careful
                // to preserve the ReservedForWriter flag since it can be set by a writer before it decides to wait
                state = lockState;
                newState = (state & ReaderMask) != OneReader ? state - OneReader :
                           (state & WriterWaitingMask) == 0 ? state & ReservedForWriter :
                           (state | ReservedForWriter) - (OneReader + OneWriterWaiting);
            } while (Interlocked.CompareExchange(ref lockState, newState, state) != state);

            if ((state & ReaderMask) == OneReader) ReleaseWaitingThreads(state); // if we were the last reader, release waiting threads
        }

        public void EnterWrite()
        {
            while (true)
            {
                int state = lockState, ownership = state & (OwnedByWriter | ReservedForWriter | ReaderMask);
                if (ownership == 0 || ownership == ReservedForWriter) // if the lock is free or reserved for a writer...
                {
                    // try to take it for ourselves
                    if (Interlocked.CompareExchange(ref lockState, state & ~ReservedForWriter | OwnedByWriter, state) == state) return;
                }
                else if ((state & WriterWaitingMask) == WriterWaitingMask) // if we want to wait but there aren't any slots left...
                {
                    Thread.Sleep(10); // massive contention. sleep while we wait for a slot
                }
                else if (Interlocked.CompareExchange(ref lockState, state + OneWriterWaiting, state) == state) // if we got a wait slot...
                {
                    writeWait.Wait();
                }
            }
        }

        public void ExitWrite()
        {
            if ((lockState & OwnedByWriter) == 0) throw new SynchronizationLockException();

            int state, newState;
            do
            {
                // if no writers are waiting, mark the lock as free. otherwise, subtract one waiter and reserve the lock for it
                state = lockState;
                newState = (state & WriterWaitingMask) == 0 ? 0 : state + unchecked(ReservedForWriter - OwnedByWriter - OneWriterWaiting);
            } while (Interlocked.CompareExchange(ref lockState, newState, state) != state);

            ReleaseWaitingThreads(state);
        }

        public bool Upgrade()
        {
            if ((lockState & ReaderMask) == 0) throw new SynchronizationLockException();

            int spinCount = 0;
            bool reserved = false;
            while (true)
            {
                int state = lockState;
                if ((state & ReaderMask) == OneReader) // if we're the only reader...
                {
                    // try to convert the lock to be owned by us in write mode
                    if (Interlocked.CompareExchange(ref lockState, state & ~(ReservedForWriter | ReaderMask) | OwnedByWriter, state) == state)
                    {
                        return true; // if we succeeded, then we're done and we were the first upgrader to do so
                    }
                }
                else if (reserved) // if the lock is reserved for us, spin until all the readers are gone
                {
                    SpinWait(spinCount++);
                }
                else if ((state & ReservedForWriter) == 0) // if the lock isn't reserved for anyone yet...
                {
                    // try to reserve it for ourselves
                    reserved = (Interlocked.CompareExchange(ref lockState, state | ReservedForWriter, state) == state);
                }
                // there are other readers and the lock is already reserved for another upgrader, so convert ourself from
                // a reader to a waiting writer. (otherwise, two readers trying to upgrade would deadlock.)
                else if ((state & WriterWaitingMask) == WriterWaitingMask) // if there aren't any slots left for waiting writers...
                {
                    Thread.Sleep(10); // massive contention. sleep while we await one
                }
                else if (Interlocked.CompareExchange(ref lockState, state + (OneWriterWaiting - OneReader), state) == state)
                {
                    writeWait.Wait(); // wait until we're awakened
                    EnterWrite(); // do the normal loop to enter write mode
                    return false; // return false because we weren't the first reader to upgrade the lock
                }
            }
        }

        public void Downgrade()
        {
            if ((lockState & OwnedByWriter) == 0) throw new SynchronizationLockException();
            int state;
            do state = lockState;
            while (Interlocked.CompareExchange(ref lockState, state + unchecked(OneReader - OwnedByWriter), state) != state);
        }

        void ReleaseWaitingThreads(int state)
        {
            // if any writers were waiting, release one of them. otherwise, if any readers were waiting, release all of them
            if ((state & WriterWaitingMask) != 0) writeWait.Release();
            else if ((state & ReaderWaitingMask) != 0) readWait.Release((uint)(state & ReaderWaitingMask));
        }

        const int OwnedByWriter = unchecked((int)0x80000000), ReservedForWriter = 0x40000000;
        const int WriterWaitingMask = 0x3FC00000, ReaderMask = 0x3FF800, ReaderWaitingMask = 0x7FF;
        const int OneWriterWaiting = 1 << 22, OneReader = 1 << 11, OneReaderWaiting = 1;

        // the high bit is set if the lock is owned by a writer. the next bit is set if the lock is reserved for a writer. the next 8 bits are
        // the number of threads waiting to write. the next 11 bits are the number of threads reading. the low 11 bits are the number of
        // threads waiting to read. this lets us easily check if the lock is free for reading by comparing it to the read mask
        int lockState;

        readonly FastSemaphore readWait = new FastSemaphore(), writeWait = new FastSemaphore();
    }

}

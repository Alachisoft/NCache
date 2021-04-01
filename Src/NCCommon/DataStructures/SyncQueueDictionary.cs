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
namespace Alachisoft.NCache.Common.DataStructures
{
    class SyncQueueDictionary<V> : QueueDictionary<V>
    {
        private readonly object _semaLock = new object();

        public override bool Enqueue(V value)
        {
            lock (_semaLock)
                return base.Enqueue(value);
        }

        public override bool Remove(V value)
        {
            lock (_semaLock)
                return base.Remove(value);
        }

        public override V Peek()
        {
            lock (_semaLock)
                return base.Peek();
        }
        public override V Dequeue()
        {
            lock (_semaLock)
                return base.Dequeue();
        }

        public override void Dispose()
        {
            lock (_semaLock)
                base.Dispose();
        }
    }
}
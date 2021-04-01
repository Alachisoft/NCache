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
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary>
    /// The StackDictionary provide impelemntation for IStore, behave as Stack and Dictionary and the same time
    /// </summary>
    public class QueueDictionary<V>  : IEnumerable
    {
        private IDictionary<V, QEntry<V>> store = null;
        private QEntry<V> head = null;
        private QEntry<V> tail = null;

        public QueueDictionary(IEqualityComparer<V> comparer)
        {
           store= new Dictionary<V, QEntry<V>>(comparer);
        }

        public QueueDictionary()
        {
            store = new Dictionary<V, QEntry<V>>();
        }

        public int Count
        {
            get
            {
                return store.Count;
            }
        }

        public virtual bool Enqueue(V value)
        {
            if (store.ContainsKey(value)) return false;

            QEntry<V> entry = new QEntry<V>(value);
            store.Add(value, entry);

            if (head == null && tail == null)
            {
                head = entry;
            }
            else if (tail != null || head != null)
            {
                entry.Prev = tail;
                tail.Next = entry;
            }

            tail = entry;

            return true;
        }

        public virtual bool Remove(V value)
        {
            QEntry<V> entry = null;
            store.TryGetValue(value, out entry);

            if (entry != null)
            {
                if (!store.Remove(value)) return false;

                if (store.Count == 0)
                {
                    head = tail = null;
                    return true;
                }

                if (entry.Prev == null)
                {
                    // if peek is being removed then set peek to next and remove this one
                    head = head.Next;
                    if (head != null) head.Prev = null;
                }
                else
                {
                    entry.Prev.Next = entry.Next;

                    //if not last entry of store then connect next of entry with previous
                    if (entry.Next != null)
                        entry.Next.Prev = entry.Prev;
                    else
                        tail = entry.Prev;
                }

                return true;
            }

            return false;
        }

        public virtual bool Contains(V value)
        {
            return store.ContainsKey(value);
        }

        public virtual V Peek()
        {
            if (head == null || head.Value == null) return default(V);

            return head.Value;
        }

        public virtual V Dequeue()
        {
            if (head == null) return default(V);

            var takeAway = head.Value;

            if (!Remove(takeAway)) return default(V);

            return takeAway;
        }

        public virtual void Dispose()
        {
            if (store != null)
            {
                store.Clear();
                store = null;
            }
            tail = head = null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var pair in store.Values)
                    yield return pair.Value;
        }

        public ICollection<V> Keys { get { return store.Keys; } }
    }
}

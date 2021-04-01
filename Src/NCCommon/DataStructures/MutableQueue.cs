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
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class MutableQueue<T>
    {
        private object _mutex = new object();
        LinkedList<T> list;

        public MutableQueue()
        {
            list = new LinkedList<T>();
        }

        public void Enqueue(T t)
        {
            lock (_mutex)
            {
                list.AddLast(t); 
            }
        }

        public T Dequeue()
        {
            lock (_mutex)
            {
                var result = list.First.Value;
                list.RemoveFirst();
                return result; 
            }
        }

        public T Peek()
        {
            lock (_mutex)
            {
                return list.First.Value; 
            }
        }

        public bool Remove(T t)
        {
            lock (_mutex)
            {
                return list.Remove(t); 
            }
        }

        public bool Contains(T t)
        {
            lock (_mutex)
            {
                return list.Contains(t); 
            }
        }

        public int Count 
        { 
            get 
            {
                lock (_mutex)
                {
                    return list.Count;  
                }
            } 
        }
    }

}

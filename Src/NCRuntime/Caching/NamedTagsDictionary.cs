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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.Caching
#else
namespace Alachisoft.NCache.Runtime.Caching
#endif
{


    /// <summary>
    /// Represents a dictionary that can be associated with the cache items to provide extra information so that items
    /// are grouped together and can be queried efficiently based on the provided information. 
    /// </summary>
    /// <remark>
    /// This Feature is Not Available in Express
    /// One dictionary can be associated with each cache item that might contain multiple named tags. 
    /// </remarks>
    /// <example>
    /// To create an instance of NamedTagsDictionary class and populate it you can use code as follows:
    /// <code>
    /// NamedTagsDictionary namedTags = new NamedTagsDictionary();
    /// namedTags.Add("myInteger", 10);
    /// namedTags.Add("myString", "hello");
    /// </code>
    /// </example>
    public class NamedTagsDictionary
    {
        private Hashtable namedTags;

        public NamedTagsDictionary()
        {
            this.namedTags = new Hashtable();
        }

        public void Add(string key, int value)
        {
            this.namedTags.Add(key, value);
        }
        
        public void Add(string key, long value) 
        {
            this.namedTags.Add(key, value);
        }
        
        public void Add(string key, float value) 
        {
            this.namedTags.Add(key, value);
        }
        
        public void Add(string key, double value) 
        {
            this.namedTags.Add(key, value);
        }

        public void Add(string key, decimal value) 
        {
            this.namedTags.Add(key, value);
        }

        public void Add(string key, string value) 
        {
            if (value == null)
                throw new  ArgumentNullException("value");

            this.namedTags.Add(key, value);
        }

        public void Add(string key, char value)
        {
            this.namedTags.Add(key, value);
        }

        public void Add(string key, bool value)
        {
            this.namedTags.Add(key, value);
        }

        public void Add(string key, DateTime value) 
        {
            this.namedTags.Add(key, value);
        }

        public void Remove(string key)
        {
            this.namedTags.Remove(key);
        }

        public int Count
        {
            get
            {
                return this.namedTags.Count;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return this.namedTags.GetEnumerator();
        }

        public bool Contains(string key)
        {
            return this.namedTags.ContainsKey(key);
        }


    }

}
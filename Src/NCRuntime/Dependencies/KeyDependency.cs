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
using Alachisoft.NCache.Runtime.Exceptions;

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.Dependencies
#else
namespace Alachisoft.NCache.Runtime.Dependencies
#endif
{
    [Serializable]
    public class KeyDependency : CacheDependency
    {
        /// <summary> keys the dependency is based upon. </summary>
        /// <remark>
        /// This Feature is Not Available in Express
        /// </remark>
        private string[] _cacheKeys;
        /// <summary> </summary>
        private long _startAfterTicks;


        /// <summary>
        /// Initializes a new instance of the KeyExpiration class that monitors an array of 
        /// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>
        public KeyDependency(string key)
            : this(new string[] { key }, DateTime.Now)
        {
        }

        /// <summary>
        /// Initializes a new instance of the KeyExpiration class that monitors an array of 
        /// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>
        public KeyDependency(string key, DateTime startAfter)
            : this(new string[] { key }, startAfter)
        {
        }

        /// <summary>
        /// Initializes a new instance of the KeyExpiration class that monitors an array of 
        /// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>
        public KeyDependency(string[] keys)
            : this(keys, DateTime.Now)
        {
        }
        public bool FindNull(string[] keys)
        {
            bool isKeyNull = false; 
            foreach (string key in keys)
            {
                if (key==null)
                    isKeyNull = true;
            }
            return isKeyNull;
        }

        /// <summary>
        /// Initializes a new instance of the KeyExpiration class that monitors an array of 
        /// file paths (to files or directories), an array of cache keys, or both for changes.
        /// </summary>
        public KeyDependency(string[] keys, DateTime startAfter)
        {
            if (keys.Length > 1)
            {
                _cacheKeys = this.GetDistinctKeys(keys);
            }
            else
            {
                _cacheKeys = keys;
            }
            if (FindNull(keys))
                throw new ArgumentException("key can not be null.");
            _startAfterTicks = startAfter.Ticks;
        }

        /// <summary>
        /// Return array of cache keys
        /// </summary>
        public string[] CacheKeys
        {
            get { return _cacheKeys; }
        }

        /// <summary>
        /// Return array of cache keys
        /// </summary>
        public long StartAfterTicks
        {
            get { return _startAfterTicks; }
        }

        private string[] GetDistinctKeys(string[] keys)
        {
            ArrayList tempKeysList = new ArrayList();
            for (int index = 0; index < keys.Length; index++)
            {
                if (String.IsNullOrEmpty(keys[index])) continue;
                if (!tempKeysList.Contains(keys[index]))
                    tempKeysList.Add(keys[index]);
            }
            if (tempKeysList.Count < 1)
                throw new OperationFailedException("One of the dependency key(s) does not exist.  ");
            return (string[])tempKeysList.ToArray(typeof(string));
        }

    }
}

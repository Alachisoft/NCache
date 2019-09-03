//  Copyright (c) 2019 Alachisoft
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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.DataGrouping
{
    /// <summary>
    /// This class contains info about Group and its Sub-Groups
    /// </summary>
    public class SubGroupInfoEntry:ISizableIndex
    {
        /// <summary>
        /// InMemory Size of SubGroupInfoEntry class instance which includes 
        /// _subGroupKeys ref [8 bytes],  _subGroupKeysMaxCount[8 bytes] and 16 bytes overhead of .net
        /// </summary>
        private static int SubGroupInfoEntryInMemorySize = 32; // in memory size of SubGroupInfoEntry Instance
        
        private HashVector _subGroupKeys = new HashVector();

        public SubGroupInfoEntry()
        {
        }

        public int KeysCount
        {
            get
            {
                if (_subGroupKeys != null)
                    return _subGroupKeys.Count;
                return 0;
            }
        }

        public IDictionary SubGroupData 
        {
            get { return _subGroupKeys; }
        }

        /// <summary>
        /// Add group information to the index
        /// </summary>
        /// <param name="key"></param>
        /// <param name="grpInfo"></param>
        public void AddKey(object key)
        {
            _subGroupKeys.Add(key, string.Empty);
        }

        /// <summary>
        /// Remove a specific key from a group or a sub group
        /// </summary>
        /// <param name="key"></param>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        public void RemoveKey(object key)
        {
            if (_subGroupKeys.Contains(key))
                _subGroupKeys.Remove(key);
        }

        /// <summary>
        /// Return all the keys in the group. If a sub group is specified keys
        /// related to that subGroup is returned only otherwise all the keys 
        /// included group and subgroup is returned.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        /// <returns></returns>
        public ICollection GetAllKeys()
        {
            return _subGroupKeys.Keys;           
        }

        public bool KeyExists(object key)
        {
            if (_subGroupKeys.Contains(key))
                return true;
            return false;         
        }

        public long IndexInMemorySize
        {
            get
            {
                return (SubGroupInfoEntryInMemorySize + (_subGroupKeys.BucketCount * Common.MemoryUtil.NetHashtableOverHead));
            }
        }
    }
}
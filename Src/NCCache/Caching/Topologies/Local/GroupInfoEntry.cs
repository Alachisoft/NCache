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
// limitations under the License.


using System;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.DataGrouping
{
    /// <summary>
    /// This class contains info about Group and its Sub-Groups
    /// </summary>
    public class GroupInfoEntry:ISizableIndex
    {
        /// <summary>
        /// InMemory Size of GroupInfoEntryInMemorySize class instance which includes _subGroups ref [8 bytes],  _subGroupIndexSize[8 bytes], and _subGroupsMaxCount[8 bytes] and 16 bytes overhead of .net
        /// </summary>
        private static int GroupInfoEntryInMemorySize = 40; 

        private HashVector _subGroups = new HashVector();

        private long _subGroupIndexSize;
        

        public GroupInfoEntry()
        {
        }

        public int SubGroupsCount
        {
            get
            {
                    return _subGroups.Count;
            }
        }

        /// <summary>
        /// Add group information to the index
        /// </summary>
        /// <param name="key"></param>
        /// <param name="grpInfo"></param>
        public void AddToSubGroup(object key, String subGroup)
        {
            SubGroupInfoEntry subGroupInfoEntry = null;

            if (subGroup == null) subGroup = "_DEFAULT_SUB_GRP_";

            long prevSize=0, currentSize=0;
            if (_subGroups.Contains(subGroup))
            {
                subGroupInfoEntry = (SubGroupInfoEntry)_subGroups[subGroup];
                prevSize = subGroupInfoEntry.IndexInMemorySize;
            }
            else
            {
                subGroupInfoEntry = new SubGroupInfoEntry();
                _subGroups[subGroup] = subGroupInfoEntry;

            }

            subGroupInfoEntry.AddKey(key);

            currentSize = subGroupInfoEntry.IndexInMemorySize;

            ModifyIndexInMemorySize(currentSize-prevSize);
        }

        /// <summary>
        /// Remove a specific key from a group or a sub group
        /// </summary>
        /// <param name="key"></param>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        public void RemoveFromGroup(object key, String subGroup)
        {
            SubGroupInfoEntry subGroupInfoEntry = null;
            if (subGroup == null) subGroup = "_DEFAULT_SUB_GRP_";

            long prevSize = 0, currentSize = 0;
            if (_subGroups.Contains(subGroup))
            {
                subGroupInfoEntry = (SubGroupInfoEntry)_subGroups[subGroup];
                prevSize = subGroupInfoEntry.IndexInMemorySize;

                subGroupInfoEntry.RemoveKey(key);

                currentSize = subGroupInfoEntry.IndexInMemorySize;

                if (subGroupInfoEntry.KeysCount == 0)
                {
                    _subGroups.Remove(subGroup);
                    ModifyIndexInMemorySize(-prevSize);
                }
                else 
                {
                    ModifyIndexInMemorySize(currentSize-prevSize);
                }
            }
        }


        /// <summary>
        /// returns the shallow copy of the group 
        /// </summary>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        /// <returns></returns>
        public IDictionary GetSubGroups()
        {
            return (HashVector)this._subGroups.Clone();
        }

        
        /// <summary>
        /// Return all the keys in the group. If a sub group is specified keys
        /// related to that subGroup is returned only otherwise all the keys 
        /// included group and subgroup is returned.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        /// <returns></returns>
        public ArrayList GetSubGroupKeys(string subGroup)
        {
            ArrayList list = new ArrayList();

            if (!string.IsNullOrEmpty(subGroup))
            {
                if (_subGroups.Contains(subGroup))
                {
                    SubGroupInfoEntry subGroupInfoEntry = (SubGroupInfoEntry)_subGroups[subGroup];
                    list.AddRange(subGroupInfoEntry.GetAllKeys());
                }
            }
            else
            {
                IDictionaryEnumerator ide = _subGroups.GetEnumerator();
                while (ide.MoveNext())
                {
                    SubGroupInfoEntry subGroupInfoEntry = ide.Value as SubGroupInfoEntry;
                    if (subGroupInfoEntry != null)
                        list.AddRange(subGroupInfoEntry.GetAllKeys());
                }
            }
            return list;
        }

        public bool KeyExists(object key, string subGroup)
        {
            if (subGroup != null)
            {
                if (_subGroups.Contains(subGroup))
                {
                    SubGroupInfoEntry subGroupInfoEntry = (SubGroupInfoEntry)_subGroups[subGroup];
                    return subGroupInfoEntry.KeyExists(key);                   
                }
                return false;
            }
            else
            {
                IDictionaryEnumerator ide = _subGroups.GetEnumerator();
                while (ide.MoveNext())
                {
                    if (((SubGroupInfoEntry)ide.Value).KeyExists(key))
                        return true;
                }
                return false;
            }
        }

        public long IndexInMemorySize
        {
            get 
            {
                return (_subGroupIndexSize + (_subGroups.BucketCount * Common.MemoryUtil.NetHashtableOverHead));
            }
        }

        private void ModifyIndexInMemorySize(long size)
        {
            _subGroupIndexSize += size;
        }
    }
}
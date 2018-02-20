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


using System.Collections;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.DataGrouping
{
    /// <summary>
    /// This class manages the indexes for data groups
    /// </summary>
    public class GroupIndexManager : ISizableIndex
    {

        private HashVector _groups = new HashVector();

        private long _groupIndexSize;


        public GroupIndexManager()
        {
        }

        /// <summary>
        /// Does trimming of Group or SubGroup
        /// </summary>
        /// <param name="gOrSg"></param>
        /// <returns></returns>
        private string DoTrimming(string gOrSg)
        {
            if (!string.IsNullOrEmpty(gOrSg))
                return gOrSg.Trim();
            return gOrSg;
        }

        /// <summary>
        /// Add group information to the index
        /// </summary>
        /// <param name="key"></param>
        /// <param name="grpInfo"></param>
        public void AddToGroup(object key, GroupInfo grpInfo)
        {
            if (grpInfo == null) return;

            string group = grpInfo.Group;
            string subGroup = grpInfo.SubGroup;

            if (group == null) return;
            group = DoTrimming(group);
            if (subGroup != null) subGroup = DoTrimming(subGroup);

            lock (_groups.SyncRoot)
            {
                long prevSize = 0, currentSize = 0;

                if (_groups.Contains(group))
                {
                    GroupInfoEntry entry = (GroupInfoEntry)_groups[group];
                    
                    prevSize = entry.IndexInMemorySize;

                    entry.AddToSubGroup(key, subGroup);

                    currentSize = entry.IndexInMemorySize;
                }
                else
                {
                    GroupInfoEntry entry = new GroupInfoEntry();
                    entry.AddToSubGroup(key, subGroup);
                    _groups[group] = entry;
                    
                    currentSize = entry.IndexInMemorySize;
                }

                ModifyGroupIndexSize(currentSize - prevSize);
            }
        }

        /// <summary>
        /// Remove a specific key from a group or a sub group
        /// </summary>
        /// <param name="key"></param>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        public void RemoveFromGroup(object key, GroupInfo grpInfo)
        {
            if (grpInfo == null) return;
            string group = grpInfo.Group;
            string subGroup = grpInfo.SubGroup;
            if (group == null) return;
            
            group = DoTrimming(group);
            subGroup = DoTrimming(subGroup);

            lock (_groups.SyncRoot)
            {
                if (_groups.Contains(group))
                {
                    GroupInfoEntry entry = (GroupInfoEntry)_groups[group];
                    
                    long prevSize = entry.IndexInMemorySize;

                    entry.RemoveFromGroup(key, subGroup);

                    long currentSize = entry.IndexInMemorySize;

                    if (entry.SubGroupsCount == 0)
                    {
                        _groups.Remove(group);
                        ModifyGroupIndexSize(-prevSize); //in case the group is being removed we will removed the whole size of the group from our groupindex size
                    }
                    else 
                    {
                        ModifyGroupIndexSize(currentSize-prevSize);
                    }
                }
            }
        }


        /// <summary>
        /// returns the shallow copy of the group 
        /// </summary>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        /// <returns></returns>
        public IDictionary GetGroup(string group, string subGroup)
        {
            if (group == null) return null;

            lock (_groups.SyncRoot)
            {
                group = DoTrimming(group);
                if (_groups.Contains(group))
                {
                    GroupInfoEntry entry = (GroupInfoEntry)_groups[group];
                    return entry.GetSubGroups();                  
                }
            }

            return null;
        }
        
        /// <summary>
        /// Return all the keys in the group. If a sub group is specified keys
        /// related to that subGroup is returned only otherwise all the keys 
        /// included group and subgroup is returned.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="subGroup"></param>
        /// <returns></returns>
        public ArrayList GetGroupKeys(string group, string subGroup)
        {
            if (group == null) return null;

            lock (_groups.SyncRoot)
            {
                group = DoTrimming(group);
                subGroup = DoTrimming(subGroup);

                if (_groups.Contains(group))
                {
                    GroupInfoEntry entry = (GroupInfoEntry)_groups[group];
                    return entry.GetSubGroupKeys(subGroup);
                }
            }
            return new ArrayList();
        }

        /// <summary>
        /// Checks whether a data groups exists or not.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool GroupExists(string group)
        {
            group = DoTrimming(group);
            return _groups != null ? _groups.Contains(group) : false;
        }


        public bool KeyExists(object key, string group, string subGroup)
        {
            group = DoTrimming(group);
            subGroup = DoTrimming(subGroup);

            if (GroupExists(group))
            {
                GroupInfoEntry entry = (GroupInfoEntry)_groups[group];
                return entry.KeyExists(key, subGroup);              
            }
            return false;
        }

        /// <summary>
        /// Gets the list of data groups.
        /// </summary>
        public ArrayList DataGroupList
        {
            get
            {
                if (_groups != null)
                {
                    lock (_groups.SyncRoot)
                    {
                        ArrayList list = new ArrayList();
                        IDictionaryEnumerator ide = _groups.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            list.Add(ide.Key);
                        }
                        return list;
                    }
                }
                return null;
            }
        }
        /// <summary>
        /// Clear all the keys from index
        /// </summary>
        public void Clear()
        {
            if (_groups != null)
            {
                lock (_groups.SyncRoot)
                {
                    _groups = new HashVector();
                    _groupIndexSize = 0;
                }
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public long IndexInMemorySize
        {
            get 
            {
                return _groupIndexSize + (_groups.BucketCount * Common.MemoryUtil.NetHashtableOverHead);
            }
        }

        private void ModifyGroupIndexSize(long change) 
        {
            _groupIndexSize += change;
        }
    }
}

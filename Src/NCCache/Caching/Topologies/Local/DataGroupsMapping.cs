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
using System;
using System.Collections;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.DataGrouping
{
    internal class DataGroupsMapping : ICloneable
    {
        private HashVector _groupMap = new HashVector();

        public DataGroupsMapping() { }

        public DataGroupsMapping(string groupList)
        {
            Initialize(groupList);
        }

        public void Initialize(string groupList)
        {
            if (groupList != null)
            {
                string[] gList = groupList.Split(new char[] { ',' });

                if (gList != null)
                {
                    for (int i = 0; i < gList.Length; i++)
                    {
                        if (_groupMap.Contains(gList[i]))
                            _groupMap.Add(gList[i], null);
                    }
                }
            }
        }

        public bool HasDatGroup(string group)
        {
            return _groupMap.Contains(group);
        }

        public ICollection GroupList
        {
            get
            {
                return _groupMap.Keys;
            }
        }

        public void AddDataGroup(string group, Address node)
        {
            if (group == null) return;

            ArrayList nodeList = null;
            if (node != null)
            {
                if (_groupMap.Contains(group))
                    nodeList = (ArrayList)_groupMap[group];
                else
                    nodeList = new ArrayList();

                nodeList.Add(node);
            }

            _groupMap[group] = nodeList;
        }

        public void AddDataGroup(DataAffinity affinity, Address node)
        {
            if (affinity == null || node == null) return;
            if (affinity.Groups != null)
            {
                foreach (string group in affinity.Groups)
                {
                    AddDataGroup(group, node);
                }
            }
        }

        #region ICloneable Members

        public object Clone()
        {
            return null;
        }

        #endregion
    }
}
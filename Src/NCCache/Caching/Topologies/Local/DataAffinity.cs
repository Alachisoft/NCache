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
using System.Collections;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.DataGrouping
{
    /// <summary>
    /// This class is used to store data groups settings
    /// for a node in the cluster
    /// </summary>
    [Serializable]
    public class DataAffinity : ICloneable, ICompactSerializable
    {
        private ArrayList _groups = new ArrayList();
        private ArrayList _allBindedGroups = new ArrayList();
        private ArrayList _unbindedGroups = new ArrayList();
        private bool _strict;


        public DataAffinity()
        {
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="strict"></param>
        public DataAffinity(ArrayList groups, bool strict)
        {
            if (groups != null)
            {
                _groups = (ArrayList)groups.Clone();
                _groups.Sort();
            }
            _strict = strict;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="groups"></param>
        /// <param name="strict"></param>
        public DataAffinity(ArrayList groups, ArrayList allBindedGroups, ArrayList unbindGroups, bool strict)
        {
            if (groups != null)
            {
                _groups = (ArrayList)groups.Clone();
                _groups.Sort();
            }
            if (allBindedGroups != null)
            {
                _allBindedGroups = (ArrayList)allBindedGroups.Clone();
                _allBindedGroups.Sort();
            }
            if (unbindGroups != null)
            {
                _unbindedGroups = (ArrayList)unbindGroups.Clone();
                _unbindedGroups.Sort();
            }

            _strict = strict;
        }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="props"></param>
        public DataAffinity(IDictionary props)
        {
            if (props.Contains("strict"))
            {
                _strict = Convert.ToBoolean(props["strict"]);
            }

            if (props.Contains("data-groups"))
            {
                string groupsStr = (string)props["data-groups"];
                if (groupsStr.Trim().Length > 0)
                {
                    string[] groups = groupsStr.Split(new char[] { ',' });
                    ArrayList list = new ArrayList();
                    for (int i = 0; i < groups.Length; i++)
                    {
                        list.Add(groups[i]);
                    }
                    list.Sort();
                    _groups = list;
                }
            }
            if (props.Contains("binded-groups-list"))
            {
                string groupsStr = (string)props["binded-groups-list"];
                if (groupsStr.Trim().Length > 0)
                {
                    string[] groups = groupsStr.Split(new char[] { ',' });
                    ArrayList list = new ArrayList();
                    for (int i = 0; i < groups.Length; i++)
                    {
                        list.Add(groups[i]);
                    }
                    list.Sort();
                    _allBindedGroups = list;
                }
            }
        }

        /// <summary>
        /// list of groups
        /// </summary>
        public ArrayList Groups
        {
            get { return _groups == null ? null : (ArrayList)_groups.Clone(); }
            set
            {
                _groups = value;
                if (_groups != null)
                    _groups.Sort();
            }
        }

        /// <summary>
        /// list of all the binded groups
        /// </summary>
        public ArrayList AllBindedGroups
        {
            get { return _allBindedGroups == null ? null : (ArrayList)_allBindedGroups.Clone(); }
            set
            {
                _allBindedGroups = value;
                if (_allBindedGroups != null)
                    _allBindedGroups.Sort();
            }
        }

        /// <summary>
        /// list of all the groups which are not binded to any node.
        /// </summary>
        public ArrayList AllUndbindedGroups
        {
            get { return _unbindedGroups == null ? null : (ArrayList)_unbindedGroups.Clone(); }
            set
            {
                _unbindedGroups = value;
                if (_unbindedGroups != null)
                    _unbindedGroups.Sort();
            }
        }
        /// <summary>
        /// Allow data without any group or not
        /// </summary>
        public bool Strict
        {
            get { return _strict; }
        }

        /// <summary>
        /// Is the specified group exists in the list
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool IsExists(string group)
        {
            if (group == null) return false;
            if (_groups == null) return false;
            if (_groups.BinarySearch(group) < 0)
                return false;
            return true;
        }

        /// <summary>
        /// Determine whether the spceified group exist in unbinded groups list or not.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool IsUnbindedGroups(string group)
        {
            if (group == null) return false;
            if (_unbindedGroups == null) return false;
            if (_unbindedGroups.BinarySearch(group) < 0)
                return false;
            return true;
        }

        #region ICloneable Members

        public object Clone()
        {
            return new DataAffinity(_groups, _allBindedGroups, _unbindedGroups, _strict);
        }

        #endregion

        #region ICompactSerializable Members

        public void Serialize(CompactWriter writer)
        {
            writer.Write(_strict);
            writer.WriteObject(_groups);
            writer.WriteObject(_allBindedGroups);
            writer.WriteObject(_unbindedGroups);

        }

        public void Deserialize(CompactReader reader)
        {
            _strict = (bool)reader.ReadBoolean();
            _groups = (ArrayList)reader.ReadObject();
            _allBindedGroups = (ArrayList)reader.ReadObject();
            _unbindedGroups = (ArrayList)reader.ReadObject();
        }

        #endregion

        public static DataAffinity ReadDataAffinity(CompactReader reader)
        {
            byte isNull = reader.ReadByte();
            if (isNull == 1)
                return null;
            DataAffinity newAffinity = new DataAffinity();
            newAffinity.Deserialize(reader);
            return newAffinity;
        }

        public static void WriteDataAffinity(CompactWriter writer, DataAffinity dataAffinity)
        {
            byte isNull = 1;
            if (dataAffinity == null)
                writer.Write(isNull);
            else
            {
                isNull = 0;
                writer.Write(isNull);
                dataAffinity.Serialize(writer);
            }
            return;
        }
    }
}
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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.DataGrouping
{
    /// <summary>
    /// This class contains data group information for an object
    /// </summary>
    [Serializable]
    public class GroupInfo : ICloneable, ICompactSerializable, ISizable
    {
        string _group = null;
        string _subGroup = null;

        public GroupInfo()
        { }

        public GroupInfo(string group, string subGroup)
        {
            if (!string.IsNullOrEmpty(group)) Group = group;
            SubGroup = subGroup;
        }

        public string Group
        {
            get { return _group; }
            set { if (!string.IsNullOrEmpty(value)) this._group = Common.Util.StringPool.PoolString(value); }
        }

        public string SubGroup
        {
            get { return _subGroup; }
            set {
                this._subGroup = Common.Util.StringPool.PoolString(value); }
        }

        #region ICloneable Members

        public object Clone()
        {
            return new GroupInfo(_group, _subGroup);
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _group = (string)reader.ReadObject();
            _subGroup = (string)reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_group);
            writer.WriteObject(_subGroup);
        }

        public static GroupInfo ReadGrpInfo(CompactReader reader)
        {
            byte isNull = reader.ReadByte();
            if (isNull == 1)
                return null;
            GroupInfo newInfo = new GroupInfo();
            newInfo.Deserialize(reader);
            return newInfo;
        }

        public static void WriteGrpInfo(CompactWriter writer, GroupInfo grpInfo)
        {
            byte isNull = 1;
            if (grpInfo == null)
                writer.Write(isNull);
            else
            {
                isNull = 0;
                writer.Write(isNull);
                grpInfo.Serialize(writer);
            }
            return;
        }

        #endregion

        public int Size
        {
            get
            {
                int temp = 0;
                temp += Common.MemoryUtil.NetReferenceSize; //for _group
                temp += Common.MemoryUtil.NetReferenceSize; //for _subGroup
                temp = Common.MemoryUtil.GetInMemoryInstanceSize(temp);// Get Actual Size of GroupInfo Instance 
                return temp;
            }
        }

        public int InMemorySize
        {
            get { return this.Size; }
        }
    }
}
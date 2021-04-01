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
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.Common.DataStructures
{
    public class GroupEnumerationPointer : EnumerationPointer, ICompactSerializable
    {
        private string _group;
        private string _subGroup;

        public GroupEnumerationPointer(string group, string subGroup)
            : base()
        {
            _group = group;
            _subGroup = subGroup;
        }

        public GroupEnumerationPointer(string id, int chunkId, string group, string subGroup)
            : base(id, chunkId)
        {
            _group = group;
            _subGroup = subGroup;
        }

        public string Group
        {
            get { return _group; }
            set { _group = value; }
        }

        public string SubGroup
        {
            get { return _subGroup; }
            set { _subGroup = value; }
        }

        public override bool IsGroupPointer
        {
            get { return true; }
        }

        public override bool Equals(object obj)
        {
            bool equals = false;

            if (obj is GroupEnumerationPointer)
            {
                GroupEnumerationPointer other = obj as GroupEnumerationPointer;
                if (base.Equals(obj))
                {
                    equals = object.Equals(_group, other._group) && object.Equals(_subGroup, other._subGroup);
                }
            }

            return equals;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            base.Deserialize(reader);
            _group = reader.ReadString();
            _subGroup = reader.ReadString();
        }

        void ICompactSerializable.Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_group);
            writer.Write(_subGroup);
        }

        #endregion
    }
}
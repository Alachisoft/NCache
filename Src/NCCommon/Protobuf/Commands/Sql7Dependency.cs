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

namespace Alachisoft.NCache.Common.Protobuf
{
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"Sql7Dependency")]
    public partial class Sql7Dependency : global::ProtoBuf.IExtensible
    {
        public Sql7Dependency() {}
    

        private string _connectionString = "";
        [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"connectionString", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string connectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        private string _dbCacheKey = "";
        [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"dbCacheKey", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string dbCacheKey
        {
            get { return _dbCacheKey; }
            set { _dbCacheKey = value; }
        }
        private global::ProtoBuf.IExtension extensionObject;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
}
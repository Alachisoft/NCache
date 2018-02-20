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
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"YukonDependency")]
    public partial class YukonDependency : global::ProtoBuf.IExtensible
    {
        public YukonDependency() {}
    

        private string _connectionString = "";
        [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"connectionString", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string connectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        private string _query = "";
        [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"query", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string query
        {
            get { return _query; }
            set { _query = value; }
        }

        private int _commandType = default(int);
        [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"commandType", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int commandType
        {
            get { return _commandType; }
            set { _commandType = value; }
        }
        private readonly global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.YukonParam> _param = new global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.YukonParam>();
        [global::ProtoBuf.ProtoMember(4, Name=@"param", DataFormat = global::ProtoBuf.DataFormat.Default)]
        public global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.YukonParam> param
        {
            get { return _param; }
        }
  
        private global::ProtoBuf.IExtension extensionObject;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
}
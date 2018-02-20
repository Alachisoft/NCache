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
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"OracleParam")]
    public partial class OracleParam : global::ProtoBuf.IExtensible
    {
        public OracleParam() {}
    

        private string _key = "";
        [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"key", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string key
        {
            get { return _key; }
            set { _key = value; }
        }

        private Alachisoft.NCache.Common.Protobuf.OracleCommandParam _cmdParam = null;
        [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"cmdParam", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue(null)]
        public Alachisoft.NCache.Common.Protobuf.OracleCommandParam cmdParam
        {
            get { return _cmdParam; }
            set { _cmdParam = value; }
        }
        private global::ProtoBuf.IExtension extensionObject;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
}
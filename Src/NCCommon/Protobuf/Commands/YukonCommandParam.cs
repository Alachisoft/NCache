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
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"YukonCommandParam")]
    public partial class YukonCommandParam : global::ProtoBuf.IExtensible
    {
        public YukonCommandParam() {}
    

        private int _typeId = default(int);
        [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"typeId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int typeId
        {
            get { return _typeId; }
            set { _typeId = value; }
        }

        private int _direction = default(int);
        [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"direction", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int direction
        {
            get { return _direction; }
            set { _direction = value; }
        }

        private int _dbType = default(int);
        [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"dbType", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int dbType
        {
            get { return _dbType; }
            set { _dbType = value; }
        }

        private int _cmpOptions = default(int);
        [global::ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"cmpOptions", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int cmpOptions
        {
            get { return _cmpOptions; }
            set { _cmpOptions = value; }
        }

        private int _version = default(int);
        [global::ProtoBuf.ProtoMember(5, IsRequired = false, Name=@"version", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int version
        {
            get { return _version; }
            set { _version = value; }
        }

        private string _value = "";
        [global::ProtoBuf.ProtoMember(6, IsRequired = false, Name=@"value", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string value
        {
            get { return _value; }
            set { _value = value; }
        }

        private bool _isNullable = default(bool);
        [global::ProtoBuf.ProtoMember(7, IsRequired = false, Name=@"isNullable", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue(default(bool))]
        public bool isNullable
        {
            get { return _isNullable; }
            set { _isNullable = value; }
        }

        private int _localeId = default(int);
        [global::ProtoBuf.ProtoMember(8, IsRequired = false, Name=@"localeId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int localeId
        {
            get { return _localeId; }
            set { _localeId = value; }
        }

        private int _offset = default(int);
        [global::ProtoBuf.ProtoMember(9, IsRequired = false, Name=@"offset", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        private int _precision = default(int);
        [global::ProtoBuf.ProtoMember(10, IsRequired = false, Name=@"precision", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int precision
        {
            get { return _precision; }
            set { _precision = value; }
        }

        private int _scale = default(int);
        [global::ProtoBuf.ProtoMember(11, IsRequired = false, Name=@"scale", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        private int _size = default(int);
        [global::ProtoBuf.ProtoMember(12, IsRequired = false, Name=@"size", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
        [global::System.ComponentModel.DefaultValue(default(int))]
        public int size
        {
            get { return _size; }
            set { _size = value; }
        }

        private string _sourceColumn = "";
        [global::ProtoBuf.ProtoMember(13, IsRequired = false, Name=@"sourceColumn", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string sourceColumn
        {
            get { return _sourceColumn; }
            set { _sourceColumn = value; }
        }

        private bool _sourceColumnNull = default(bool);
        [global::ProtoBuf.ProtoMember(14, IsRequired = false, Name=@"sourceColumnNull", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue(default(bool))]
        public bool sourceColumnNull
        {
            get { return _sourceColumnNull; }
            set { _sourceColumnNull = value; }
        }

        private string _sqlValue = "";
        [global::ProtoBuf.ProtoMember(15, IsRequired = false, Name=@"sqlValue", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string sqlValue
        {
            get { return _sqlValue; }
            set { _sqlValue = value; }
        }

        private string _typeName = "";
        [global::ProtoBuf.ProtoMember(16, IsRequired = false, Name=@"typeName", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string typeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }

        private string _udtTypeName = "";
        [global::ProtoBuf.ProtoMember(17, IsRequired = false, Name=@"udtTypeName", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue("")]
        public string udtTypeName
        {
            get { return _udtTypeName; }
            set { _udtTypeName = value; }
        }

        private bool _nullValueProvided = default(bool);
        [global::ProtoBuf.ProtoMember(18, IsRequired = false, Name=@"nullValueProvided", DataFormat = global::ProtoBuf.DataFormat.Default)]
        [global::System.ComponentModel.DefaultValue(default(bool))]
        public bool nullValueProvided
        {
            get { return _nullValueProvided; }
            set { _nullValueProvided = value; }
        }
        private global::ProtoBuf.IExtension extensionObject;
        global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
}
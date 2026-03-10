//  Copyright (c) 2026 Alachisoft
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
namespace Alachisoft.NCache.Common.Protobuf
{
    [global::System.Serializable, global::ProtoBuf.Extended.ProtoContract(Name=@"KeyValuePair")]
    public partial class KeyValuePair : global::ProtoBuf.Extended.IExtensible
    {
      public KeyValuePair() {}
      

    private string _key = "";
    [global::ProtoBuf.Extended.ProtoMember(1, IsRequired = false, Name=@"key", DataFormat = global::ProtoBuf.Extended.DataFormat.Default)][global::System.ComponentModel.DefaultValue("")]
    public string key
    {
      get { return _key; }
      set { _key = value; }
    }

    private string _value = "";
    [global::ProtoBuf.Extended.ProtoMember(2, IsRequired = false, Name=@"value", DataFormat = global::ProtoBuf.Extended.DataFormat.Default)][global::System.ComponentModel.DefaultValue("")]
    public string value
    {
      get { return _value; }
      set { _value = value; }
    }
      private global::ProtoBuf.Extended.IExtension extensionObject;
     global::ProtoBuf.Extended.IExtension global::ProtoBuf.Extended.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extended.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
}

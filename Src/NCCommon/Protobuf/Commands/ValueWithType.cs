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
namespace Alachisoft.NCache.Common.Protobuf
{
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"ValueWithType")]
    public partial class ValueWithType : global::ProtoBuf.IExtensible
    {
      public ValueWithType() {}
      

    private string _value = "";
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"value", DataFormat = global::ProtoBuf.DataFormat.Default)][global::System.ComponentModel.DefaultValue("")]
    public string value
    {
      get { return _value; }
      set { _value = value; }
    }

    private string _type = "";
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"type", DataFormat = global::ProtoBuf.DataFormat.Default)][global::System.ComponentModel.DefaultValue("")]
    public string type
    {
      get { return _type; }
      set { _type = value; }
    }
      private global::ProtoBuf.IExtension extensionObject;
      global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
}

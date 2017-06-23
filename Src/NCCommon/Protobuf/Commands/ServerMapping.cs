// Copyright (c) 2017 Alachisoft
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

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: ServerMapping.proto
namespace Alachisoft.NCache.Common.Protobuf
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"ServerMapping")]
  public partial class ServerMapping : global::ProtoBuf.IExtensible
  {
    public ServerMapping() {}
    

    private string _publicIp = "";
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"publicIp", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue("")]
    public string publicIp
    {
      get { return _publicIp; }
      set { _publicIp = value; }
    }

    private int _publicPort = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"publicPort", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int publicPort
    {
      get { return _publicPort; }
      set { _publicPort = value; }
    }

    private string _privateIp = "";
    [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"privateIp", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue("")]
    public string privateIp
    {
      get { return _privateIp; }
      set { _privateIp = value; }
    }

    private int _privatePort = default(int);
    [global::ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"privatePort", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int privatePort
    {
      get { return _privatePort; }
      set { _privatePort = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}

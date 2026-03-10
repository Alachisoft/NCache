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
    [global::System.Serializable, global::ProtoBuf.Extended.ProtoContract(Name=@"GetOptimalServerResponse")]
    public partial class GetOptimalServerResponse : global::ProtoBuf.Extended.IExtensible
    {
      public GetOptimalServerResponse() {}
      

    private string _server = "";
    [global::ProtoBuf.Extended.ProtoMember(1, IsRequired = false, Name=@"server", DataFormat = global::ProtoBuf.Extended.DataFormat.Default)][global::System.ComponentModel.DefaultValue("")]
    public string server
    {
      get { return _server; }
      set { _server = value; }
    }

    private int _port = default(int);
    [global::ProtoBuf.Extended.ProtoMember(2, IsRequired = false, Name=@"port", DataFormat = global::ProtoBuf.Extended.DataFormat.TwosComplement)][global::System.ComponentModel.DefaultValue(default(int))]
    public int port
    {
      get { return _port; }
      set { _port = value; }
    }
      private global::ProtoBuf.Extended.IExtension extensionObject;
     global::ProtoBuf.Extended.IExtension global::ProtoBuf.Extended.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extended.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }

    private readonly global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.KeyValuePair> _serverPublicIp = new global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.KeyValuePair>();
    [global::ProtoBuf.Extended.ProtoMember(3, Name = @"serverPublicIp", DataFormat = global::ProtoBuf.Extended.DataFormat.Default)]
    public global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.KeyValuePair> serverPublicIp
        {
        get { return _serverPublicIp; }
    }
    }
  
}

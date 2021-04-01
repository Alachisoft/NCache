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
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"DSUpdatedCallbackResponse")]
    public partial class DSUpdatedCallbackResponse : global::ProtoBuf.IExtensible
    {
      public DSUpdatedCallbackResponse() {}
      

    private int _callbackId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"callbackId", DataFormat = global::ProtoBuf.DataFormat.ZigZag)][global::System.ComponentModel.DefaultValue(default(int))]
    public int callbackId
    {
      get { return _callbackId; }
      set { _callbackId = value; }
    }

    private int _opCode = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"opCode", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)][global::System.ComponentModel.DefaultValue(default(int))]
    public int opCode
    {
      get { return _opCode; }
      set { _opCode = value; }
    }
    private readonly global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult> _result = new global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult>();
    [global::ProtoBuf.ProtoMember(3, Name=@"result", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult> result
    {
      get { return _result; }
    }
  
      private global::ProtoBuf.IExtension extensionObject;
      global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
}

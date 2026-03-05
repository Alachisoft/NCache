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
    [global::System.Serializable, global::ProtoBuf.Extended.ProtoContract(Name=@"DSUpdatedCallbackResponse")]
    public partial class DSUpdatedCallbackResponse : global::ProtoBuf.Extended.IExtensible
    {
      public DSUpdatedCallbackResponse() {}
      

    private int _callbackId = default(int);
    [global::ProtoBuf.Extended.ProtoMember(1, IsRequired = false, Name=@"callbackId", DataFormat = global::ProtoBuf.Extended.DataFormat.ZigZag)][global::System.ComponentModel.DefaultValue(default(int))]
    public int callbackId
    {
      get { return _callbackId; }
      set { _callbackId = value; }
    }

    private int _opCode = default(int);
    [global::ProtoBuf.Extended.ProtoMember(2, IsRequired = false, Name=@"opCode", DataFormat = global::ProtoBuf.Extended.DataFormat.TwosComplement)][global::System.ComponentModel.DefaultValue(default(int))]
    public int opCode
    {
      get { return _opCode; }
      set { _opCode = value; }
    }
    private readonly global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult> _result = new global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult>();
    [global::ProtoBuf.Extended.ProtoMember(3, Name=@"result", DataFormat = global::ProtoBuf.Extended.DataFormat.Default)]
    public global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult> result
    {
      get { return _result; }
    }
  
      private global::ProtoBuf.Extended.IExtension extensionObject;
     global::ProtoBuf.Extended.IExtension global::ProtoBuf.Extended.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extended.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
}

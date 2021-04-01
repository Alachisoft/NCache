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
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"UnRegisterKeyNotifCommand")]
    public partial class UnRegisterKeyNotifCommand : global::ProtoBuf.IExtensible
    {
      public UnRegisterKeyNotifCommand() {}
      

    private int _updateCallbackId = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"updateCallbackId", DataFormat = global::ProtoBuf.DataFormat.ZigZag)][global::System.ComponentModel.DefaultValue(default(int))]
    public int updateCallbackId
    {
      get { return _updateCallbackId; }
      set { _updateCallbackId = value; }
    }

    private int _removeCallbackId = default(int);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"removeCallbackId", DataFormat = global::ProtoBuf.DataFormat.ZigZag)][global::System.ComponentModel.DefaultValue(default(int))]
    public int removeCallbackId
    {
      get { return _removeCallbackId; }
      set { _removeCallbackId = value; }
    }

    private string _key = "";
    [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"key", DataFormat = global::ProtoBuf.DataFormat.Default)][global::System.ComponentModel.DefaultValue("")]
    public string key
    {
      get { return _key; }
      set { _key = value; }
    }

    private long _requestId = default(long);
    [global::ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"requestId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)][global::System.ComponentModel.DefaultValue(default(long))]
    public long requestId
    {
      get { return _requestId; }
      set { _requestId = value; }
    }
      private global::ProtoBuf.IExtension extensionObject;
      global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
}

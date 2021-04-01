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
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"GetResponse")]
  public partial class GetResponse : global::ProtoBuf.IExtensible
  {
    public GetResponse() {}
    

    private int _flag = default(int);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"flag", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int flag
    {
      get { return _flag; }
      set { _flag = value; }
    }

    private string _lockId = "";
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"lockId", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue("")]
    public string lockId
    {
      get { return _lockId; }
      set { _lockId = value; }
    }

    private long _lockTime = default(long);
    [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"lockTime", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(long))]
    public long lockTime
    {
      get { return _lockTime; }
      set { _lockTime = value; }
    }

    private ulong _version = default(ulong);
    [global::ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"version", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(ulong))]
    public ulong version
    {
      get { return _version; }
      set { _version = value; }
    }
    private readonly global::System.Collections.Generic.List<byte[]> _data = new global::System.Collections.Generic.List<byte[]>();
    [global::ProtoBuf.ProtoMember(5, Name=@"data", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public global::System.Collections.Generic.List<byte[]> data
    {
      get { return _data; }
    }
  


    private Alachisoft.NCache.Common.Protobuf.CacheItemType.ItemType _itemType = Alachisoft.NCache.Common.Protobuf.CacheItemType.ItemType.CACHEITEM;
    [global::ProtoBuf.ProtoMember(6, IsRequired = false, Name=@"itemType", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(Alachisoft.NCache.Common.Protobuf.CacheItemType.ItemType.CACHEITEM)]
    public Alachisoft.NCache.Common.Protobuf.CacheItemType.ItemType itemType
    {
      get { return _itemType; }
      set { _itemType = value; }
    }
    private long _requestId = default(long);
    [global::ProtoBuf.ProtoMember(7, IsRequired = false, Name=@"requestId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(long))]
    public long requestId
    {
      get { return _requestId; }
      set { _requestId = value; }
    }

    private int _commandID = default(int);
    [global::ProtoBuf.ProtoMember(8, IsRequired = false, Name=@"commandID", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int commandID
    {
      get { return _commandID; }
      set { _commandID = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}
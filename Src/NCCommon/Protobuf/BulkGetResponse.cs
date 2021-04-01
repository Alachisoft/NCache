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
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"BulkGetResponse")]
  public partial class BulkGetResponse : global::ProtoBuf.IExtensible
  {
    public BulkGetResponse() {}
    

    private Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse _keyValuePackage = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"keyValuePackage", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse keyValuePackage
    {
      get { return _keyValuePackage; }
      set { _keyValuePackage = value; }
    }

    private long _requestId = default(long);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"requestId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(long))]
    public long requestId
    {
      get { return _requestId; }
      set { _requestId = value; }
    }

    private int _commandID = default(int);
    [global::ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"commandID", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue(default(int))]
    public int commandID
    {
      get { return _commandID; }
      set { _commandID = value; }
    }

    private string _intendedRecipient = "";
    [global::ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"intendedRecipient", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue("")]
    public string intendedRecipient
    {
      get { return _intendedRecipient; }
      set { _intendedRecipient = value; }
    }

    private int _numberOfChuncks = (int)1;
    [global::ProtoBuf.ProtoMember(5, IsRequired = false, Name=@"numberOfChuncks", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue((int)1)]
    public int numberOfChuncks
    {
      get { return _numberOfChuncks; }
      set { _numberOfChuncks = value; }
    }

    private int _sequenceId = (int)1;
    [global::ProtoBuf.ProtoMember(6, IsRequired = false, Name=@"sequenceId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    [global::System.ComponentModel.DefaultValue((int)1)]
    public int sequenceId
    {
      get { return _sequenceId; }
      set { _sequenceId = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}
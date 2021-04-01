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
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"GetHashmapResponse")]
    public partial class GetHashmapResponse : global::ProtoBuf.IExtensible
    {
      public GetHashmapResponse() {}
      

    private long _viewId = default(long);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"viewId", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)][global::System.ComponentModel.DefaultValue(default(long))]
    public long viewId
    {
      get { return _viewId; }
      set { _viewId = value; }
    }
    private readonly global::System.Collections.Generic.List<string> _members = new global::System.Collections.Generic.List<string>();
    [global::ProtoBuf.ProtoMember(2, Name=@"members", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public global::System.Collections.Generic.List<string> members
    {
      get { return _members; }
    }
  
    private readonly global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.KeyValuePair> _keyValuePair = new global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.KeyValuePair>();
    [global::ProtoBuf.ProtoMember(3, Name=@"keyValuePair", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public global::System.Collections.Generic.List<Alachisoft.NCache.Common.Protobuf.KeyValuePair> keyValuePair
    {
      get { return _keyValuePair; }
    }
  

    private int _bucketSize = default(int);
    [global::ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"bucketSize", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)][global::System.ComponentModel.DefaultValue(default(int))]
    public int bucketSize
    {
      get { return _bucketSize; }
      set { _bucketSize = value; }
    }
      private global::ProtoBuf.IExtension extensionObject;
      global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
}

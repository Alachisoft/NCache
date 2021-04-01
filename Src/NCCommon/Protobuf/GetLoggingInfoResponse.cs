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
    [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"GetLoggingInfoResponse")]
    public partial class GetLoggingInfoResponse : global::ProtoBuf.IExtensible
    {
      public GetLoggingInfoResponse() {}
      

    private bool _errorsEnabled = default(bool);
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"errorsEnabled", DataFormat = global::ProtoBuf.DataFormat.Default)][global::System.ComponentModel.DefaultValue(default(bool))]
    public bool errorsEnabled
    {
      get { return _errorsEnabled; }
      set { _errorsEnabled = value; }
    }

    private bool _detailedErrorsEnabled = default(bool);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"detailedErrorsEnabled", DataFormat = global::ProtoBuf.DataFormat.Default)][global::System.ComponentModel.DefaultValue(default(bool))]
    public bool detailedErrorsEnabled
    {
      get { return _detailedErrorsEnabled; }
      set { _detailedErrorsEnabled = value; }
    }
      private global::ProtoBuf.IExtension extensionObject;
      global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
}

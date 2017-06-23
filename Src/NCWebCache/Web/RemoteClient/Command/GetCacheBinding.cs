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


using System.IO;
using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Web.Caching.Util;

namespace Alachisoft.NCache.Web.Command

{
    internal sealed class GetCacheBinding : CommandBase
    {
        Common.Protobuf.GetCacheBindingCommand _getCacheBinding;

        public GetCacheBinding(string id)
        {
            name = "GetCacheBinding";
            _getCacheBinding = new Common.Protobuf.GetCacheBindingCommand();
            _getCacheBinding.cacheId = id;
            
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_CACHE_BINDING; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.InternalCommand; }
        }

        protected override void CreateCommand()
        {
            _command = new Common.Protobuf.Command();
            _command.requestID = RequestId;
            _command.getCacheBindingCommand = _getCacheBinding;
            _command.type = Common.Protobuf.Command.Type.GET_CACHE_BINDING;
            _command.clientLastViewId = ClientLastViewId;
            _command.intendedRecipient = IntendedRecipient;
        }

        public override byte[] ToByte()
        {
            if (_commandBytes == null)
            {
                CreateCommand();
                SerializeCommand();
            }
            return _commandBytes;
        }

        public override void SerializeCommand()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ///Write discarding buffer that socketserver reads
                byte[] discardingBuffer = new byte[20];
                stream.Write(discardingBuffer, 0, discardingBuffer.Length);

                byte[] size = new byte[Connection.CmdSizeHolderBytesCount];
                stream.Write(size, 0, size.Length);

                ProtoBuf.Serializer.Serialize(stream, _command);
                int messageLen = (int)stream.Length - (size.Length + discardingBuffer.Length);

                size = HelperFxn.ToBytes(messageLen.ToString());
                stream.Position = discardingBuffer.Length;
                stream.Write(size, 0, size.Length);

                _commandBytes = stream.ToArray();
                stream.Close();
            }
        }

        
    }
}
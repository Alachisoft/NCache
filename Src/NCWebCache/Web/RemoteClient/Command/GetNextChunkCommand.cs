// Copyright (c) 2015 Alachisoft
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
using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Web.Command
{
    internal class GetNextChunkCommand : CommandBase
    {
        private Alachisoft.NCache.Common.Protobuf.GetNextChunkCommand _getNextChunkCommand;

        internal GetNextChunkCommand(EnumerationPointer pointer)
        {
            base.name = "GetNextChunkCommand";

            _getNextChunkCommand = new Alachisoft.NCache.Common.Protobuf.GetNextChunkCommand();
            _getNextChunkCommand.enumerationPointer = EnumerationPointerConversionUtil.ConvertToProtobufEnumerationPointer(pointer);
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getNextChunkCommand = _getNextChunkCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_NEXT_CHUNK;
            base._command.intendedRecipient = base.IntendedRecipient;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_NEXT_CHUNK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.ChunkRead; }
        }
    }
}

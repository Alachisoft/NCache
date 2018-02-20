// Copyright (c) 2018 Alachisoft
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

namespace Alachisoft.NCache.Web.Command
{
    [Serializable]
    class GetStreamLengthCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.GetStreamLengthCommand _getStreamLen;

        public GetStreamLengthCommand(string key, string lockHandle)
        {
            base.name = "GetStreamLengthCommand";
            this._getStreamLen = new Alachisoft.NCache.Common.Protobuf.GetStreamLengthCommand();
            this._getStreamLen.key = key;
            this._getStreamLen.lockHandle = lockHandle;
            this._getStreamLen.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_STREAM_LENGTH; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicRead; }
        }

        internal override bool IsKeyBased
        {
            get { return false; }
        }


        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.getStreamLengthCommand = this._getStreamLen;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_STREAM_LENGTH;
        }
    }
}
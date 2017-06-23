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

using System;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Web.Command

{
    internal sealed class AddAttributeCommand : CommandBase
    {
        private Common.Protobuf.AddAttributeCommand _addAttributeCommand;

        internal AddAttributeCommand(string key, DateTime absoluteExpiration)
        {
            name = "AddAttributeCommand";
            base.key = key;
            _addAttributeCommand = new Common.Protobuf.AddAttributeCommand();
            if (absoluteExpiration != Cache.NoAbsoluteExpiration)
                _addAttributeCommand.absExpiration = absoluteExpiration.ToUniversalTime().Ticks;

            _addAttributeCommand.key = key;
            _addAttributeCommand.requestId = RequestId;
        }

        protected override void CreateCommand()
        {
            _command = new Common.Protobuf.Command();
            _command.requestID = RequestId;
            _command.addAttributeCommand = _addAttributeCommand;
            _command.type = Common.Protobuf.Command.Type.ADD_ATTRIBUTE;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.ADD_ATTRIBUTE; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.AtomicWrite; }
        }
    }
}

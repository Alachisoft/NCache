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
using System.Text;

using Alachisoft.NCache.Common;
using System.IO;

using Alachisoft.NCache.Web.Communication;
using Alachisoft.NCache.Common.Protobuf.Util;
using Alachisoft.NCache.Web.Caching.Util;



namespace Alachisoft.NCache.Web.Command
{
    internal sealed class BulkRemoveCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.BulkRemoveCommand _bulkRemoveCommand;

        public BulkRemoveCommand(string[] keys, BitSet flagMap)
        {
            base.name = "BulkRemoveCommand";
            base.BulkKeys = keys;
            _bulkRemoveCommand = new Alachisoft.NCache.Common.Protobuf.BulkRemoveCommand();
            _bulkRemoveCommand.keys.AddRange(keys);
            _bulkRemoveCommand.flag = flagMap.Data;
            _bulkRemoveCommand.requestId = base.RequestId;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.REMOVE_BULK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.BulkWrite; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.bulkRemoveCommand = _bulkRemoveCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.REMOVE_BULK;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.intendedRecipient = base.IntendedRecipient;
            base._command.commandVersion = 1;
        }
    }
}

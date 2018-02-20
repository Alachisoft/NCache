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

using System.Collections.Generic;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Web.Command
{
    internal sealed class BulkGetCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.BulkGetCommand _bulkGetCommand;
        private int _methodOverload;

        public BulkGetCommand(string[] keys, BitSet flagMap, string provider, int methodOverload)
        {
            base.name = "BulkGetCommand";
            base.BulkKeys = keys;
            _bulkGetCommand = new Alachisoft.NCache.Common.Protobuf.BulkGetCommand();
            _bulkGetCommand.keys.AddRange(keys);
            _bulkGetCommand.providerName = provider;
            _bulkGetCommand.flag = flagMap.Data;
            _bulkGetCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_BULK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkRead; }
        }

        protected override void CreateCommand()
        {
            base._command = new Alachisoft.NCache.Common.Protobuf.Command();
            base._command.requestID = base.RequestId;
            base._command.bulkGetCommand = _bulkGetCommand;
            base._command.type = Alachisoft.NCache.Common.Protobuf.Command.Type.GET_BULK;
            base._command.clientLastViewId = base.ClientLastViewId;
            base._command.intendedRecipient = base.IntendedRecipient;
            base._command.commandVersion = 1; // NCache 4.1 Onwards
            base._command.MethodOverload = _methodOverload;
        }

        protected override CommandBase GetMergedCommand(List<CommandBase> commands)
        {
            BulkGetCommand mergedCommand = null;
            if (commands != null || commands.Count > 0)
            {
                foreach (CommandBase command in commands)
                {
                    if (command is BulkGetCommand)
                    {
                        BulkGetCommand bulkCommand = (BulkGetCommand) command;
                        if (mergedCommand == null)
                        {
                            mergedCommand = bulkCommand;
                        }
                        else
                        {
                            mergedCommand._bulkGetCommand.keys.AddRange(bulkCommand._bulkGetCommand.keys);
                        }
                    }
                }
            }

            return mergedCommand;
        }
    }
}
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

using Alachisoft.NCache.Common;
using System.Collections.Generic;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class BulkRemoveCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.BulkRemoveCommand _bulkRemoveCommand;
        private int _methodOverload;

        public BulkRemoveCommand(string[] keys, BitSet flagMap, string providerName, short onDsItemRemovedId, int methodOverload)
        {
            base.name = "BulkRemoveCommand";
            base.BulkKeys = keys;
            _bulkRemoveCommand = new Alachisoft.NCache.Common.Protobuf.BulkRemoveCommand();
            _bulkRemoveCommand.keys.AddRange(keys);
            _bulkRemoveCommand.datasourceItemRemovedCallbackId = onDsItemRemovedId;
            _bulkRemoveCommand.flag = flagMap.Data;
            _bulkRemoveCommand.requestId = base.RequestId;
            _bulkRemoveCommand.providerName = providerName;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.REMOVE_BULK; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkWrite; }
        }

        internal override bool IsSafe { get { return false; } }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _bulkRemoveCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.REMOVE_BULK;
        }

        protected override void CreateCommand()
        {

            
            _bulkRemoveCommand.requestId = base.RequestId;
            _bulkRemoveCommand.clientLastViewId = base.ClientLastViewId;
            _bulkRemoveCommand.intendedRecipient = base.IntendedRecipient;
            _bulkRemoveCommand.commandVersion = 1;
            _bulkRemoveCommand.MethodOverload = _methodOverload;
        }

        protected override CommandBase GetMergedCommand(List<CommandBase> commands)
        {
            BulkRemoveCommand mergedCommand = null;
            if (commands != null || commands.Count > 0)
            {
                foreach (CommandBase command in commands)
                {
                    if (command is BulkRemoveCommand)
                    {
                        BulkRemoveCommand bulkCommand = (BulkRemoveCommand)command;
                        if (mergedCommand == null)
                        {
                            mergedCommand = bulkCommand;
                        }
                        else
                        {
                            mergedCommand._bulkRemoveCommand.keys.AddRange(bulkCommand._bulkRemoveCommand.keys);
                        }
                    }
                }
            }
            return mergedCommand;
        }
    }
}

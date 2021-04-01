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
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections.Generic;
using System.IO;

namespace Alachisoft.NCache.Client
{
    internal sealed class BulkGetCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.BulkGetCommand _bulkGetCommand;
        private int _methodOverload;

        public BulkGetCommand(string[] keys, BitSet flagMap,  int methodOverload)
        {
            base.name = "BulkGetCommand";
            base.BulkKeys = keys;
            _bulkGetCommand = new Alachisoft.NCache.Common.Protobuf.BulkGetCommand();
            _bulkGetCommand.keys.AddRange(keys);
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

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _bulkGetCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.GET_BULK;
        }

        protected override void CreateCommand()
        {

            _bulkGetCommand.commandID = this._commandID;
            _bulkGetCommand.requestId = base.RequestId;
            _bulkGetCommand.clientLastViewId = base.ClientLastViewId;
            _bulkGetCommand.intendedRecipient = base.IntendedRecipient;
            _bulkGetCommand.commandVersion = 1; // NCache 4.1 Onwards
            _bulkGetCommand.MethodOverload = _methodOverload;


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
                        BulkGetCommand bulkCommand = (BulkGetCommand)command;
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

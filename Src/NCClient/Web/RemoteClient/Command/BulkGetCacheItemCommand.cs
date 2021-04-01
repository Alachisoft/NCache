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
    internal sealed class BulkGetCacheItemCommand : CommandBase
    {
        Alachisoft.NCache.Common.Protobuf.BulkGetCacheItemCommand _bulkGetCacheItemCommand;
        private int _methodOverload;

        public BulkGetCacheItemCommand(string[] keys, BitSet flagMap, int methodOverload)
        {
            base.name = "BulkGetCacheItemCommand";
            base.BulkKeys = keys;
            _bulkGetCacheItemCommand = new Alachisoft.NCache.Common.Protobuf.BulkGetCacheItemCommand();
            _bulkGetCacheItemCommand.keys.AddRange(keys);
            _bulkGetCacheItemCommand.flag = flagMap.Data;
            _bulkGetCacheItemCommand.requestId = base.RequestId;
            _methodOverload = methodOverload;
        }

        internal override CommandType CommandType
        {
            get { return CommandType.GET_BULK_CACHEITEM; }
        }

        internal override RequestType CommandRequestType
        {
            get { return RequestType.KeyBulkRead; }
        }

        protected override void SerializeCommandInternal(Stream stream)
        {
            ProtoBuf.Serializer.Serialize(stream, _bulkGetCacheItemCommand);
        }

        protected override short GetCommandHandle()
        {
            return (short)Common.Protobuf.Command.Type.GET_BULK_CACHEITEM;
        }

        protected override void CreateCommand()
        {
            _bulkGetCacheItemCommand.requestId = base.RequestId;
            _bulkGetCacheItemCommand.clientLastViewId = base.ClientLastViewId;
            _bulkGetCacheItemCommand.intendedRecipient = base.IntendedRecipient;
            _bulkGetCacheItemCommand.commandVersion = 1; // NCache 4.1 Onwards
            _bulkGetCacheItemCommand.MethodOverload = _methodOverload;
        }

        protected override CommandBase GetMergedCommand(List<CommandBase> commands)
        {
            BulkGetCacheItemCommand mergedCommand = null;
            if (commands != null || commands.Count > 0)
            {
                foreach (CommandBase command in commands)
                {
                    if (command is BulkGetCacheItemCommand)
                    {
                        BulkGetCacheItemCommand bulkCommand = (BulkGetCacheItemCommand)command;
                        if (mergedCommand == null)
                        {
                            mergedCommand = bulkCommand;
                        }
                        else
                        {
                            mergedCommand._bulkGetCacheItemCommand.keys.AddRange(bulkCommand._bulkGetCacheItemCommand.keys);
                        }
                    }
                }
            }
            return mergedCommand;
        }
    }
}

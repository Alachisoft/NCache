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
using System.Collections;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkAddAndInsertCommandBase : CommandBase
    {
        protected struct CommandInfo
        {
            public int PackageSize;

            public CacheEntry[] Entries;

            public string RequestId;
            public string[] Keys;
            public BitSet Flag;

            public long ClientLastViewId;
            public string IntendedRecipient;
        }

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        public override bool IsBulkOperation
        {
            get
            {
                return true;
            }
        }

        internal static string NC_NULL_VAL = "NLV";
        protected string serailizationContext;

        private string _clientId = string.Empty; 

        public string ClientId
        {
            get { return _clientId; }
            set { _clientId = value; }
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
        }

        //PROTOBUF
        protected CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager, string cacheId)
        {
            CommandInfo cmdInfo = new CommandInfo();
            int packageSize = 0;
            int index = 0;
            string version = string.Empty;
            Hashtable queryInfoHashtable = null;

            switch (command.type)
            {
                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_BULK:
                    Alachisoft.NCache.Common.Protobuf.BulkAddCommand bulkAddCommand = command.bulkAddCommand;

                    packageSize = bulkAddCommand.addCommand.Count;

                    cmdInfo.Keys = new string[packageSize];
                    cmdInfo.Entries = new CacheEntry[packageSize];

                    cmdInfo.RequestId = bulkAddCommand.requestId.ToString();
                    cmdInfo.ClientLastViewId = command.clientLastViewId;
                    cmdInfo.IntendedRecipient = command.intendedRecipient;
                    foreach (Alachisoft.NCache.Common.Protobuf.AddCommand addCommand in bulkAddCommand.addCommand)
                    {
                        cmdInfo.Keys[index] = addCommand.key;

                        cmdInfo.Entries[index] = new CacheEntry(UserBinaryObject.CreateUserBinaryObject(addCommand.data.ToArray()), Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(addCommand.absExpiration, addCommand.sldExpiration, serailizationContext), new PriorityEvictionHint((CacheItemPriority)addCommand.priority));
                        if (index == 0) cmdInfo.Flag = new BitSet((byte)addCommand.flag);

                        CallbackEntry cbEntry = null;
                        if ((short)addCommand.updateCallbackId != -1 || (short)addCommand.removeCallbackId != -1)
                        {
                            cbEntry = new CallbackEntry(ClientId,
                                Convert.ToInt32(cmdInfo.RequestId),
                                UserBinaryObject.CreateUserBinaryObject(addCommand.data.ToArray()),
                                (short)addCommand.removeCallbackId,
                                (short)addCommand.updateCallbackId,
                                new BitSet((byte)addCommand.flag),
                                (EventDataFilter)(addCommand.updateDataFilter != -1 ? (int)addCommand.updateDataFilter : (int)EventDataFilter.None),
                                (EventDataFilter)(addCommand.removeDataFilter != -1 ? (int)addCommand.removeDataFilter : (int)EventDataFilter.DataWithMetadata)
                                );
                            cmdInfo.Entries[index].Value = cbEntry.Clone();
                        }


                        

                        version = command.version;

                        if (string.IsNullOrEmpty(version))
                        {
                            queryInfoHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(addCommand.queryInfo);
                        }
                        else
                        {

                            ObjectQueryInfo objectQueryInfo;
                            
                            objectQueryInfo = addCommand.objectQueryInfo;

                            queryInfoHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(objectQueryInfo.queryInfo);

                        }

                        if (queryInfoHashtable != null)
                        {
                            if (cmdInfo.Entries[index].QueryInfo == null) cmdInfo.Entries[index].QueryInfo = new Hashtable();
                            cmdInfo.Entries[index].QueryInfo.Add("query-info", queryInfoHashtable);
                        }
                       


                        index++;
                    }

                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT_BULK:
                    Alachisoft.NCache.Common.Protobuf.BulkInsertCommand bulkInsertCommand = command.bulkInsertCommand;

                    packageSize = bulkInsertCommand.insertCommand.Count;

                    cmdInfo.Keys = new string[packageSize];
                    cmdInfo.Entries = new CacheEntry[packageSize];
                    cmdInfo.RequestId = bulkInsertCommand.requestId.ToString();
                    cmdInfo.ClientLastViewId = command.clientLastViewId;
                    cmdInfo.IntendedRecipient = command.intendedRecipient;
                    foreach (Alachisoft.NCache.Common.Protobuf.InsertCommand insertCommand in bulkInsertCommand.insertCommand)
                    {
                        cmdInfo.Keys[index] = insertCommand.key;
                        cmdInfo.Entries[index] = new CacheEntry(UserBinaryObject.CreateUserBinaryObject(insertCommand.data.ToArray()), Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(insertCommand.absExpiration, insertCommand.sldExpiration,  serailizationContext), new PriorityEvictionHint((CacheItemPriority)insertCommand.priority));
                        if (index == 0) cmdInfo.Flag = new BitSet((byte)insertCommand.flag);

                        CallbackEntry cbEntry = null;
                        if (insertCommand.updateCallbackId != -1 || insertCommand.removeCallbackId != -1)
                        {
                            cbEntry = new CallbackEntry(ClientId,
                                Convert.ToInt32(cmdInfo.RequestId),
                                UserBinaryObject.CreateUserBinaryObject(insertCommand.data.ToArray()),
                                 (short)insertCommand.removeCallbackId,
                                 (short)insertCommand.updateCallbackId,
                                new BitSet((byte)insertCommand.flag),
                                (EventDataFilter)(insertCommand.updateDataFilter != -1 ? (int)insertCommand.updateDataFilter : (int)EventDataFilter.None),
                                (EventDataFilter)(insertCommand.removeDataFilter != -1 ? (int)insertCommand.removeDataFilter : (int)EventDataFilter.None)
                                );
                            cmdInfo.Entries[index].Value = cbEntry.Clone();
                        }
                        
                        
                        version = command.version;

                        if (string.IsNullOrEmpty(version))
                        {
                            queryInfoHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(insertCommand.queryInfo);
                           
                        }
                        else
                        {
                            ObjectQueryInfo objectQueryInfo;

                            objectQueryInfo = insertCommand.objectQueryInfo;
                            queryInfoHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(objectQueryInfo.queryInfo);
                            
                        }

                        if (queryInfoHashtable != null)
                        {
                            if (cmdInfo.Entries[index].QueryInfo == null) cmdInfo.Entries[index].QueryInfo = new Hashtable();
                            cmdInfo.Entries[index].QueryInfo.Add("query-info", queryInfoHashtable);
                        }

                        index++;
                    }

                    break;
            }

            return cmdInfo;
        }
    }
}

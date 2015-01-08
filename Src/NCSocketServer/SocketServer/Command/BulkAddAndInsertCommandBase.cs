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

            public object[] Values;

            public string RequestId;
            public string[] Keys;
            public short[] RemoveCallbackId;
            public int[] RemoveDataFilter;
            public short[] UpdateCallbackId;
            public int[] UpdateDataFilter;

            public ExpirationHint[] ExpirationHint;
            public PriorityEvictionHint[] EvictionHint;

            public CallbackEntry[] CallbackEnteries;

            public Hashtable[] QueryInfo;
            public BitSet[] Flags;

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

            switch (command.type)
            {
                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_BULK:
                    Alachisoft.NCache.Common.Protobuf.BulkAddCommand bulkAddCommand = command.bulkAddCommand;

                    packageSize = bulkAddCommand.addCommand.Count;

                    cmdInfo.Keys = new string[packageSize];
                    cmdInfo.UpdateCallbackId = new short[packageSize];
                    cmdInfo.UpdateDataFilter = new int[packageSize];
                    cmdInfo.RemoveCallbackId = new short[packageSize];
                    cmdInfo.RemoveDataFilter = new int[packageSize];
                    cmdInfo.CallbackEnteries = new CallbackEntry[packageSize];
                    cmdInfo.EvictionHint = new PriorityEvictionHint[packageSize];
                    cmdInfo.ExpirationHint = new ExpirationHint[packageSize];
                    cmdInfo.Flags = new BitSet[packageSize];
                    cmdInfo.Values = new object[packageSize];
                    cmdInfo.QueryInfo = new Hashtable[packageSize];

                    cmdInfo.RequestId = bulkAddCommand.requestId.ToString();
                    cmdInfo.ClientLastViewId = command.clientLastViewId;
                    cmdInfo.IntendedRecipient = command.intendedRecipient;
                    foreach (Alachisoft.NCache.Common.Protobuf.AddCommand addCommand in bulkAddCommand.addCommand)
                    {
                        cmdInfo.Keys[index] = addCommand.key;
                        cmdInfo.UpdateCallbackId[index] = (short)addCommand.updateCallbackId;

                        if (addCommand.updateDataFilter != -1)
                            cmdInfo.UpdateDataFilter[index] = addCommand.updateDataFilter;
                        else
                            cmdInfo.UpdateDataFilter[index] = (int) EventDataFilter.None;

                        cmdInfo.RemoveCallbackId[index] = (short)addCommand.removeCallbackId;

                        if (addCommand.removeDataFilter != -1)
                            cmdInfo.RemoveDataFilter[index] = addCommand.removeDataFilter;
                        else
                            cmdInfo.RemoveDataFilter[index] = (int)EventDataFilter.DataWithMetadata;

                        cmdInfo.EvictionHint[index] = new PriorityEvictionHint((CacheItemPriority)addCommand.priority);
                        cmdInfo.ExpirationHint[index] = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(addCommand.absExpiration, addCommand.sldExpiration, serailizationContext);
                        cmdInfo.Flags[index] = new BitSet((byte)addCommand.flag);

                        CallbackEntry cbEntry = null;
                        if (cmdInfo.UpdateCallbackId[index] != -1 || cmdInfo.RemoveCallbackId[index] != -1)
                        {
                            cbEntry = new CallbackEntry(ClientId,
                                Convert.ToInt32(cmdInfo.RequestId),
                                cmdInfo.Values[index],
                                cmdInfo.RemoveCallbackId[index],
                                cmdInfo.UpdateCallbackId[index],
                                cmdInfo.Flags[index],
                                (EventDataFilter)cmdInfo.UpdateDataFilter[index],
                                (EventDataFilter)cmdInfo.RemoveDataFilter[index]
                                );
                        }

                        cmdInfo.CallbackEnteries[index] = cbEntry;

                        Hashtable queryInfo = new Hashtable();

                        version = command.version;

                        if (string.IsNullOrEmpty(version))
                        {
                            queryInfo["query-info"] = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(addCommand.queryInfo);
                        }
                        else
                        {

                            ObjectQueryInfo objectQueryInfo;
                            
                            objectQueryInfo = addCommand.objectQueryInfo;

                            queryInfo["query-info"] = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(objectQueryInfo.queryInfo);

                        }

                        cmdInfo.QueryInfo[index] = queryInfo;
                        cmdInfo.Values[index] = UserBinaryObject.CreateUserBinaryObject(addCommand.data.ToArray());

                        index++;
                    }

                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT_BULK:
                    Alachisoft.NCache.Common.Protobuf.BulkInsertCommand bulkInsertCommand = command.bulkInsertCommand;

                    packageSize = bulkInsertCommand.insertCommand.Count;

                    cmdInfo.Keys = new string[packageSize];
                    cmdInfo.UpdateCallbackId = new short[packageSize];
                    cmdInfo.UpdateDataFilter = new int[packageSize];
                    cmdInfo.RemoveCallbackId = new short[packageSize];
                    cmdInfo.RemoveDataFilter = new int[packageSize];
                    cmdInfo.CallbackEnteries = new CallbackEntry[packageSize];
                    cmdInfo.EvictionHint = new PriorityEvictionHint[packageSize];
                    cmdInfo.ExpirationHint = new ExpirationHint[packageSize];
                    cmdInfo.Flags = new BitSet[packageSize];
                    cmdInfo.Values = new object[packageSize];
                    cmdInfo.QueryInfo = new Hashtable[packageSize];
                    cmdInfo.RequestId = bulkInsertCommand.requestId.ToString();
                    cmdInfo.ClientLastViewId = command.clientLastViewId;
                    cmdInfo.IntendedRecipient = command.intendedRecipient;
                    foreach (Alachisoft.NCache.Common.Protobuf.InsertCommand insertCommand in bulkInsertCommand.insertCommand)
                    {
                        cmdInfo.Keys[index] = insertCommand.key;
                        cmdInfo.UpdateCallbackId[index] = (short)insertCommand.updateCallbackId;
                        cmdInfo.RemoveCallbackId[index] = (short)insertCommand.removeCallbackId;

                        if (insertCommand.updateDataFilter != -1)
                            cmdInfo.UpdateDataFilter[index] = insertCommand.updateDataFilter;
                        else
                            cmdInfo.UpdateDataFilter[index] = (int)EventDataFilter.None;
                        //for old clients eventdata filter will be missing
                        if (insertCommand.removeDataFilter != -1)
                            cmdInfo.RemoveDataFilter[index] = insertCommand.removeDataFilter;
                        else
                            cmdInfo.RemoveDataFilter[index] = (int) EventDataFilter.DataWithMetadata;
                        
                        cmdInfo.EvictionHint[index] = new PriorityEvictionHint((CacheItemPriority)insertCommand.priority);
                        cmdInfo.ExpirationHint[index] = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(insertCommand.absExpiration, insertCommand.sldExpiration, serailizationContext);
                        cmdInfo.Flags[index] = new BitSet((byte)insertCommand.flag);

                        CallbackEntry cbEntry = null;
                        if (cmdInfo.UpdateCallbackId[index] != -1 || cmdInfo.RemoveCallbackId[index] != -1)
                        {
                            cbEntry = new CallbackEntry(ClientId,
                                Convert.ToInt32(cmdInfo.RequestId),
                                cmdInfo.Values[index],
                                cmdInfo.RemoveCallbackId[index],
                                cmdInfo.UpdateCallbackId[index],
                                cmdInfo.Flags[index],
                                (EventDataFilter)cmdInfo.UpdateDataFilter[index],
                                (EventDataFilter)cmdInfo.RemoveDataFilter[index]
                                );
                        }

                        cmdInfo.CallbackEnteries[index] = cbEntry;


                        Hashtable queryInfo = new Hashtable();
                        version = command.version;

                        if (string.IsNullOrEmpty(version))
                        {
                            queryInfo["query-info"] = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(insertCommand.queryInfo);
                           
                        }
                        else
                        {
                            ObjectQueryInfo objectQueryInfo;

                            objectQueryInfo = insertCommand.objectQueryInfo;
                            queryInfo["query-info"] = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(objectQueryInfo.queryInfo);
                            
                        }

                        cmdInfo.QueryInfo[index] = queryInfo;
                        cmdInfo.Values[index] = UserBinaryObject.CreateUserBinaryObject(insertCommand.data.ToArray());

                        index++;
                    }

                    break;
            }

            return cmdInfo;
        }
    }
}

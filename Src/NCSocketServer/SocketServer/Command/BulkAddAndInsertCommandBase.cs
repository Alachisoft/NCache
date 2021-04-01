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
using System;
using System.Collections;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.DataGrouping;
using Alachisoft.NCache.Runtime;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Protobuf;
using System.IO;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Pooling;
using Alachisoft.NCache.Runtime.Events;


namespace Alachisoft.NCache.SocketServer.Command
{
    class BulkAddAndInsertCommandBase : CommandBase
    {
        protected struct CommandInfo
        {
            public int PackageSize;

            public CacheEntry[] Entries;

            public long RequestId;
            public string ProviderName;
            public string[] Keys;
            public string Group;
            public string SubGroup;
            public short OnDsItemsAddedCallback;
            public short onUpdateCallbackId;
            public bool returnVersion;
        
            public BitSet Flag;
           

            public long ClientLastViewId;
            public string IntendedRecipient;

            public string ClientID;
            public string type;
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


        protected CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager, string cacheId)
        {
            CommandInfo cmdInfo = new CommandInfo();
            int packageSize = 0;
            int index = 0;
            string version = string.Empty;
            NCache nCache = clientManager.CmdExecuter as NCache;
            Caching.Cache cache = nCache.Cache;
            Hashtable queryInfoHashtable = null;
            Hashtable tagHashtable = null;
            Hashtable namedTagHashtable = null;
            try
            {
                switch (command.type)
                {
                    case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD_BULK:
                        Alachisoft.NCache.Common.Protobuf.BulkAddCommand bulkAddCommand = command.bulkAddCommand;
                        packageSize = bulkAddCommand.addCommand.Count;

                        cmdInfo.Keys = new string[packageSize];
                        cmdInfo.Entries = new CacheEntry[packageSize];
                        cmdInfo.OnDsItemsAddedCallback = (short)bulkAddCommand.datasourceItemAddedCallbackId;
                        cmdInfo.ProviderName = bulkAddCommand.providerName.Length == 0 ? null : bulkAddCommand.providerName;
                        cmdInfo.RequestId = bulkAddCommand.requestId;
                        cmdInfo.ClientLastViewId = command.clientLastViewId;
                        cmdInfo.IntendedRecipient = command.intendedRecipient;

                        cmdInfo.returnVersion = bulkAddCommand.returnVersions;

                        foreach (Alachisoft.NCache.Common.Protobuf.AddCommand addCommand in bulkAddCommand.addCommand)
                        {
                            cmdInfo.Keys[index] = addCommand.key;
                            cmdInfo.ClientID = addCommand.clientID;

                            BitSet flag = BitSet.CreateAndMarkInUse(clientManager.CacheFakePool, NCModulesConstants.SocketServer);
                            flag.Data =((byte)addCommand.flag);
                            if (index == 0) cmdInfo.Flag = flag;

                            object value = cache.SocketServerDataService.GetCacheData(addCommand.data.ToArray(), cmdInfo.Flag);

                            cmdInfo.Entries[index] = CacheEntry.CreateCacheEntry(clientManager.CacheTransactionalPool, value, Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(clientManager.CacheTransactionalPool, null, addCommand.absExpiration, addCommand.sldExpiration, addCommand.isResync, serailizationContext),PriorityEvictionHint.Create(clientManager.CacheTransactionalPool, (CacheItemPriority)addCommand.priority));
                            cmdInfo.Entries[index].Flag.Data |= flag.Data;

                            Notifications notification = null;
                            if ((short)addCommand.updateCallbackId != -1 || (short)addCommand.removeCallbackId != -1 || cmdInfo.OnDsItemsAddedCallback != -1)
                            {
                                notification = new Notifications(!string.IsNullOrEmpty(cmdInfo.ClientID) ? cmdInfo.ClientID : clientManager.ClientID,
                                    Convert.ToInt32(cmdInfo.RequestId),
                                    (short)addCommand.removeCallbackId,
                                    (short)addCommand.updateCallbackId,
                                    (short)(cmdInfo.RequestId == -1 ? -1 : 0),
                                    cmdInfo.OnDsItemsAddedCallback,
                                    (EventDataFilter)(addCommand.updateDataFilter != -1 ? (int)addCommand.updateDataFilter : (int)EventDataFilter.None),
                                    (EventDataFilter)(addCommand.removeDataFilter != -1 ? (int)addCommand.removeDataFilter : (int)EventDataFilter.None)
                                    );
                                cmdInfo.Entries[index].Notifications = notification;
                            }

                            cmdInfo.onUpdateCallbackId = (short)addCommand.updateCallbackId;
                            if (!String.IsNullOrEmpty(cmdInfo.Group))
                            {
                                cmdInfo.Entries[index].GroupInfo = new GroupInfo(cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.type);
                            }

                            version = command.version;
                            if (queryInfoHashtable != null)
                            {
                                if (cmdInfo.Entries[index].QueryInfo == null) cmdInfo.Entries[index].QueryInfo = new Hashtable();
                                cmdInfo.Entries[index].QueryInfo.Add("query-info", queryInfoHashtable);
                            }
                           cmdInfo.Entries[index].MarkInUse(NCModulesConstants.SocketServer);
                            index++;

                            if (addCommand?.PoolManager != null)
                                addCommand.PoolManager.GetProtobufAddCommandPool()?.Return(addCommand);
                        }

                        break;

                    case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT_BULK:
                        Alachisoft.NCache.Common.Protobuf.BulkInsertCommand bulkInsertCommand = command.bulkInsertCommand;

                        packageSize = bulkInsertCommand.insertCommand.Count;

                        cmdInfo.Keys = new string[packageSize];
                        cmdInfo.Entries = new CacheEntry[packageSize];

                        cmdInfo.OnDsItemsAddedCallback = (short)bulkInsertCommand.datasourceUpdatedCallbackId;
                        cmdInfo.ProviderName = bulkInsertCommand.providerName.Length == 0 ? null : bulkInsertCommand.providerName;
                        cmdInfo.RequestId = bulkInsertCommand.requestId;
                        cmdInfo.ClientLastViewId = command.clientLastViewId;
                        cmdInfo.IntendedRecipient = command.intendedRecipient;

                        cmdInfo.returnVersion = bulkInsertCommand.returnVersions;

                        foreach (Alachisoft.NCache.Common.Protobuf.InsertCommand insertCommand in bulkInsertCommand.insertCommand)
                        {
                            cmdInfo.Keys[index] = insertCommand.key;
                            cmdInfo.ClientID = insertCommand.clientID;
                            BitSet flag = BitSet.CreateAndMarkInUse(clientManager.CacheFakePool, NCModulesConstants.SocketServer);
                            flag.Data =((byte)insertCommand.flag);
                            if (index == 0) cmdInfo.Flag = flag;
                            object value = cache.SocketServerDataService.GetCacheData(insertCommand.data.ToArray(), cmdInfo.Flag);

                            cmdInfo.Entries[index] = CacheEntry.CreateCacheEntry(clientManager.CacheTransactionalPool, value, Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(clientManager.CacheTransactionalPool, null, insertCommand.absExpiration, insertCommand.sldExpiration, insertCommand.isResync, serailizationContext), PriorityEvictionHint.Create(clientManager.CacheTransactionalPool, (CacheItemPriority)insertCommand.priority));
                            cmdInfo.Entries[index].Flag.Data |= flag.Data;

                            Notifications notification = null;
                            if (insertCommand.updateCallbackId != -1 || insertCommand.removeCallbackId != -1 || cmdInfo.OnDsItemsAddedCallback != -1)
                            {
                                notification = new Notifications(!string.IsNullOrEmpty(cmdInfo.ClientID) ? cmdInfo.ClientID : clientManager.ClientID,
                                    Convert.ToInt32(cmdInfo.RequestId),
                                     (short)insertCommand.removeCallbackId,
                                     (short)insertCommand.updateCallbackId,
                                    (short)(cmdInfo.RequestId == -1 ? -1 : 0),
                                    cmdInfo.OnDsItemsAddedCallback,
                                    (EventDataFilter)(insertCommand.updateDataFilter != -1 ? (int)insertCommand.updateDataFilter : (int)EventDataFilter.None),
                                    (EventDataFilter)(insertCommand.removeDataFilter != -1 ? (int)insertCommand.removeDataFilter : (int)EventDataFilter.None)
                                    );
                                cmdInfo.Entries[index].Notifications = notification;
                            }

                            cmdInfo.onUpdateCallbackId = (short)insertCommand.updateCallbackId;
                           
                            if (!String.IsNullOrEmpty(cmdInfo.Group))
                            {
                                cmdInfo.Entries[index].GroupInfo = new GroupInfo(cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.type);
                            }



                            version = command.version;

                            if (queryInfoHashtable != null)
                            {
                                if (cmdInfo.Entries[index].QueryInfo == null) cmdInfo.Entries[index].QueryInfo = new Hashtable();
                                cmdInfo.Entries[index].QueryInfo.Add("query-info", queryInfoHashtable);
                            }

                            if (tagHashtable != null)
                            {
                                if (cmdInfo.Entries[index].QueryInfo == null) cmdInfo.Entries[index].QueryInfo = new Hashtable();
                                cmdInfo.Entries[index].QueryInfo.Add("tag-info", tagHashtable);
                            }
                            
                            cmdInfo.Entries[index].MarkInUse(NCModulesConstants.SocketServer);
                            index++;

                            if (insertCommand?.PoolManager != null)
                                insertCommand.PoolManager.GetProtobufInsertCommandPool()?.Return(insertCommand);
                        }

                        break;
                }
            }
            finally
            {
              
            }
            return cmdInfo;
        }
        
    }
}

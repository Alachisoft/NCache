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
using System.IO;
using System.Collections;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Runtime;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.SocketServer.Pooling;
using Alachisoft.NCache.Common.Pooling;
using Runtime = Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Caching;


namespace Alachisoft.NCache.SocketServer.Command
{
    class AddAndInsertCommandBase : CommandBase
    {
        //public static StreamWriter writer = new StreamWriter("c:\\ServerPerf.log.txt");
        //public static HPTimeStats ts = new HPTimeStats();

        private readonly BitSet _bitSet;
        private readonly PriorityEvictionHint _priorityEvictionHint;

        protected struct CommandInfo
        {
            public bool DoAsync;
            public int DataFormatValue;
            public long RequestId;
            public string Key;
            public string Group;
            public string SubGroup;
            public string Type;
            public string ProviderName;
            public string ResyncProviderName;

            public BitSet Flag;
            public object LockId;
            public LockAccessType LockAccessType;

            public ExpirationHint ExpirationHint;
            public PriorityEvictionHint EvictionHint;

            public short RemoveCallbackId;
            public short UpdateCallbackId;
            public short DsItemAddedCallbackId;
            public Hashtable queryInfo;

            public ulong ItemVersion;
            public object value;

            public EventDataFilter UpdateDataFilter;
            public EventDataFilter RemoveDataFilter;

            public string ClientID;
            public int CallbackType;
        }

        internal static string NC_NULL_VAL = "NLV";
        protected string serializationContext;

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        public AddAndInsertCommandBase()
        {
            _bitSet = new BitSet();
            _priorityEvictionHint = new PriorityEvictionHint();
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {

        }

        //public override void ExecuteCommand(ClientManager clientManager, string command, byte[] data)
        //{
        //}

        //PROTOBUF
        protected virtual CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager, string cacheId)
        {
            //if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("AddInsertCmd.Parse", "enter");

            CommandInfo cmdInfo = new CommandInfo();

            Hashtable queryInfoHashtable = null;
            Hashtable tagHashtable = null;
            Hashtable namedTagHashtable = null;
            string version = string.Empty;

            NCache nCache = clientManager.CmdExecuter as NCache;
            Caching.Cache cache = nCache.Cache;
            switch (command.type)
            {
                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD:
                    Alachisoft.NCache.Common.Protobuf.AddCommand addCommand = command.addCommand;
                    if (clientManager.ClientVersion < 5000 && !clientManager.CreateEventSubscription)
                    {
                        if (addCommand.removeCallbackId != -1 || addCommand.updateCallbackId != -1)
                        {
                            Util.EventHelper.SubscribeEvents(clientManager.ClientID, TopicConstant.ItemLevelEventsTopic, nCache, null);
                            clientManager.CreateEventSubscription = true;
                        }
                    }
                    cmdInfo.Key = clientManager.CacheTransactionalPool.StringPool.GetString(addCommand.key);
                    cmdInfo.DoAsync = addCommand.isAsync;
                    cmdInfo.DsItemAddedCallbackId = (short)addCommand.datasourceItemAddedCallbackId;

                    cmdInfo.EvictionHint = _priorityEvictionHint;
                    cmdInfo.EvictionHint.Priority = (CacheItemPriority)addCommand.priority;
                    cmdInfo.ExpirationHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(clientManager.CacheTransactionalPool, null, addCommand.absExpiration, addCommand.sldExpiration, addCommand.isResync, serializationContext);

                    BitSet bitset = _bitSet;
                    bitset.Data =((byte)addCommand.flag);
                    cmdInfo.Flag = bitset;

                    cmdInfo.ProviderName = addCommand.providerName.Length == 0 ? null : addCommand.providerName;
                    cmdInfo.queryInfo = new Hashtable();

                    cmdInfo.ClientID = addCommand.clientID;
                    version = command.version;

                    if (queryInfoHashtable != null)
                    {
                        if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                        cmdInfo.queryInfo.Add("query-info", queryInfoHashtable);
                    }
                    
                    cmdInfo.RemoveCallbackId = (short)addCommand.removeCallbackId;
                    //for old clients data fitler information will be missing
                    if (addCommand.removeDataFilter != -1)
                        cmdInfo.RemoveDataFilter = (EventDataFilter)addCommand.removeDataFilter;
                    else
                        cmdInfo.RemoveDataFilter = Runtime.Events.EventDataFilter.None;

                    cmdInfo.RequestId = addCommand.requestId;
                    cmdInfo.ResyncProviderName = addCommand.resyncProviderName.Length == 0 ? null : addCommand.resyncProviderName;
                    if (addCommand.subGroup != null) cmdInfo.SubGroup = addCommand.subGroup.Length == 0 ? null : addCommand.subGroup;
                    cmdInfo.UpdateCallbackId = (short)addCommand.updateCallbackId;

                    if (addCommand.updateDataFilter != -1)
                    {
                        cmdInfo.UpdateDataFilter = (EventDataFilter)addCommand.updateDataFilter;
                    }
                    else
                        cmdInfo.UpdateDataFilter = (int)Runtime.Events.EventDataFilter.None;
                    
                    cmdInfo.value = cache.SocketServerDataService.GetCacheData(addCommand.data.ToArray(), cmdInfo.Flag);
                    try
                    {
                        for (int count = 0; count < addCommand.data.Count; count++)
                        {
                            cmdInfo.DataFormatValue = cmdInfo.DataFormatValue + addCommand.data[count].Length;
                        }
                    }
                    catch
                    {

                    }
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT:
                    Alachisoft.NCache.Common.Protobuf.InsertCommand insertCommand = command.insertCommand;
                    if (clientManager.ClientVersion < 5000 && !clientManager.CreateEventSubscription)
                    {
                        if (insertCommand.removeCallbackId != -1 || insertCommand.updateCallbackId != -1)
                        {
                            Util.EventHelper.SubscribeEvents(clientManager.ClientID, TopicConstant.ItemLevelEventsTopic, nCache, null);
                            clientManager.CreateEventSubscription = true;
                        }
                    }
                    cmdInfo.Key = clientManager.CacheTransactionalPool.StringPool.GetString(insertCommand.key);
                    cmdInfo.DoAsync = insertCommand.isAsync;
                    cmdInfo.DsItemAddedCallbackId = (short)insertCommand.datasourceUpdatedCallbackId;

                    cmdInfo.EvictionHint = _priorityEvictionHint;
                    cmdInfo.EvictionHint.Priority = (CacheItemPriority)insertCommand.priority;
                    cmdInfo.ExpirationHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(clientManager.CacheTransactionalPool, null, insertCommand.absExpiration, insertCommand.sldExpiration, insertCommand.isResync, serializationContext);

                    bitset = _bitSet;
                    bitset.Data =((byte)insertCommand.flag);
                    cmdInfo.Flag = bitset;

                    cmdInfo.ProviderName = insertCommand.providerName.Length == 0 ? null : insertCommand.providerName;
                    cmdInfo.ClientID = insertCommand.clientID;
                    cmdInfo.CallbackType = insertCommand.CallbackType;
                    version = command.version;

                    //version added in 4.2 [Dated: 18-Nov-2013; Author: Sami]

                    if (tagHashtable != null)
                    {
                        if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                        cmdInfo.queryInfo.Add("tag-info", tagHashtable);
                    }
                    
                  

                    cmdInfo.RemoveCallbackId = (short)insertCommand.removeCallbackId;
                    
                    if (insertCommand.removeDataFilter != -1)
                        cmdInfo.RemoveDataFilter = (EventDataFilter)insertCommand.removeDataFilter ;
                    else
                        cmdInfo.RemoveDataFilter = Runtime.Events.EventDataFilter.None;
                    
                    cmdInfo.RequestId = insertCommand.requestId;
                    cmdInfo.ResyncProviderName = insertCommand.resyncProviderName.Length == 0 ? null : insertCommand.resyncProviderName;
                   cmdInfo.UpdateCallbackId = (short)insertCommand.updateCallbackId;

                    if (insertCommand.updateDataFilter != -1)
                        cmdInfo.UpdateDataFilter = (EventDataFilter)insertCommand.updateDataFilter ;
                    else
                        cmdInfo.UpdateDataFilter = (int)Runtime.Events.EventDataFilter.None;

                    cmdInfo.ItemVersion = insertCommand.itemVersion;
                    cmdInfo.LockAccessType = (LockAccessType)insertCommand.lockAccessType;
                    cmdInfo.LockId = insertCommand.lockId;
                    cmdInfo.value = cache.SocketServerDataService.GetCacheData(insertCommand.data, cmdInfo.Flag);

                    try
                    {
                        for (int count = 0; count < insertCommand.data.Count; count++)
                        {
                            cmdInfo.DataFormatValue = cmdInfo.DataFormatValue + insertCommand.data[count].Length;
                        }
                    }
                    catch
                    {

                    }
                    break;
            }

            return cmdInfo;
        }

        
        #region ILeasable

        public override void ResetLeasable()
        {
            base.ResetLeasable();
            serializationContext = default;

            _bitSet.ResetLeasable();
            _priorityEvictionHint.ResetLeasable();
        }

        public override void ReturnLeasableToPool()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

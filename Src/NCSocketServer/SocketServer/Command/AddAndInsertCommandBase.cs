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

using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.CacheSynchronization;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Runtime;


using Alachisoft.NCache.Common.Protobuf;



namespace Alachisoft.NCache.SocketServer.Command
{
    class AddAndInsertCommandBase : CommandBase
    {
        protected struct CommandInfo
        {
            public bool DoAsync;
            public int DataFormatValue;
            public string RequestId;
            public string Key;
            public string Group;
            public string SubGroup;
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
            public CacheSyncDependency SyncDependency;

            public Hashtable queryInfo;

            public ulong ItemVersion;
            public object value;

            public int UpdateDataFilter;
            public int RemoveDataFilter;

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

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
        }

        //PROTOBUF
        protected virtual CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager, string cacheId)
        {
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("AddInsertCmd.Parse", "enter");

            CommandInfo cmdInfo = new CommandInfo();

            Hashtable queryInfoHashtable = null;
            Hashtable tagHashtable = null;
            Hashtable namedTagHashtable = null;
            string version = string.Empty;

            NCache nCache = clientManager.CmdExecuter as NCache;
            Caching.Cache cache = nCache.Cache;
            bool expEnabled = cache.Configuration.ExpirationPolicy.IsExpirationEnabled;
            switch (command.type)
            {
                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD:
                    Alachisoft.NCache.Common.Protobuf.AddCommand addCommand = command.addCommand;
                    cmdInfo.Key = addCommand.key;
                    cmdInfo.DoAsync = addCommand.isAsync;
                    cmdInfo.DsItemAddedCallbackId = (short)addCommand.datasourceItemAddedCallbackId;
                    cmdInfo.EvictionHint = new PriorityEvictionHint((CacheItemPriority)addCommand.priority);
                    cmdInfo.ExpirationHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(cache.Configuration.ExpirationPolicy,addCommand.dependency, addCommand.absExpiration, addCommand.sldExpiration, addCommand.isResync, serializationContext);
                    cmdInfo.Flag = new BitSet((byte)addCommand.flag);
                    if (addCommand.group != null) cmdInfo.Group = addCommand.group.Length == 0 ? null : addCommand.group;
                    cmdInfo.ProviderName = addCommand.providerName.Length == 0 ? null : addCommand.providerName;
                    cmdInfo.queryInfo = new Hashtable();

                    cmdInfo.ClientID = addCommand.clientID;
                    version = command.version;

                    // version added in 4.2 [Dated: 18-Nov-2013]
                    if (string.IsNullOrEmpty(version))
                    {
                        queryInfoHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(addCommand.queryInfo);
                        tagHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromTagInfoObj(addCommand.tagInfo);
                        if (clientManager.IsDotNetClient)
                            namedTagHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromNamedTagInfoObjFromDotNet(addCommand.namedTagInfo);
                        else
                            namedTagHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromNamedTagInfoObjFromJava(addCommand.namedTagInfo);
                    }
                    else 
                    {
                        ObjectQueryInfo objectQueryInfo = addCommand.objectQueryInfo;

                        queryInfoHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(objectQueryInfo.queryInfo);
                        tagHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromTagInfoObj(objectQueryInfo.tagInfo);
                        if (clientManager.IsDotNetClient)
                            namedTagHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromNamedTagInfoObjFromDotNet(objectQueryInfo.namedTagInfo);
                        else
                            namedTagHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromNamedTagInfoObjFromJava(objectQueryInfo.namedTagInfo); 
                    }

                    if (queryInfoHashtable != null)
                    {
                        if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                        cmdInfo.queryInfo.Add("query-info", queryInfoHashtable);
                    }

                    if (tagHashtable != null)
                    {
                        if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                        cmdInfo.queryInfo.Add("tag-info", tagHashtable);
                    }

                    if (namedTagHashtable != null)
                    {
                            if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                            cmdInfo.queryInfo.Add("named-tag-info", namedTagHashtable);
                    }

                    cmdInfo.RemoveCallbackId = (short)addCommand.removeCallbackId;
                    // for old clients data fitler information will be missing
                    if (addCommand.removeDataFilter != -1)
                        cmdInfo.RemoveDataFilter = (int)addCommand.removeDataFilter;
                    else
                        cmdInfo.RemoveDataFilter = (int)Runtime.Events.EventDataFilter.DataWithMetadata;

                    cmdInfo.RequestId = addCommand.requestId.ToString();
                    cmdInfo.ResyncProviderName = addCommand.resyncProviderName.Length == 0 ? null : addCommand.resyncProviderName;
                    if (addCommand.subGroup != null) cmdInfo.SubGroup = addCommand.subGroup.Length == 0 ? null : addCommand.subGroup;
                    cmdInfo.SyncDependency = base.GetCacheSyncDependencyObj(addCommand.syncDependency);
                    cmdInfo.UpdateCallbackId = (short)addCommand.updateCallbackId;

                    if (addCommand.updateDataFilter != -1)
                    {
                        cmdInfo.UpdateDataFilter = addCommand.updateDataFilter;
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
                    cmdInfo.Key = insertCommand.key;
                    cmdInfo.DoAsync = insertCommand.isAsync;
                    cmdInfo.DsItemAddedCallbackId = (short)insertCommand.datasourceUpdatedCallbackId;
                    cmdInfo.EvictionHint = new PriorityEvictionHint((CacheItemPriority)insertCommand.priority);
                    cmdInfo.ExpirationHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(cache.Configuration.ExpirationPolicy, insertCommand.dependency, insertCommand.absExpiration, insertCommand.sldExpiration, insertCommand.isResync, serializationContext);
                    cmdInfo.Flag = new BitSet((byte)insertCommand.flag);
                    if (insertCommand.group != null) cmdInfo.Group = insertCommand.group.Length == 0 ? null : insertCommand.group;
                    cmdInfo.ProviderName = insertCommand.providerName.Length == 0 ? null : insertCommand.providerName;

                    cmdInfo.ClientID = insertCommand.clientID;
                    cmdInfo.CallbackType = insertCommand.CallbackType;
                    version = command.version;

                    // version added in 4.2 [Dated: 18-Nov-2013]
                    if (string.IsNullOrEmpty(version))
                    {
                        if (insertCommand.queryInfo != null)
                        {
                            queryInfoHashtable = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(insertCommand.queryInfo);
                        }

                        if (insertCommand.tagInfo != null)
                        {
                            tagHashtable =Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromTagInfoObj(insertCommand.tagInfo);
                        }

                        if (insertCommand.namedTagInfo != null)
                        {
                            if (clientManager.IsDotNetClient)
                            {
                                namedTagHashtable =Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromNamedTagInfoObjFromDotNet(insertCommand.namedTagInfo);
                            }
                            else
                            {
                                namedTagHashtable =Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromNamedTagInfoObjFromJava(insertCommand.namedTagInfo);
                            }
                        }
                    }
                    else
                    {
                        ObjectQueryInfo objectQueryInfo = insertCommand.objectQueryInfo;

                        if (objectQueryInfo.queryInfo != null)
                        {
                            queryInfoHashtable =Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromQueryInfoObj(objectQueryInfo.queryInfo);
                        }

                        if (objectQueryInfo.tagInfo != null)
                        {
                            tagHashtable =Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromTagInfoObj(objectQueryInfo.tagInfo);
                        }

                        if (objectQueryInfo.namedTagInfo != null)
                        {
                            if (clientManager.IsDotNetClient)
                            {
                                namedTagHashtable =Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromNamedTagInfoObjFromDotNet(objectQueryInfo.namedTagInfo);
                            }
                            else
                            {
                                namedTagHashtable =Alachisoft.NCache.Caching.Util.ProtobufHelper.GetHashtableFromNamedTagInfoObjFromJava(objectQueryInfo.namedTagInfo);
                            }
                        }
                    }

                    if (queryInfoHashtable != null)
                    {
                        if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                        cmdInfo.queryInfo.Add("query-info", queryInfoHashtable);
                    }

                    if (tagHashtable != null)
                    {
                        if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                        cmdInfo.queryInfo.Add("tag-info", tagHashtable);
                    }

                    if (namedTagHashtable != null)
                    {
                            if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                            cmdInfo.queryInfo.Add("named-tag-info", namedTagHashtable);
                    }

                    cmdInfo.RemoveCallbackId = (short)insertCommand.removeCallbackId;
                    
                    if (insertCommand.removeDataFilter != -1)
                        cmdInfo.RemoveDataFilter = insertCommand.removeDataFilter ;
                    else
                        cmdInfo.RemoveDataFilter = (int) Runtime.Events.EventDataFilter.DataWithMetadata;
                    
                    cmdInfo.RequestId = insertCommand.requestId.ToString();
                    cmdInfo.ResyncProviderName = insertCommand.resyncProviderName.Length == 0 ? null : insertCommand.resyncProviderName;
                    if (insertCommand.subGroup != null) cmdInfo.SubGroup = insertCommand.subGroup.Length == 0 ? null : insertCommand.subGroup;
                    if (insertCommand.syncDependency != null) cmdInfo.SyncDependency = base.GetCacheSyncDependencyObj(insertCommand.syncDependency);
                    cmdInfo.UpdateCallbackId = (short)insertCommand.updateCallbackId;

                    if (insertCommand.updateDataFilter != -1)
                        cmdInfo.UpdateDataFilter = insertCommand.updateDataFilter ;
                    else
                        cmdInfo.UpdateDataFilter = (int)Runtime.Events.EventDataFilter.None;

                    cmdInfo.ItemVersion = insertCommand.itemVersion;
                    cmdInfo.LockAccessType = (LockAccessType)insertCommand.lockAccessType;
                    cmdInfo.LockId = insertCommand.lockId;
                    cmdInfo.value = cache.SocketServerDataService.GetCacheData(insertCommand.data.ToArray(), cmdInfo.Flag);
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
    }
}

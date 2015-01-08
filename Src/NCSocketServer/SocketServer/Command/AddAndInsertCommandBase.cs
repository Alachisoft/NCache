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
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.SocketServer.Command
{
    class AddAndInsertCommandBase : CommandBase
    {
        protected struct CommandInfo
        {
            public string RequestId;
            public string Key;

            public BitSet Flag;
            public object LockId;
            public LockAccessType LockAccessType;

            public ExpirationHint ExpirationHint;
            public PriorityEvictionHint EvictionHint;

            public short RemoveCallbackId;
            public short UpdateCallbackId;
            public Hashtable queryInfo;

            public object value;

            public int UpdateDataFilter;
            public int RemoveDataFilter;
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

            string version = string.Empty;

            switch (command.type)
            {
                case Alachisoft.NCache.Common.Protobuf.Command.Type.ADD:
                    Alachisoft.NCache.Common.Protobuf.AddCommand addCommand = command.addCommand;
                    cmdInfo.Key = addCommand.key;
                    cmdInfo.EvictionHint = new PriorityEvictionHint((CacheItemPriority)addCommand.priority);
                    cmdInfo.ExpirationHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(addCommand.absExpiration, addCommand.sldExpiration,serializationContext);
                    cmdInfo.Flag = new BitSet((byte)addCommand.flag);
                    cmdInfo.queryInfo = new Hashtable();

                    version = command.version;

                    //version added in 4.2 [Dated: 18-Nov-2013; Author: Sami]
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
                        if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                        cmdInfo.queryInfo.Add("query-info", queryInfoHashtable);
                    }

                    cmdInfo.RemoveCallbackId = (short)addCommand.removeCallbackId;
                    //for old clients data fitler information will be missing
                    if (addCommand.removeDataFilter != -1)
                        cmdInfo.RemoveDataFilter = (int)addCommand.removeDataFilter;
                    else
                        cmdInfo.RemoveDataFilter = (int)Runtime.Events.EventDataFilter.DataWithMetadata;

                    cmdInfo.RequestId = addCommand.requestId.ToString();
                    cmdInfo.UpdateCallbackId = (short)addCommand.updateCallbackId;

                    if (addCommand.updateDataFilter != -1)
                    {
                        cmdInfo.UpdateDataFilter = addCommand.updateDataFilter;
                    }
                    else
                        cmdInfo.UpdateDataFilter = (int)Runtime.Events.EventDataFilter.None;

                    cmdInfo.value = UserBinaryObject.CreateUserBinaryObject(addCommand.data.ToArray());
                    break;

                case Alachisoft.NCache.Common.Protobuf.Command.Type.INSERT:
                    Alachisoft.NCache.Common.Protobuf.InsertCommand insertCommand = command.insertCommand;
                    cmdInfo.Key = insertCommand.key;
                    cmdInfo.EvictionHint = new PriorityEvictionHint((CacheItemPriority)insertCommand.priority);
                    cmdInfo.ExpirationHint = Alachisoft.NCache.Caching.Util.ProtobufHelper.GetExpirationHintObj(insertCommand.absExpiration, insertCommand.sldExpiration, serializationContext);
                    cmdInfo.Flag = new BitSet((byte)insertCommand.flag);
                    version = command.version;

                    //version added in 4.2 [Dated: 18-Nov-2013; Author: Sami]
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
                        if (cmdInfo.queryInfo == null) cmdInfo.queryInfo = new Hashtable();
                        cmdInfo.queryInfo.Add("query-info", queryInfoHashtable);
                    }

                    cmdInfo.RemoveCallbackId = (short)insertCommand.removeCallbackId;
                    
                    if (insertCommand.removeDataFilter != -1)
                        cmdInfo.RemoveDataFilter = insertCommand.removeDataFilter ;
                    else
                        cmdInfo.RemoveDataFilter = (int) Runtime.Events.EventDataFilter.DataWithMetadata;
                    
                    cmdInfo.RequestId = insertCommand.requestId.ToString();
                    cmdInfo.UpdateCallbackId = (short)insertCommand.updateCallbackId;

                    if (insertCommand.updateDataFilter != -1)
                        cmdInfo.UpdateDataFilter = insertCommand.updateDataFilter ;
                    else
                        cmdInfo.UpdateDataFilter = (int)Runtime.Events.EventDataFilter.None;
                    cmdInfo.LockAccessType = (LockAccessType)insertCommand.lockAccessType;
                    cmdInfo.LockId = insertCommand.lockId;
                    cmdInfo.value = UserBinaryObject.CreateUserBinaryObject(insertCommand.data.ToArray());
                    break;
            }
            return cmdInfo;
        }
    }
}

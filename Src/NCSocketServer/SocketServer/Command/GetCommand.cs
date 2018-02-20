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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using System.Text;
using Alachisoft.NCache.SocketServer.RuntimeLogging;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string Key;
            public string Group;
            public string SubGroup;
            public BitSet FlagMap;
            public LockAccessType LockAccessType;
            public object LockId;
            public TimeSpan LockTimeout;
            public ulong CacheItemVersion;
            public string ProviderName;
            public int ThreadId;
        }
        
        private OperationResult _getResult = OperationResult.Success;
        CommandInfo cmdInfo;

        internal override OperationResult OperationResult
        {
            get
            {
                return _getResult;
            }
        }

        public override bool CanHaveLargedata
        {
            get
            {
                return true;
            }
        }

        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "Get";
            details.Append("Command Keys: " + cmdInfo.Key);
            details.Append(" ; ");
            if (cmdInfo.FlagMap != null)
                details.Append("ReadThru: " + cmdInfo.FlagMap.IsBitSet(BitSetConstants.ReadThru));
            return details.ToString();
        }

        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            int overload;
            string exception = null;
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            try
            {
                overload = command.MethodOverload;
                cmdInfo = ParseCommand(command, clientManager);
                if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GetCmd.Exec", "cmd parsed");

            }
            catch (ArgumentOutOfRangeException arEx)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "GetCommand", "command: " + command + " Error" + arEx);
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(arEx, command.requestID,command.commandID));
                return;
            }
            catch (Exception exc)
            {
                _getResult = OperationResult.Failure;
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }
            Alachisoft.NCache.Common.Protobuf.GetResponse getResponse = null;
            try
            {
                object lockId = cmdInfo.LockId;
                ulong version = cmdInfo.CacheItemVersion;
                DateTime lockDate = new DateTime();
                NCache nCache = clientManager.CmdExecuter as NCache;
                CompressedValueEntry flagValueEntry = null;

                OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);

                if (cmdInfo.LockAccessType == LockAccessType.ACQUIRE)
                {
                    operationContext.Add(OperationContextFieldName.ClientThreadId, clientManager.ClientID);
                    operationContext.Add(OperationContextFieldName.ClientThreadId, cmdInfo.ThreadId);
                    operationContext.Add(OperationContextFieldName.IsRetryOperation, command.isRetryCommand);
                }
                flagValueEntry = nCache.Cache.GetGroup(cmdInfo.Key, cmdInfo.FlagMap, cmdInfo.Group, cmdInfo.SubGroup, ref version, ref lockId, ref lockDate, cmdInfo.LockTimeout, cmdInfo.LockAccessType, cmdInfo.ProviderName, operationContext);
                stopWatch.Stop();
                UserBinaryObject ubObj = null;

                if (flagValueEntry != null)
                {
                    if (flagValueEntry.Value is UserBinaryObject)
                        ubObj = (UserBinaryObject)flagValueEntry.Value;
                    else
                        ubObj = (UserBinaryObject)nCache.Cache.SocketServerDataService.GetClientData(flagValueEntry.Value, ref flagValueEntry.Flag, LanguageContext.DOTNET);
                }
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                getResponse = new Alachisoft.NCache.Common.Protobuf.GetResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET;
                if (lockId != null)
                {
                    getResponse.lockId = lockId.ToString();
                }
                getResponse.lockTime = lockDate.Ticks;
                getResponse.version = version;

                if (ubObj == null)
                {
                    response.get = getResponse;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
                else
                {
                    getResponse.flag = flagValueEntry.Flag.Data;
                    getResponse.data.AddRange(ubObj.DataList);
                    response.get = getResponse;
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                }
            }
            catch (Exception exc)
            {
                exception = exc.ToString();
                _getResult = OperationResult.Failure;
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
            }
            finally
            {
                TimeSpan executionTime = stopWatch.Elapsed;
                try
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                    {
                        int resutlt = 0;
                        if (getResponse != null)
                        {
                            if ( getResponse.data!=null)
                                resutlt = getResponse.data.Count;
                        }
                        string methodName = null;
                        if (cmdInfo.LockAccessType == LockAccessType.ACQUIRE)
                            methodName = MethodsName.GET.ToLower();
                        else if (cmdInfo.LockAccessType == LockAccessType.COMPARE_VERSION)
                            methodName = MethodsName.GetIfNewer.ToLower();
                        else
                            methodName = MethodsName.GET.ToLower();
                        APILogItemBuilder log = new APILogItemBuilder(methodName);
                        log.GenerateGetCommandAPILogItem(cmdInfo.Key, cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.FlagMap, (long)cmdInfo.CacheItemVersion, cmdInfo.LockAccessType, cmdInfo.LockTimeout, cmdInfo.LockId, cmdInfo.ProviderName, overload, exception, executionTime, clientManager.ClientID.ToLower(), clientManager.ClientSocketId.ToString(), resutlt);
                    }
                }
                catch
                {

                }
            }
            if (ServerMonitor.MonitorActivity) ServerMonitor.LogClientActivity("GetCmd.Exec", "cmd executed on cache");
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetCommand getCommand = command.getCommand;

            cmdInfo.CacheItemVersion = getCommand.version;
            cmdInfo.FlagMap = new BitSet((byte)getCommand.flag);

			cmdInfo.Group = getCommand.group.Length == 0 ? null : getCommand.group;
            cmdInfo.Key = getCommand.key;
            cmdInfo.LockAccessType = (LockAccessType)getCommand.lockInfo.lockAccessType;
            cmdInfo.LockId = getCommand.lockInfo.lockId;
            cmdInfo.LockTimeout = new TimeSpan(getCommand.lockInfo.lockTimeout);
			cmdInfo.ProviderName = getCommand.providerName.Length == 0 ? null : getCommand.providerName;
            cmdInfo.RequestId = getCommand.requestId.ToString();
			cmdInfo.SubGroup = getCommand.subGroup.Length == 0 ? null : getCommand.subGroup;
            cmdInfo.ThreadId = getCommand.threadId;

            return cmdInfo;
        }
    }
}

using System;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using System.Collections;
using Alachisoft.NCache.Common.Monitoring;
using System.Text;
using Alachisoft.NCache.SocketServer.RuntimeLogging;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.SocketServer.Pooling;
using Alachisoft.NCache.Util;
using System.Diagnostics;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class InsertCommand : AddAndInsertCommandBase
    {
        private OperationResult _insertResult = OperationResult.Success;
        CommandInfo cmdInfo;

        private readonly InsertResponse _insertResponse;
        private readonly OperationContext _operationContext;

        internal override OperationResult OperationResult
        {
            get
            {
                return _insertResult;
            }
        }

        public InsertCommand() : base()
        {
            _insertResponse = new InsertResponse();
            _operationContext = new OperationContext();
        }

        //PROTOBUF
        public override string GetCommandParameters(out string commandName)
        {
            StringBuilder details = new StringBuilder();
            commandName = "Insert";
            details.Append("Command Key: " + cmdInfo.Key);
            details.Append(" ; ");

            UserBinaryObject binaryObject = cmdInfo.value as UserBinaryObject;
            if (binaryObject != null)
                details.Append("Command Value Size: " + binaryObject.Size);
            else
                details.Append("Command Value: " + cmdInfo.value);
            details.Append(" ; ");

            if (cmdInfo.Flag != null)
            {
                if (cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru))
                    details.Append("WriteThru: " + cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru) + " ; ");
                if (cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind))
                    details.Append("WriteBehind: " + cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind) + " ; ");

            }
            if (cmdInfo.ExpirationHint != null)
                details.Append("Dependency: " + cmdInfo.ExpirationHint.GetType().Name);
            return details.ToString();
        }

        private CallbackType CallbackType(int type)
        {
            if (type == 0)
                return Runtime.Events.CallbackType.PullBasedCallback;
            else if (type == 1)
                return Runtime.Events.CallbackType.PushBasedNotification;
            else
                return Runtime.Events.CallbackType.PushBasedNotification; // default
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            NCache nCache = clientManager.CmdExecuter as NCache;
            int overload;
            long dataLength = 0;
            string exception = null;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            bool itemUpdated = false;
            bool itemRemove = false;
            try
            {
                try
                {
                    overload = command.MethodOverload;
                    serializationContext = nCache.CacheId;
                    cmdInfo = ParseCommand(command, clientManager, serializationContext);
                }
                catch (System.Exception exc)
                {
                    _insertResult = OperationResult.Failure;
                    {
                        //PROTOBUF:RESPONSE
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    }
                    return;
                }

                Notifications callbackEntry = null;

                if (cmdInfo.UpdateCallbackId != -1 || cmdInfo.RemoveCallbackId != -1 || (!cmdInfo.RequestId.Equals("-1") && cmdInfo.DoAsync) || cmdInfo.DsItemAddedCallbackId != -1)
                {
                    if (cmdInfo.RemoveCallbackId != -1)
                        itemRemove = true;
                    if (cmdInfo.UpdateCallbackId != -1)
                        itemUpdated = true;

                    callbackEntry = new Notifications(!string.IsNullOrEmpty(cmdInfo.ClientID) ? cmdInfo.ClientID : clientManager.ClientID,
                        Convert.ToInt32(cmdInfo.RequestId),
                        cmdInfo.RemoveCallbackId,
                        cmdInfo.UpdateCallbackId,
                        (short)(cmdInfo.RequestId.Equals("-1") ? -1 : 0),
                        cmdInfo.DsItemAddedCallbackId,
                        (Runtime.Events.EventDataFilter)cmdInfo.UpdateDataFilter,
                        (Runtime.Events.EventDataFilter)cmdInfo.RemoveDataFilter,
                        CallbackType(cmdInfo.CallbackType)
                        );
                }

                UserBinaryObject data = cmdInfo.value as UserBinaryObject;
                if (data != null)
                    dataLength = data.Length;

                if (!cmdInfo.DoAsync)
                {
                    OperationContext operationContext = null;

                    try
                    {
                        operationContext = _operationContext;
                        operationContext.Add(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
                        CommandsUtil.PopulateClientIdInContext(ref operationContext, clientManager.ClientAddress);
                        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
                        UInt64 itemVersion = 0;
                        if (cmdInfo.ItemVersion == 0)
                            itemVersion = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                        else
                            itemVersion = cmdInfo.ItemVersion;

                        operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);
                        operationContext.Add(OperationContextFieldName.MethodOverload, overload);

                        operationContext.MarkInUse(NCModulesConstants.SocketServer);
                        ulong version = nCache.Cache.Insert(cmdInfo.Key,
                       cmdInfo.value,
                       cmdInfo.ExpirationHint,
                       cmdInfo.EvictionHint,
                       cmdInfo.Group,
                       cmdInfo.SubGroup,
                       cmdInfo.queryInfo,
                       cmdInfo.Flag,
                       cmdInfo.LockId,
                       cmdInfo.ItemVersion,
                       cmdInfo.LockAccessType,
                       cmdInfo.ProviderName,
                       cmdInfo.ResyncProviderName,
                       operationContext,
                       callbackEntry,
                       cmdInfo.Type
                       );

                        stopWatch.Stop();

                        //PROTOBUF:RESPONSE
                        _insertResponse.version = version;

                        if (clientManager.ClientVersion >= 5000)
                        {
                            ResponseHelper.SetResponse(_insertResponse, command.requestID, command.commandID);

                            _serializedResponsePackets.Add(ResponseHelper.SerializeInsertResponse(_insertResponse, Response.Type.INSERT));
                        }
                        else
                        {
                            var response = Stash.ProtobufResponse;
                            response.insert = _insertResponse;
                            ResponseHelper.SetResponse(response, command.requestID, command.commandID, Response.Type.INSERT);

                            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
                        }
                    }
                    catch (System.Exception exc)
                    {
                        _insertResult = OperationResult.Failure;
                        exception = exc.ToString();

                        //PROTOBUF:RESPONSE
                        _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                    }
                    finally
                    {
                        operationContext?.MarkFree(NCModulesConstants.SocketServer);

                        // Returning these here explicitly because the CacheEntry created for this operation is actually 
                        // fetched from Stash rather than pool. Since the return call for that entry is going to fail, the 
                        // metadata (such as these objects) attached to that entry won't be returned to pool. Therefore, 
                        // we return them here explicitly.
                        if (cmdInfo.value is UserBinaryObject userBinaryObject)
                            MiscUtil.ReturnUserBinaryObjectToPool(userBinaryObject, userBinaryObject.PoolManager);

                        MiscUtil.ReturnExpirationHintToPool(cmdInfo.ExpirationHint, cmdInfo.ExpirationHint?.PoolManager);
                        //MiscUtil.ReturnSyncDependencyToPool(cmdInfo.SyncDependency, cmdInfo.SyncDependency?.PoolManager);

                        TimeSpan executionTime = stopWatch.Elapsed;

                        try
                        {
                            if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                            {

                                APILogItemBuilder log = new APILogItemBuilder(MethodsName.INSERT.ToLower());
                                object toInsert;
                                if (cmdInfo.value is UserBinaryObject)
                                {
                                    toInsert = dataLength;
                                }
                                else
                                    toInsert = cmdInfo.DataFormatValue;
                                Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                                log.GenerateADDInsertAPILogItem(cmdInfo.Key, toInsert, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, cmdInfo.EvictionHint.Priority, expirationHint["tag-info"] != null ? expirationHint["tag-info"] as Hashtable : null, cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.Flag, cmdInfo.ProviderName, cmdInfo.ResyncProviderName, false, expirationHint["named-tags"] != null ? expirationHint["named-tags"] as Hashtable : null, cmdInfo.UpdateCallbackId, cmdInfo.DsItemAddedCallbackId, false, itemUpdated, itemRemove, overload, exception, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());

                            }
                        }
                        catch
                        {

                        }
                    }
                }
                else
                {
                    OperationContext operationContext = null;

                    try
                    {
                        operationContext = new OperationContext();
                        
                        operationContext.Add(OperationContextFieldName.WriteThru, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteThru));
                        operationContext.Add(OperationContextFieldName.WriteBehind, cmdInfo.Flag.IsBitSet(BitSetConstants.WriteBehind));

                        if (cmdInfo.ProviderName != null)
                            operationContext.Add(OperationContextFieldName.WriteThruProviderName, cmdInfo.ProviderName);

                        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

                        UInt64 itemVersion = 0;
                        if (cmdInfo.ItemVersion == 0)
                            itemVersion = (ulong)(DateTime.UtcNow - Common.Util.Time.ReferenceTime).TotalMilliseconds;
                        else
                            itemVersion = cmdInfo.ItemVersion;

                        operationContext.Add(OperationContextFieldName.ItemVersion, itemVersion);

                        operationContext.Add(OperationContextFieldName.MethodOverload, overload);

                        bool onAsyncCall = false;
                        if (callbackEntry != null)
                        {
                            onAsyncCall = true;
                        }

                        // Fetching this from pool to avoid value corruption for eviction hint
                        cmdInfo.EvictionHint = Caching.EvictionPolicies.PriorityEvictionHint.Create(
                            clientManager.CacheTransactionalPool, cmdInfo.EvictionHint.Priority
                        );

                        nCache.Cache.InsertAsync(cmdInfo.Key,
                             (object)cmdInfo.value,
                            cmdInfo.ExpirationHint,
                            cmdInfo.EvictionHint,
                            cmdInfo.Group,
                            cmdInfo.SubGroup,
                            cmdInfo.Flag,
                            cmdInfo.queryInfo,
                            cmdInfo.ProviderName,
                            operationContext,
                            callbackEntry,
                            cmdInfo.Type
                            );
                        stopWatch.Stop();
                        TimeSpan executionTime = stopWatch.Elapsed;
                        try
                        {
                            if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null && Alachisoft.NCache.Management.APILogging.APILogManager.EnableLogging)
                            {
                                APILogItemBuilder log = new APILogItemBuilder(MethodsName.INSERTASYNC.ToLower());
                                object toInsert;
                                if (cmdInfo.value is UserBinaryObject)
                                {
                                    toInsert = dataLength;
                                }
                                else
                                    toInsert = cmdInfo.DataFormatValue;
                                Hashtable expirationHint = log.GetDependencyExpirationAndQueryInfo(cmdInfo.ExpirationHint, cmdInfo.queryInfo);
                                log.GenerateADDInsertAPILogItem(cmdInfo.Key, toInsert, expirationHint["dependency"] != null ? expirationHint["dependency"] as ArrayList : null, expirationHint["absolute-expiration"] != null ? (long)expirationHint["absolute-expiration"] : -1, expirationHint["sliding-expiration"] != null ? (long)expirationHint["sliding-expiration"] : -1, cmdInfo.EvictionHint.Priority, expirationHint["tag-info"] != null ? expirationHint["tag-info"] as Hashtable : null, cmdInfo.Group, cmdInfo.SubGroup, cmdInfo.Flag, cmdInfo.ProviderName, cmdInfo.ResyncProviderName, false, expirationHint["named-tags"] != null ? expirationHint["named-tags"] as Hashtable : null, cmdInfo.UpdateCallbackId, cmdInfo.DsItemAddedCallbackId, onAsyncCall, itemUpdated, itemRemove, overload, null, executionTime, clientManager.ClientID, clientManager.ClientSocketId.ToString());

                            }
                        }
                        catch
                        {

                        }
                    }
                    finally
                    {
                        operationContext?.MarkFree(NCModulesConstants.SocketServer);
                    }
                }
            }
            finally
            {
                cmdInfo.Flag?.MarkFree(NCModulesConstants.SocketServer);
            }
        }

        public sealed override void ResetLeasable()
        {
            base.ResetLeasable();

            cmdInfo = default;
            _insertResponse.ResetLeasable();
            _operationContext.ResetLeasable();
            _insertResult = OperationResult.Success;
        }

        public sealed override void ReturnLeasableToPool()
        {
            _insertResponse?.ReturnLeasableToPool();
            PoolManager.GetSocketServerInsertCommandPool()?.Return(this);
        }
    }
}

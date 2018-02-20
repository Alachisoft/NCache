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
using System.IO;
using System.Reflection;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Diagnostics;
using System.Collections.Generic;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    /// <summary>
    /// Manager class for read-trhough and write-through operations
    /// </summary>
    internal class WriteThruProviderMgr: IDisposable
    {
        private string _myProvider;
        /// <summary> The runtime context associated with the current cache. </summary>
        private CacheRuntimeContext		_context;
        /// <summary> The external datasource writer </summary>
        private IWriteThruProvider		_dsWriter;

        /// <summary> This will help to identify language type of read object </summary>
        private LanguageContext _languageContext;       

        private string _cacheName;

        private long _operationDelay;

        public LanguageContext LanguageContext
        {
            get { return _languageContext; }
            set {_languageContext=value; }
        }
        ILogger NCacheLog
        {
            get { return _context.NCacheLog; }
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public WriteThruProviderMgr()
        {
        }

        /// <summary>
        /// Overloaded constructor
        /// Initializes the object based on the properties specified in configuration
        /// </summary>
        /// <param name="properties">properties collection for this cache.</param>
        public WriteThruProviderMgr(string cacheName, IDictionary properties, CacheRuntimeContext context, long operationDelay, string providerName)
        {
            _cacheName=cacheName;
            _context = context;
            _operationDelay = operationDelay;
            _myProvider = providerName;
            Initialize(properties);
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {            
            if(_dsWriter != null)
            {                
                lock (_dsWriter)
                {
                    try
                    {
                        _dsWriter.Dispose();
                    }
                    catch (Exception e)
                    {                       
                        NCacheLog.Error("WriteThruProviderMgr", "User code threw " + e.ToString());
                    }
                    _dsWriter = null;
                }
            }
        }

        #endregion

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Method that allows the object to initialize itself. Passes the property map down 
        /// the object hierarchy so that other objects may configure themselves as well..
        /// sample config string
        /// 
        /// backing-source
        /// (
        /// 	read-thru
        /// 	(
        /// 		 assembly='Diyatech.Sample';
        /// 		 class='mySync.DB.Reader'
        /// 	);
        /// 	write-thru
        /// 	(
        /// 		  assembly='Diyatech.Sample';
        /// 		  class='mySync.DB.Writer'
        /// 	)
        /// )
        /// </summary>
        /// <param name="properties">properties collection for this cache.</param>
        private void Initialize(IDictionary properties)
        {
            Assembly asm = null;
            if(properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                if(!properties.Contains("assembly-name"))
                    throw new ConfigurationException("Missing assembly name for write-thru option");
                if(!properties.Contains("class-name"))
                    throw new ConfigurationException("Missing class name for write-thru option");

                string assembly = Convert.ToString(properties["assembly-name"]);
                string classname = Convert.ToString(properties["class-name"]);
                IDictionary	startupparams = properties["parameters"] as IDictionary;

                //This is added to load the .exe and .dll providers
                //to keep previous provider running this bad chunk of code is written
                //later on you can directly provide the provider name read from config.
                string extension = ".dll";
                if (properties.Contains("full-name"))
                {
                    extension = Path.GetExtension(Convert.ToString(properties["full-name"]));
                }

                if(startupparams == null) startupparams = new Hashtable();                
                
                try
                {

                    if (extension.Equals(".dll") || extension.Equals(".exe"))
                    {


                        AuthenticateFeature.Authenticate(LanguageContext.DOTNET);

                        _languageContext = LanguageContext.DOTNET;

                        if (classname.Contains("Alachisoft.NCache.Web.ClientCache.ClientCacheWriteThru") && InternalProviderFactory.Instance != null)
                        {
                            _dsWriter = InternalProviderFactory.Instance.CreateWriteThruProvider();
                        }
                        else
                        {
                            string path = AppUtil.DeployedAssemblyDir + _cacheName + GetWriteThruAssemblyPath(assembly) + extension;

                            if (DatasourceMgr.AssemblyCache.ContainsKey(assembly))
                                asm = DatasourceMgr.AssemblyCache[assembly];
                            else
                            {
                                try
                                {
                                    asm = Assembly.LoadFrom(path);
                                }
                                catch (Exception e)
                                {
                                    try
                                    {
                                        asm = Assembly.Load(assembly);
                                    }
                                    catch (Exception)
                                    {
                                        try
                                        {
                                            string version = Assembly.GetExecutingAssembly().ImageRuntimeVersion;
                                            path = Path.Combine(AppUtil.InstallDir, "bin");
                                            path = Path.Combine(path, "assembly");
                                            if (version.Contains("v4"))
                                                path = Path.Combine(path, "4.0") + GetWriteThruAssemblyPath(assembly) + extension;
                                            else
                                                path = Path.Combine(path, "2.0") + GetWriteThruAssemblyPath(assembly) + extension;
                                            asm = Assembly.LoadFrom(path);
                                        }
                                        catch (Exception)
                                        {
                                            string message = string.Format("Could not load assembly \"" + assembly + "\". {0}",
                                                e.Message);
                                            throw new Exception(message);
                                        }
                                    }
                                }
                            }
                            if (asm != null)
                                _dsWriter = (IWriteThruProvider)asm.CreateInstance(classname);

                            if (_dsWriter == null)
                                throw new Exception("Unable to instantiate " + classname);

                            if (asm != null)
                                DatasourceMgr.AssemblyCache[assembly] = asm;
                        }

                        _dsWriter.Init(startupparams,_cacheName);
                    }

                    

                }
                catch(InvalidCastException)
                {
                    throw new ConfigurationException("The class specified in write-thru does not implement IDatasourceWriter");
                }
                catch(Exception e)
                {
                    throw new ConfigurationException(e.Message, e);
                }
            }
            catch(ConfigurationException)
            {
                throw;
            }
            catch(Exception e)
            {
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }		
        }

        #endregion

        public void HotApplyConfig(long operationDelay)
        {
            _operationDelay = operationDelay;
        }

        public string MyProviderName
        {
            get { return _myProvider; }
        }

        private string GetWriteThruAssemblyPath(string asm)
        {
            string path = Path.DirectorySeparatorChar.ToString();
            string[] folderNames = asm.Split(new char[] { ',','=' });
            path = path + folderNames[0];
            return path;
        }

        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public void WriteBehind(CacheBase internalCache, object key, CacheEntry entry, string source, string taskId, OpCode operationCode)
        {
            if (_context.DsMgr._writeBehindAsyncProcess != null )
            {
                _context.DsMgr._writeBehindAsyncProcess.Enqueue(new DSWriteBehindOperation(_context, key,  entry, operationCode, _myProvider, _operationDelay, taskId, source,WriteBehindAsyncProcessor.TaskState.Waite));

            }
        }

        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public void WriteBehind(CacheBase internalCache, object key, CacheEntry entry, string source, string taskId, OpCode operationCode, WriteBehindAsyncProcessor.TaskState state)
        {
            if (_context.DsMgr._writeBehindAsyncProcess != null)
            {
                _context.DsMgr._writeBehindAsyncProcess.Enqueue(new DSWriteBehindOperation(_context, key,  entry, operationCode, _myProvider, _operationDelay, taskId, source,state));
            }
        }

        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public void WriteBehind(CacheBase internalCache, object[] keys,  CacheEntry[] entries, string source, string taskId, OpCode operationCode)
        {
            if (_context.DsMgr._writeBehindAsyncProcess != null)
            {                
                for (int i = 0; i < keys.Length; i++)
                {
                    _context.DsMgr._writeBehindAsyncProcess.Enqueue(new DSWriteBehindOperation(_context, keys[i],  entries[i], operationCode, _myProvider, _operationDelay, taskId + "-" + i, source, WriteBehindAsyncProcessor.TaskState.Waite));
                }
            }
        }

        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public void WriteBehind(CacheBase internalCache, object[] keys, CacheEntry[] entries, string source, string taskId, OpCode operationCode, WriteBehindAsyncProcessor.TaskState state)
        {
            if (_context.DsMgr._writeBehindAsyncProcess != null)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    _context.DsMgr._writeBehindAsyncProcess.Enqueue(new DSWriteBehindOperation(_context, keys[i],  entries[i], operationCode, _myProvider, _operationDelay, taskId + "-" + i, source, state));
                }
            }
        }

        internal void WriteBehind(DSWriteBehindOperation operation)
        {
            if (_context.DsMgr._writeBehindAsyncProcess != null)
            {
                if (operation != null)
                {
                    operation.RetryCount++;
                    operation.OperationDelay = _operationDelay;//config value
                    _context.DsMgr._writeBehindAsyncProcess.Enqueue(operation);
                }
            }
        }
        internal void WriteBehind(ArrayList operations)
        {
            if (_context.DsMgr._writeBehindAsyncProcess != null)
            {
                DSWriteBehindOperation operation = null;
                for (int i = 0; i < operations.Count; i++)
                {
                    operation = operations[i] as DSWriteBehindOperation;
                    if (operations != null)
                    {
                        operation.RetryCount++;
                        operation.OperationDelay = _operationDelay;//config value
                        _context.DsMgr._writeBehindAsyncProcess.Enqueue(operation);
                    }
                }
            }
        }
        /// <summary>
        /// Dequeue a task matching task id
        /// </summary>
        /// <param name="taskId">taskId</param>
        public void DequeueWriteBehindTask(string[] taskId)
        {
            if (_context.DsMgr != null && _context.DsMgr._writeBehindAsyncProcess != null)
            {
                _context.DsMgr._writeBehindAsyncProcess.Dequeue(taskId);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="state"></param>
        public void SetState(string taskId, WriteBehindAsyncProcessor.TaskState state)
        {
            if (_context.DsMgr != null && _context.DsMgr._writeBehindAsyncProcess != null)
            {
                _context.DsMgr._writeBehindAsyncProcess.SetState(taskId, state);
            }
        }

        public void SetState(string taskId, WriteBehindAsyncProcessor.TaskState state, Hashtable newTable)
        {
            if (_context.DsMgr != null && _context.DsMgr._writeBehindAsyncProcess != null)
            {
                _context.DsMgr._writeBehindAsyncProcess.SetState(taskId, state, newTable);
            }
        }

        /// <summary>
        /// Clone the current write behind queue
        /// </summary>
        /// <returns>write behind queue</returns>
        public WriteBehindAsyncProcessor.WriteBehindQueue CloneQueue()
        {
            if (_context.DsMgr != null && _context.DsMgr._writeBehindAsyncProcess != null)
            {
                return _context.DsMgr._writeBehindAsyncProcess.CloneQueue();
            }
            return null;
        }

        public void CopyQueue(WriteBehindAsyncProcessor.WriteBehindQueue queue)
        {
            if (_context.DsMgr != null && _context.DsMgr._writeBehindAsyncProcess != null)
            {
                _context.DsMgr._writeBehindAsyncProcess.MergeQueue(_context, queue);
            }
        }

      
        /// <summary>
        /// Update the data source, according to type of operation specified
        /// </summary>
        /// <param name="cacheImpl">cache</param>
        /// <param name="keys">array of keys to be updated</param>
        /// <param name="values">array of values. required in case of insert or add operations.
        /// pass null otherwise</param>
        /// <param name="entries">array of cache enteries. required in case of remove operations.
        /// pass null otherwise</param>
        /// <param name="returnSet">the table returned from the bulk operation that was performed.
        /// this table will be updated accordingly</param>
        /// <param name="operationCode">type of operation</param>
        public OperationResult[] WriteThru(CacheBase cacheImpl, DSWriteOperation[] operations, Hashtable returnSet,bool async, OperationContext operationContext)
        {
            Exception exc = null;
            OperationResult[] result = null;

            try
            {
                List<WriteOperation> writeOperations = new List<WriteOperation>();
                //create write operations
                for (int i = 0; i < operations.Length; i++)
                {
                    writeOperations.Add(operations[i].GetWriteOperation(_languageContext, operationContext));
                }

                if (writeOperations.Count > 0)
                {
                    _context.PerfStatsColl.MsecPerDSWriteBeginSample();

                    result = WriteThru(writeOperations.ToArray());

                    _context.PerfStatsColl.MsecPerDSWriteEndSample(writeOperations.Count);
                }
            }
            catch (Exception e)
            {
                exc = e;
            }
            finally
            {
                if (!async)
                    this._context.PerfStatsColl.IncrementWriteThruPerSecBy(operations.Length);
                else
                    this._context.PerfStatsColl.IncrementWriteBehindPerSecBy(operations.Length);

                ArrayList failedOpsKeys = new ArrayList(); 
                if (result != null)//no exception
                {
                    for (int i = 0; i < result.Length; i++)
                    {
                        //populate return set
                        if (result[i].DSOperationStatus == OperationResult.Status.Failure || result[i].DSOperationStatus == OperationResult.Status.FailureDontRemove)
                        {
                            if (result[i].Exception != null)
                                returnSet[result[i].Operation.Key] = result[i].Exception;
                            else if (!String.IsNullOrEmpty(result[i].Error))
                                returnSet[result[i].Operation.Key] = new Exception(result[i].Error);
                            else
                                returnSet[result[i].Operation.Key] = result[i].DSOperationStatus;//return status
                            _context.PerfStatsColl.IncrementDSFailedOpsPerSec();
                        }
                        else
                        {
                            if (result[i].DSOperationStatus == OperationResult.Status.FailureRetry)
                                _context.PerfStatsColl.IncrementDSFailedOpsPerSec();
                            returnSet[result[i].Operation.Key] = result[i].DSOperationStatus;
                        }
                        if (result[i].DSOperationStatus == OperationResult.Status.Failure)
                        {
                            switch (result[i].Operation.OperationType)
                            {
                                case WriteOperationType.Add:
                                case WriteOperationType.Update:
                                    failedOpsKeys.Add(result[i].Operation.Key);
                                    //j++;
                                    break;
                            }
                        }
                    }
                }
                else if (exc!=null) //remove all batch operations
                {
                    for (int i = 0; i < operations.Length; i++)
                    {
                        failedOpsKeys.Add(operations[i].Key.ToString());
                        returnSet[operations[i].Key.ToString()] = exc;
                    }
                    _context.PerfStatsColl.IncrementDSFailedOpsPerSecBy(operations.Length);
                }
                try
                {
                    if (failedOpsKeys.Count > 0)
                    {
                        string[] failedOps=new string[failedOpsKeys.Count];
                        Array.Copy(failedOpsKeys.ToArray(), failedOps,failedOpsKeys.Count);

                        cacheImpl.Remove(failedOps, ItemRemoveReason.Removed, false, operationContext);
                    }
                }
                catch (Exception e)
                {
                    throw new OperationFailedException("Data Source write operation failed. Error: " + e.Message, e);
                }
            }
            return result;
        }

        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>-1 if writer is null, 0 if user operation returned false, 1 if successful</returns>
        private OperationResult WriteThru(WriteOperation writeOp, OperationContext operationContext)
        {
            OperationResult result = null; 
            if (_dsWriter == null) return result;
            Stopwatch writeThruWatch = new Stopwatch();
            writeThruWatch.Start();
            if(operationContext.Contains(OperationContextFieldName.MethodOverload))
            {
                writeOp.MethodOverlaod =(int) operationContext.GetValueByField(OperationContextFieldName.MethodOverload);
            }
            result = _dsWriter.WriteToDataSource(writeOp);
            writeThruWatch.Stop();
            double elapsedByWriteThru = writeThruWatch.Elapsed.TotalSeconds;

            if (elapsedByWriteThru > ServiceConfiguration.CommandExecutionThreshold &&
                ServiceConfiguration.EnableCommandThresholdLogging)
            {
                if (_context.NCacheLog != null)
                    _context.NCacheLog.Warn("WriteThruProviderMgr.WriteThru",
                        "WriteThru took " + elapsedByWriteThru + " seconds to complete. Which is longer than expected.");
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <param name="opCode"></param>
        /// <returns></returns>
        private OperationResult[] WriteThru(WriteOperation[] writeOperations)
        {
            if (_dsWriter == null) return null;
            System.Diagnostics.Stopwatch writeThruWatch = new System.Diagnostics.Stopwatch();
            try
            {
                writeThruWatch.Start();
                return _dsWriter.WriteToDataSource(writeOperations);
            }
            finally
            {
                writeThruWatch.Stop();
                double elapsedByWriteThru = writeThruWatch.Elapsed.TotalSeconds;

                if (elapsedByWriteThru > ServiceConfiguration.CommandExecutionThreshold &&
                    ServiceConfiguration.EnableCommandThresholdLogging)
                {
                    if (_context.NCacheLog != null)
                        _context.NCacheLog.Warn("WriteThruProviderMgr.BulkWriteThru",
                            "WriteThru took " + elapsedByWriteThru +
                            " seconds to complete. Which is longer than expected.");
                }
            }


        }
    
        internal OperationResult WriteThru(CacheBase cacheImpl, DSWriteOperation operation,bool async, OperationContext operationContext)
        {
            if (_context.DsMgr == null || (_context != null && !(_context.DsMgr.IsWriteThruEnabled)))
                throw new OperationFailedException("Backing source not available. Verify backing source settings");

            Exception exc = null;
            OperationResult dsResult = null;

            try
            {
                WriteOperation writeOperation = operation.GetWriteOperation(_languageContext, operationContext);

                _context.PerfStatsColl.MsecPerDSWriteBeginSample();

                //WriteOperations
                dsResult = WriteThru(writeOperation, operationContext);

                _context.PerfStatsColl.MsecPerDSWriteEndSample();

            }
            catch (Exception e)
            {
                exc = e;
            }
            finally
            {

                try
                {
                    if (!async)
                        this._context.PerfStatsColl.IncrementWriteThruPerSec();
                    else
                        this._context.PerfStatsColl.IncrementWriteBehindPerSec(); 

                    if ((exc != null) || (dsResult != null && dsResult.DSOperationStatus == OperationResult.Status.Failure))
                    {
                        switch (operation.OperationCode)
                        {
                            case OpCode.Add:
                            case OpCode.Update:
                                
                                cacheImpl.Remove(operation.Key, ItemRemoveReason.Removed, false, null, 0, LockAccessType.IGNORE_LOCK, operationContext);
                                cacheImpl.Context.NCacheLog.CriticalInfo("Removing key: " + operation.Key + " after data source write operation");
                                break;
                        }
                        _context.PerfStatsColl.IncrementDSFailedOpsPerSec();
                        if (exc != null)
                            throw new OperationFailedException("IWriteThruProvider failed."  + exc.ToString(), exc);
                    }

                    if (dsResult != null && (dsResult.DSOperationStatus == OperationResult.Status.Failure))
                    {
                        _context.PerfStatsColl.IncrementDSFailedOpsPerSec();
                        throw new OperationFailedException("IWriteThruProvider failed. " + ((dsResult.Exception != null) ? "Exception: " + dsResult.Exception.ToString() : (dsResult.Error != null ? "ErrorMessage: " + dsResult.Error : "")), exc);
                    }
                    if (dsResult != null && (dsResult.DSOperationStatus == OperationResult.Status.Failure || dsResult.DSOperationStatus == OperationResult.Status.FailureRetry))
                    {
                        _context.PerfStatsColl.IncrementDSFailedOpsPerSec(); 
                    }
                }
                catch (Exception ex)
                {
                    throw new OperationFailedException("Error: " + ex.Message, ex);
                }
            }

            return dsResult;
        }
    }
}
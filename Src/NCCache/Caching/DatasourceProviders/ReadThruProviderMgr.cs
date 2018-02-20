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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Diagnostics;
using Alachisoft.NCache.Common.DataSource;
using Alachisoft.NCache.Caching.DataGrouping;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    /// <summary>
    /// Manager class for read-trhough and write-through operations
    /// </summary>
    internal class ReadThruProviderMgr : IDisposable
    {
        Assembly _asm;
        /// <summary> The runtime context associated with the current cache. </summary>
        private CacheRuntimeContext _context;
        /// <summary> The external datasource reader </summary>
        private IReadThruProvider _dsReader;
        /// <summary> The NewTrace management.</summary>
        //private NewTrace nTrace;
        private string _cacheName;

        private LanguageContext _languageContext;

        public LanguageContext ProviderType
        {
            get { return _languageContext; }
        }

        ILogger NCacheLog
        {
            get { return _context.NCacheLog; }
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public ReadThruProviderMgr()
        {
        }

        public string CacheId
        {
            get { return _cacheName; }
        }

        /// <summary>
        /// Overloaded constructor
        /// Initializes the object based on the properties specified in configuration
        /// </summary>
        /// <param name="properties">properties collection for this cache.</param>
        public ReadThruProviderMgr(string cacheName, IDictionary properties, CacheRuntimeContext context)
        {
            _cacheName = cacheName;
            _context = context;
            Initialize(properties);
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
           
            if (_dsReader != null)
            {
                lock (_dsReader)
                {
                    try
                    {
                        _dsReader.Dispose();


                    }
                    catch (Exception e)
                    {
                        NCacheLog.Error("ReadThruProviderMgr", "User code threw " + e.ToString());
                    }
                }
                _dsReader = null;
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
            
            if (properties == null)
                throw new ArgumentNullException("properties");

            try
            {
                if (!properties.Contains("assembly-name"))
                    throw new ConfigurationException("Missing assembly name for read-thru option");
                if (!properties.Contains("class-name"))
                    throw new ConfigurationException("Missing class name for read-thru option");
               

                string assembly = Convert.ToString(properties["assembly-name"]);
                string classname = Convert.ToString(properties["class-name"]);

                
                //This is added to load the .exe and .dll providers
                //to keep previous provider running this bad chunk of code is written
                //later on you can directly provide the provider name read from config.
                string extension = ".dll";
                if (properties.Contains("full-name"))
                {
                    extension = Path.GetExtension(Convert.ToString(properties["full-name"]));
                }
                   
                IDictionary startupparams = properties["parameters"] as IDictionary;
                if (startupparams == null) startupparams = new Hashtable();


                if (extension.EndsWith(".dll") || extension.EndsWith(".exe"))
                {
                    AuthenticateFeature.Authenticate(LanguageContext.DOTNET);

                    _languageContext = LanguageContext.DOTNET;
                    try
                    {

                        //for client cache read-thru provider
                        if (classname.Contains("Alachisoft.NCache.Web.ClientCache.ClientCacheReadThru") && InternalProviderFactory.Instance != null)
                        {
                            _dsReader = InternalProviderFactory.Instance.CreateReadThruProvider();
                        }
                        else
                        {
                            string path = AppUtil.DeployedAssemblyDir + _cacheName + GetReadThruAssemblyPath(assembly) + extension;

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
                                        AssemblyName nameOfAssembly = new AssemblyName();
                                        nameOfAssembly.Name = assembly;
                                        asm = Assembly.Load(nameOfAssembly);
                                    }
                                    catch (Exception)
                                    {
                                        try
                                        {
                                            string version = Assembly.GetExecutingAssembly().ImageRuntimeVersion;
                                            path = Path.Combine(AppUtil.InstallDir, "bin");
                                            path = Path.Combine(path, "assembly");
                                            if (version.Contains("v4"))
                                                path = Path.Combine(path, "4.0") + GetReadThruAssemblyPath(assembly) + extension;
                                            else
                                                path = Path.Combine(path, "2.0") + GetReadThruAssemblyPath(assembly) + extension;
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
                                _dsReader = (IReadThruProvider)asm.CreateInstance(classname);
                            if (_dsReader == null)
                                throw new Exception("Unable to instantiate " + classname);

                            if (asm != null)
                                DatasourceMgr.AssemblyCache[assembly] = asm;
                        }

                        _dsReader.Init(startupparams,_cacheName);
                    }
                    catch (InvalidCastException)
                    {
                        throw new ConfigurationException("The class specified in read-thru does not implement IDatasourceReader");
                    }
                    catch (TargetInvocationException e)
                    {
                        throw e;
                    }
                    catch (Exception e)
                    {
                        throw new ConfigurationException(e.Message, e);
                    }
                }

               
              
            }
            catch (ConfigurationException e)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Configuration Error: " + e.ToString(), e);
            }
        }

        #endregion

        private string GetReadThruAssemblyPath(string asm)
        {
            string path = Path.DirectorySeparatorChar.ToString();
            string[] folderNames = asm.Split(new char[] { ',', '=' });
            path = path + folderNames[0];
            return path;
        }

        /// <summary>
        /// Responsible for loading the object from the external data source. 
        /// Key is passed as parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// 
        public void ReadThru(string key, out ProviderCacheItem item, OperationContext operationContext)
        {
            item = null;
            try
            {
                if (_dsReader is ICustomReadThru)
                {
                    if (operationContext.Contains(OperationContextFieldName.ReadThru))
                        ((ICustomReadThru)_dsReader).DoReadThru =
                            Convert.ToBoolean(operationContext.GetValueByField(OperationContextFieldName.ReadThru));
                    if (operationContext.Contains(OperationContextFieldName.ReadThru))
                        ((ICustomReadThru)_dsReader).ProviderName =
                            operationContext.GetValueByField(OperationContextFieldName.ReadThruProviderName) as string;

                    if (operationContext.Contains(OperationContextFieldName.GroupInfo))
                    {
                        GroupInfo gi = operationContext.GetValueByField(OperationContextFieldName.GroupInfo) as GroupInfo;
                        if (gi != null)
                        {
                            ((ICustomReadThru)_dsReader).Group = gi.Group;
                            ((ICustomReadThru)_dsReader).SubGroup = gi.SubGroup;
                        }
                    }
                }

                Stopwatch readThruWatch = new Stopwatch();
                readThruWatch.Start();

                _dsReader.LoadFromSource(key, out item);
                readThruWatch.Stop();
                double elapsedByReadThru = readThruWatch.Elapsed.TotalSeconds;

                if (elapsedByReadThru > ServiceConfiguration.CommandExecutionThreshold &&
                    ServiceConfiguration.EnableCommandThresholdLogging)
                {
                    if (_context.NCacheLog != null)
                        _context.NCacheLog.Warn("ReadThruProviderMgr.ReadThru",
                            "ReadThru took " + elapsedByReadThru +
                            " seconds to complete. Which is longer than expected.");
                }

                this._context.PerfStatsColl.IncrementReadThruPerSec();

            }
            catch (Exception e)
            {
                //Client doesnt throw the inner exception
                //Client casts the thrown exception message into Operation failed Exception therefore the current inner exception will be casted 
                //in Operation failed exception > Inner Exception > Inner Exception
                throw new OperationFailedException("IReadThruProvider.LoadFromSource failed. Error: " + e.ToString(), e);
            }
            finally
            {
                try
                {
                    // reset all here
                    ((ICustomReadThru)_dsReader).DoReadThru = false;
                    ((ICustomReadThru)_dsReader).Group = null;
                    ((ICustomReadThru)_dsReader).SubGroup = null;
                    ((ICustomReadThru)_dsReader).ProviderName = null;
                }
                catch {}
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="exh"></param>
        /// <param name="evh"></param>
        /// <returns></returns>
        public Dictionary<string, ProviderCacheItem> ReadThru(string[] keys, OperationContext operationContext)
        {
            Dictionary<string, ProviderCacheItem> cacheItems = null;
            try
            {
                if (_dsReader is ICustomReadThru)
                {
                    if (operationContext.Contains(OperationContextFieldName.ReadThru))
                        ((ICustomReadThru)_dsReader).DoReadThru =
                            Convert.ToBoolean(operationContext.GetValueByField(OperationContextFieldName.ReadThru));
                    if (operationContext.Contains(OperationContextFieldName.ReadThru))
                        ((ICustomReadThru)_dsReader).ProviderName =
                            operationContext.GetValueByField(OperationContextFieldName.ReadThruProviderName) as string;
                }

                System.Diagnostics.Stopwatch readThruWatch = new System.Diagnostics.Stopwatch();
                readThruWatch.Start();
                cacheItems = _dsReader.LoadFromSource(keys);
                readThruWatch.Stop();
                double elapsedByReadThru = readThruWatch.Elapsed.TotalSeconds;

                if (elapsedByReadThru > ServiceConfiguration.CommandExecutionThreshold &&
                    ServiceConfiguration.EnableCommandThresholdLogging)
                {
                    if (_context.NCacheLog != null)
                        _context.NCacheLog.Warn("ReadthruProviderMgr.ReadThru",
                            "ReadThru took " + elapsedByReadThru +
                            " seconds to complete. Which is longer than expected.");
                }
                this._context.PerfStatsColl.IncrementReadThruPerSecBy(keys.Length);	

            }
            catch (Exception e)
            {
            }
            return cacheItems;
        }

        public IReadThruProvider DSReader
        {
            get { return _dsReader; }
        }
    }
}
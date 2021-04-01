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
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Monitoring.APILogging;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Common.DataSource;

namespace Alachisoft.NCache.SocketServer.RuntimeLogging
{

    public partial class APILogItemBuilder
    {
        private object _lock = new object();
        private static IDictionary<string, IDictionary<string, List<OverloadInfo>>> _methodDic;
        
        /// <summary>
        /// Name of method which is called by server
        /// </summary>
        private string _methodName = null;

        public APILogItemBuilder(string methodName)
        {
            if (_methodDic ==null)
            {
                _methodDic = CacheServer.MethodsDictionary;
            }
            _methodName = methodName;
        }
        public APILogItemBuilder()
        {
        }

        public void GenerateADDInsertAPILogItem(string key, object value, ArrayList dependency, long absoluteExpiration, long slidingExpiration, CacheItemPriority priority, Hashtable Tags, string group, string subGroup, BitSet dsWriteOption, string providerName, string resyncProviderName,
            bool isResyncExpiredItems, Hashtable namedTags, short ondatasourceitemupdatedcallback, short onDataSourceItemAdded, bool asynCall, bool itemUpdate, bool itemRemove, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();
           
            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            

            foreach (Parameters param in parameter)
            {
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("key", key);
                    }

                    else if (parameterName.Equals("dswriteoption", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (providerName == null)
                            providerName = "null";

                        Tuple<string, BitSet> writeThruOptionsTuple = new Tuple<string, BitSet>(providerName, dsWriteOption);
                        logItemParameters.Add("dswriteoption", writeThruOptionsTuple);
                    }

                    else if (parameterName == "item")
                    {
                        if (absoluteExpiration == -1 && slidingExpiration == -1)
                        {
                            Tuple<ExpirationType, long> expirationTuple = new Tuple<ExpirationType, long>(ExpirationType.None, -1);
                            logItemParameters.Add("expiration", expirationTuple);
                        }
                        if (absoluteExpiration != -1 && !logItemParameters.ContainsKey("expiration"))
                        {
                            DateTime dateTimeExpiration = new DateTime(absoluteExpiration);
                            Tuple<ExpirationType, long> expirationTuple = new Tuple<ExpirationType, long>(ExpirationType.Absolute, dateTimeExpiration.Subtract(DateTime.UtcNow).Ticks);
                            logItemParameters.Add("expiration", expirationTuple);
                        }
                        if (slidingExpiration != -1 && !logItemParameters.ContainsKey("expiration"))
                        {
                            Tuple<ExpirationType, long> expirationTuple = new Tuple<ExpirationType, long>(ExpirationType.Sliding, slidingExpiration);
                            logItemParameters.Add("expiration", expirationTuple);
                        }
                        logItemParameters.Add("priority", priority);
                        logItemParameters.Add("value", value);
                        if (Tags != null && !logItemParameters.ContainsKey("tags")) logItemParameters.Add("tags", Tags);
                        if (namedTags != null && !logItemParameters.ContainsKey("namedtags")) logItemParameters.Add("namedtags", namedTags);
                        if (group != null && !logItemParameters.ContainsKey("group")) logItemParameters.Add("group", group);
                        if (subGroup != null && !logItemParameters.ContainsKey("subgroup")) logItemParameters.Add("subgroup", subGroup);
                        if (dependency != null && !logItemParameters.ContainsKey("dependency")) logItemParameters.Add("dependency", dependency);
                        if (resyncProviderName != null) logItemParameters.Add("resyncProviderName", resyncProviderName);
                        if (!logItemParameters.ContainsKey("ondatasourceitemupdatedcallback") && (_methodName.ToLower() == "insert" || _methodName.ToLower() == "insertasync"))
                        {
                            if (onDataSourceItemAdded != -1)
                                logItemParameters.Add("ondatasourceitemupdatedcallback", "Avaliable");
                            else
                                logItemParameters.Add("ondatasourceitemupdatedcallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("onasyncitemupdatecallback") && (_methodName.ToLower() == "insert" || _methodName.ToLower() == "insertasync"))
                        {
                            if (onDataSourceItemAdded != -1)
                                logItemParameters.Add("onasyncitemupdatecallback", "Avaliable");
                            else
                                logItemParameters.Add("onasyncitemupdatecallback", "NA");
                        }

                        if (!logItemParameters.ContainsKey("ondatasourceitemupdatedcallback") && (_methodName.ToLower() == "insert" || _methodName.ToLower() == "insertasync"))
                        {
                            if (ondatasourceitemupdatedcallback != -1)
                                logItemParameters.Add("ondatasourceitemupdatedcallback", "Avaliable");
                            else
                                logItemParameters.Add("ondatasourceitemupdatedcallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("ondatasourceitemadded") && (_methodName.ToLower() == "add" || _methodName.ToLower() == "addasync"))
                        {
                            if (onDataSourceItemAdded != -1)
                                logItemParameters.Add("ondatasourceitemadded", "Avaliable");
                            else
                                logItemParameters.Add("ondatasourceitemadded", "NA");
                        }


                        if (!logItemParameters.ContainsKey("asyncitemupdatecallback") && (_methodName.ToLower() == "insert" || _methodName.ToLower() == "insertasync"))
                        {
                            if (asynCall)
                                logItemParameters.Add("asyncitemupdatecallback", "Avaliable");
                            else
                                logItemParameters.Add("asyncitemupdatecallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("onasyncItemAddCallback") && (_methodName.ToLower() == "add" || _methodName.ToLower() == "addasync"))
                        {
                            if (asynCall)
                                logItemParameters.Add("onasyncitemAddcallback", "Avaliable");
                            else
                                logItemParameters.Add("onasyncitemaddcallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("itemremovecallback"))
                        {
                            if (itemRemove)
                                logItemParameters.Add("itemremovecallback", "Avaliable");
                            else
                                logItemParameters.Add("itemremovecallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("itemupdatecallback"))
                        {
                            if (itemUpdate)
                                logItemParameters.Add("itemupdatecallback", "Avaliable");
                            else
                                logItemParameters.Add("itemupdatecallback", "NA");
                        }

                    }
                  
                }
            }
             logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem); 
       
        }

       

        public void GenerateADDInsertBulkAPILogItem(int keys, int value, ArrayList dependency, long absoluteExpiration, long slidingExpiration, CacheItemPriority priority, Hashtable Tags, Hashtable namedTags,string group, string subGroup, BitSet dsWriteOption, string providerName,
            string resyncProviderName, bool isResyncExpiredItems, bool allowQueryTags,long version, short ondatasourceitemupdatedcallback, short ondatasourceitemadded, bool itemUpdate, bool itemRemove, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("keys",StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (keys >0)  logItemParameters.Add("keys", keys);
                    }
                    
                    else if (parameterName == "items")
                    {
                        if (absoluteExpiration == -1 && slidingExpiration == -1)
                        {
                            Tuple<ExpirationType, long> expirationTuple = new Tuple<ExpirationType, long>(ExpirationType.None, -1);
                            logItemParameters.Add("expiration", expirationTuple);
                        }
                        if (absoluteExpiration != -1 && !logItemParameters.ContainsKey("expiration"))
                        {
                            DateTime dateTimeExpiration = new DateTime(absoluteExpiration);
                            Tuple<ExpirationType, long> expirationTuple = new Tuple<ExpirationType, long>(ExpirationType.Absolute, dateTimeExpiration.Subtract(DateTime.UtcNow).Ticks);
                            logItemParameters.Add("expiration", expirationTuple);
                        }
                        if (slidingExpiration != -1 && !logItemParameters.ContainsKey("expiration"))
                        {
                            Tuple<ExpirationType, long> expirationTuple = new Tuple<ExpirationType, long>(ExpirationType.Sliding, slidingExpiration);
                            logItemParameters.Add("expiration", expirationTuple);
                        }
                        logItemParameters.Add("priority", priority);
                        if (value > 0) logItemParameters.Add("value", value);
                        if (Tags != null && !logItemParameters.ContainsKey("tags")) logItemParameters.Add("tags", Tags);
                        if (namedTags != null && !logItemParameters.ContainsKey("namedtags")) logItemParameters.Add("namedtags", namedTags);
                        if (group != null && !logItemParameters.ContainsKey("group")) logItemParameters.Add("group", group);
                        if (subGroup != null && !logItemParameters.ContainsKey("subgroup")) logItemParameters.Add("subgroup", subGroup);
                        if (dependency != null && !logItemParameters.ContainsKey("dependency")) logItemParameters.Add("dependency", dependency);
                        //  if (vers != null) logItemParameters.Add("version", version);
                        if (resyncProviderName != null) logItemParameters.Add("resyncProviderName", resyncProviderName);
                        if (version != null) logItemParameters.Add("version", version);

                        if (!logItemParameters.ContainsKey("ondatasourceitemupdatedcallback") && _methodName.ToLower() == "insertbulk")
                        {
                            if (ondatasourceitemupdatedcallback != -1)
                                logItemParameters.Add("ondatasourceitemupdatedcallback", "Avaliable");
                            else
                                logItemParameters.Add("ondatasourceitemupdatedcallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("ondatasourceitemadded") && _methodName.ToLower() == "addbulk")
                        {
                            if (ondatasourceitemadded != -1)
                                logItemParameters.Add("ondatasourceitemadded", "Avaliable");
                            else
                                logItemParameters.Add("ondatasourceitemadded", "NA");
                        }

                        if (!logItemParameters.ContainsKey("ondatasourceitemsupdatedcallback") && _methodName.ToLower() == "insertbulk")
                        {
                            if (ondatasourceitemupdatedcallback != -1)
                                logItemParameters.Add("ondatasourceitemsupdatedcallback", "Avaliable");
                            else
                                logItemParameters.Add("ondatasourceitemsupdatedcallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("ondatasourceitesmadded") && _methodName.ToLower() == "addbulk")
                        {
                            if (ondatasourceitemadded != -1)
                                logItemParameters.Add("ondatasourceitemsadded", "Avaliable");
                            else
                                logItemParameters.Add("ondatasourceitemsadded", "NA");
                        }
                        if (!logItemParameters.ContainsKey("asyncitemupdatecallback") && (_methodName.ToLower() == "insert" || _methodName.ToLower() == "insertasync"))
                        {
                            if (ondatasourceitemupdatedcallback != -1)
                                logItemParameters.Add("asyncitemupdatecallback", "Avaliable");
                            else
                                logItemParameters.Add("asyncitemupdatecallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("asyncItemAddCallback") && (_methodName.ToLower() == "add" || _methodName.ToLower() == "addasync"))
                        {
                            if (ondatasourceitemadded != -1)
                                logItemParameters.Add("asyncitemaddcallback", "Avaliable");
                            else
                                logItemParameters.Add("asyncitemaddcallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("asyncitemupdatecallback") && (_methodName.ToLower() == "insert" || _methodName.ToLower() == "insertasync"))
                        {
                            if (ondatasourceitemadded != -1)
                                logItemParameters.Add("asyncitemupdatecallback", "Avaliable");
                            else
                                logItemParameters.Add("asyncitemupdatecallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("itemremovecallback"))
                        {
                            if (itemRemove)
                                logItemParameters.Add("itemremovecallback", "Avaliable");
                            else
                                logItemParameters.Add("itemremovecallback", "NA");
                        }
                        if (!logItemParameters.ContainsKey("itemupdatecallback"))
                        {
                            if (itemUpdate)
                                logItemParameters.Add("itemupdatecallback", "Avaliable");
                            else
                                logItemParameters.Add("itemupdatecallback", "NA");
                        }
                        
                    }
                    else if (parameterName.Equals("dswriteoption", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (providerName == null)
                            providerName = "null";

                        Tuple<string, BitSet> writeThruOptionsTuple = new Tuple<string, BitSet>(providerName, dsWriteOption);
                        logItemParameters.Add("dswriteoption", writeThruOptionsTuple);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem); 
        }

        public void GenerateAddAttributeAPILogItem(string key, long absoluteExpiration,ArrayList dependency, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {
                parameterName = param.ParameterName.ToLower();
                if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                {
                   logItemParameters.Add("key",key);
                }
                else if (parameterName.Equals("attributes", StringComparison.InvariantCultureIgnoreCase) || parameterName == "attributes")
                {
                    if (absoluteExpiration!=-1) logItemParameters.Add("expiration", absoluteExpiration);
                    if (dependency != null) logItemParameters.Add("dependency", dependency);
                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem); 
        }

        public void GenerateAddDependencyAPILogItem(string key, bool isResyncRequired, ArrayList dependency,  string syncDependency, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            
            foreach (Parameters param in parameter)
            {
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                 if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                  {
                     logItemParameters.Add("key",key);
                  }
                 
                  else if (parameterName.Equals("dependency",StringComparison.InvariantCultureIgnoreCase))
                  {
                      logItemParameters.Add("dependency",dependency);
                  
                  }
                  else if (parameterName.Equals("isResyncRequired",StringComparison.InvariantCultureIgnoreCase))
                  {
                      logItemParameters.Add("isresyncrequired",isResyncRequired);
                  }
                 
                  
                }
            }
             logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem); 
        }

        public void GenerateBulkDeleteAPILogItem(int keys, BitSet dsWriteOption, string providerName,short onDataSourceItemRemovedCallback, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("keys",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("keys",keys);
                    }
                    else if (parameterName.Equals("dswriteoption", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (providerName == null)
                            providerName = "null";

                        Tuple<string, BitSet> writeThruOptionsTuple = new Tuple<string, BitSet>(providerName, dsWriteOption);
                        logItemParameters.Add("dswriteoption", writeThruOptionsTuple);
                    }
                    else if (parameterName.Equals("onDataSourceItemRemovedCallback ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("ondatadourceitemremovedcallback", onDataSourceItemRemovedCallback);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
     
        internal void GenerateBulkGetAPILogItem(int keys, string providerName, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int result)
        {

            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("keys",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("keys",keys);
                    }
                    else if (parameterName.Equals("dsreadoption",StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (providerName == null)
                            providerName = "null";

                       
                       
                    }
                }
            }
            logItemParameters.Add("result", result);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateBulkRemoveAPILogItem(int keys, BitSet dsWriteOption, string providerName, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("keys",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("keys",keys);
                    }
                    else if (parameterName.Equals("dswriteoption", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (providerName == null)
                            providerName = "null";

                        Tuple<string, BitSet> writeThruOptionsTuple = new Tuple<string, BitSet>(providerName, dsWriteOption);
                        logItemParameters.Add("dswriteoption", writeThruOptionsTuple);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateClearAPILogItem(BitSet updateOpt, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
              List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("updateOpt",StringComparison.InvariantCultureIgnoreCase))
                    {
                        Tuple<string, BitSet> writeThruOptionsTuple = new Tuple<string, BitSet>(null, updateOpt);
                        logItemParameters.Add("dswriteoption", writeThruOptionsTuple);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateClearAsyncAPILogItem(BitSet updateOpt,short id, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("updateopt",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("updateopt",updateOpt);
                    }
                    else  if (parameterName.Equals("onasynccacheclearcallback",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("onasynccacheclearcallback", "Avaliable");
                    }

                    else if (parameterName.Equals("dataSourceClearedCallback", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (id!=-1 )
                        logItemParameters.Add("datasourceclearedcallback", "Avaliable");
                        else
                            logItemParameters.Add("datasourceclearedcallback", "NA");
                    }


                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateOpenStreamAPILogItem(string key, int mode, string group, string subGroup, int priority, ArrayList dependency, long absExpiration, long sldExpiration, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("key", key);
                    }
                    else if (parameterName.Equals("streammode", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("streammode", mode);
                    }
                    else if (parameterName.Equals("group", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("group", group);
                    }
                    else if (parameterName.Equals("subgroup", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("subgroup", subGroup);
                    }
                    else if (parameterName.Equals("priority", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("priority", priority);
                    }
                    else if (parameterName.Equals("slidingExpiration", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("slidingExpiration", sldExpiration);
                    }
                    else if (parameterName.Equals("absoluteExpiration", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("absoluteExpiration", absExpiration);

                    }
                    else if (parameterName.Equals("dependency", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("dependency", dependency);
                    }

                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateCloseStreamAPILogItem(string key, string lockHandle, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("key",key);
                    }
                    else if (parameterName.Equals("lockhandle",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("lockhandle", lockHandle);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
     
        public void GenerateContainsCommandAPILogItem(string key, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("key",key);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
    
        public void GenerateDeleteAPILogItem(string key, BitSet dsWriteOption,  object lockHandle, long version, LockAccessType accessType, string providerName, short onDataSourceItemRemovedCallback,  int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("key",key);
                    }
                    else if (parameterName.Equals("lockhandle",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("lockhandle",lockHandle);
                    }
                    else if (parameterName.Equals("dswriteoption", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (providerName == null)
                            providerName = "null";

                        Tuple<string, BitSet> writeThruOptionsTuple = new Tuple<string, BitSet>(providerName, dsWriteOption);
                        logItemParameters.Add("dswriteoption", writeThruOptionsTuple);
                    }
                    else if (parameterName.Equals("version",StringComparison.InvariantCultureIgnoreCase))
                    {
                          logItemParameters.Add("version",version);
                    }
                    else if (parameterName.Equals("accesstype",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("accesstype",accessType);
                    }
                    else if (parameterName.Equals("onDataSourceItemRemovedCallback ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("ondatadourceitemremovedcallback", onDataSourceItemRemovedCallback);
                    }

                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
      
        public void GenerateDeleteQueryAPILogItem(string query, IDictionary values, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {

            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("query", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("query", query);
                    }
                    else if (parameterName.Equals("values", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("values", values);
                    }
                   
                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }
    
        public void GenerateDisposeAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }


        public void GenerateGetRunningTasksAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateDisposeReaderAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateExecuteReaderAPILogItem(string query, IDictionary values, bool getData, int chunkSize,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int resultCount)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("query",query);
                    }
                    else if (parameterName.Equals("values",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("values",values);
                    }
                    else if (parameterName.Equals("getdata",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("getdata",getData);
                    }
                     else if (parameterName.Equals("chunksize",StringComparison.InvariantCultureIgnoreCase))
                    {
                          logItemParameters.Add("chunksize",chunkSize);
                    }
                }
            }
            logItemParameters.Add("result", resultCount);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
     
        public void GenerateExecuteReaderCQAPILogItem(string query, IDictionary values, bool getData, int chunkSize, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int resultCount)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("cquery",StringComparison.InvariantCultureIgnoreCase))

                    {
                        logItemParameters.Add("cquery",query);
                    }
                    else if (parameterName.Equals("values",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("values",values);
                    }
                    else if (parameterName.Equals("getdata",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("getdata",getData);
                    }
                    else if (parameterName.Equals("chunksize",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("chunksize",chunkSize);
                    }
                }
            }
            logItemParameters.Add("result", resultCount);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        internal void GenerateGetCacheItemAPILogItem(string key, string group, string subGroup,  long version, LockAccessType accessType, TimeSpan lockTimeout,  string lockHandle, string providerName,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP,int result)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("key",key);
                    }
                    else if (parameterName.Equals("group",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("group",group);
                    }
                    else if (parameterName.Equals("subgroup",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("subgroup",subGroup);
                    }
                  
                    else if (parameterName.Equals("version",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("version",version);
                    }
                    else if (parameterName.Equals("accesstype",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("accesstype",accessType);
                    }
                    else if (parameterName.Equals("lockhandle",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("lockhandle",lockHandle);
                    }
                     else if (parameterName.Equals("locktimeout",StringComparison.InvariantCultureIgnoreCase))
                    {
                          logItemParameters.Add("locktimeout",lockTimeout);
                    }
                }
            }
            logItemParameters.Add("result", result);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateGetCacheStreamAPILogItem(string key, string group, string subGroup, Dependency dependency, long absoluteExpiration, long slidingExpiration, CacheItemPriority priority,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("key",key);
                    }
                    else if (parameterName.Equals("group",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("group",group);
                    }
                    else if (parameterName.Equals("subgroup",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("subgroup",subGroup);
                    }
                    else if (parameterName.Equals("dependency",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("dependency",dependency);
                    }
                    else if (parameterName.Equals("absoluteexpiration",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("absoluteexpiration",absoluteExpiration);
                    }
                    else if (parameterName.Equals("slidingexpiration",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("slidingexpiration",slidingExpiration);
                    }
                     else if (parameterName.Equals("priority",StringComparison.InvariantCultureIgnoreCase))
                    {
                          logItemParameters.Add("priority",priority);
                    }
                   
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
  
        internal void GenerateGetCommandAPILogItem(string key, string group, string subGroup,  long version, LockAccessType accessType, TimeSpan lockTimeout,  object lockHandle, string providerName,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP,int result)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("key",key);
                    }
                    else if (parameterName.Equals("group",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("group",group);
                    }
                    else if (parameterName.Equals("subgroup",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("subgroup",subGroup);
                    }
                   
                    else if (parameterName.Equals("accesstype",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("accesstype",accessType);
                    }
                    else if (parameterName.Equals("locktimeout",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("locktimeout",lockTimeout);
                    }
                    else if (parameterName.Equals("lockhandle",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("lockhandle",lockHandle);
                    }
                    else if (parameterName.Equals("version", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("version", version);
                    }
                   
                }
            }
            logItemParameters.Add("result", result);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }
  
        public void GenerateGetEnumeratorAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
             logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
   
        public void GenerateGetGroupKeysAPILogItem(string group, string subGroup,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP,int result)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("group",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("group",group);
                    }
                    else if (parameterName.Equals("subgroup",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("subgroup",subGroup);
                    }
                }
            }
            logItemParameters.Add("result", result);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
   
        public void GenerateGetGroupDataAPILogItem(string group, string subGroup,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP,int result)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("group",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("group",group);
                    }
                    else if (parameterName.Equals("subgroup",StringComparison.InvariantCultureIgnoreCase))
                    {
                         logItemParameters.Add("subgroup",subGroup);
                    }
                }
            }
            logItemParameters.Add("result", result);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);

        }

        public void GenerateGetByTagAPILogItem(Hashtable tag,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP,int result)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("tag",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("tag",tag);
                    }
                }
            }
              logItemParameters.Add("result", result);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateGetByAnyTagAPILogItem(Hashtable tag, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP,int result)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("tags",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("tags",tag);
                    }
                }
            }
            logItemParameters.Add("result", result);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }
  
        public void GenerateGetkeysByTagsAPILogItem(Hashtable tag,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int result)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("tag", StringComparison.InvariantCultureIgnoreCase) || parameterName.Equals("tags", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("tags",tag);
                    }
                }
            } 
            logItemParameters.Add("result", result);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
 
        public void GenerateGetByAllTagsAPILogItem(Hashtable tags,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP,int result)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("tags",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("tags",tags);
                    }
                }
            }
            logItemParameters.Add("result", result);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }
     
        public void GenerateRemoveByAnyTagAPILogItem(Hashtable tags,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("tags",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("tags",tags);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
    
        public void GenerateRemoveByAllTagsAPILogItem(Hashtable tags,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName =_methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("tags",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("tags",tags);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
    
        public void GenerateRemoveByTagAPILogItem(Hashtable tag,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("tag",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("tag",tag);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        internal void GenerateGetIfNewerAPILogItem(string key, string group, string subGroup, long version, LockAccessType accessType, TimeSpan lockTimeout,  LockHandle lockHandle, string providerName,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
             List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("key",key);
                    }
                    else  if (parameterName.Equals("group",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("group",group);
                    }
                    else  if (parameterName.Equals("subgroup",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("subgroup",subGroup);
                    }
                   
                    else  if (parameterName.Equals("version",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("version",version);
                    }
                    else  if (parameterName.Equals("accesstype",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("accesstype",accessType);
                    }
                    else  if (parameterName.Equals("locktimeout",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("locktimeout",lockTimeout);
                    }
                     else  if (parameterName.Equals("lockhandle",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("lockhandle",lockHandle);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateGetTaskResultAPILogItem(string taskId,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
             List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("taskid",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("taskid",taskId);
                    }
                   
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateGetRunningTaskAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateGetConnectedClientsAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }


        public void GenerateExecuteTaskAPILogItem(string task, string keyFilter, string query, byte[] parameters, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("task",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("task",task);
                    }
                    
                    else if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("query",query);
                    }
                    else if (parameterName.Equals("parameters",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("parameters",parameters);

                    }
                    else if (parameterName.Equals("keyfilter",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("keyfilter",keyFilter);
                    }
                   
                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);


        }

        public void GenerateUnRegisterCQAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            //foreach (Parameters param in parameter) {
               
            //    if (param.ParameterName != null)
            //    {
            //        parameterName = param.ParameterName.ToLower();
            //        if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase)))
            //        {
            //            logItemParameters.Query=query;
            //        }
                   
            //    }
            //}
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
      
        public void GenerateRegisterCQAPILogItem(string query,IDictionary values, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("query",query);
                    }
                    else if (parameterName.Equals("values", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("values", values);
                    }
                   
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateSearchCQAPILogItem(string query,IDictionary values, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int resultCount)
        {
             List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("query",query);
                    }
                    else if (parameterName.Equals("values", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("values", values);
                    }
                   
                }
            }
            logItemParameters.Add("result", resultCount);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateSearchEntriesCQAPILogItem(string query, IDictionary values,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int resultCount)
        {
             List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("query",query);
                    }
                    else if (parameterName.Equals("values",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("values",values);
                    }
                   
                }
            }
            logItemParameters.Add("result", resultCount);
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }
      
        public void GenerateSearchEntriesAPILogItem(string query, IDictionary values, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int resultCount)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("query",query);
                    }
                    else if (parameterName.Equals("values",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("values",values);
                    }

                }
            }
            logItemParameters.Add("result", resultCount);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GeneratUnlockAPILogItem(string key,string lockHandle,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("key",key);
                    }
                    else if (parameterName.Equals("lockhandle", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("lockhandle", lockHandle);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateLockAPILogItem(string key, TimeSpan lockTimeout, object lockHandle,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("key",key);
                    }
                    else if (parameterName.Equals("locktimeout",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("locktimeout",lockTimeout);
                    }
                    else if (parameterName.Equals("lockhandle",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("lockhandle",lockHandle);
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);

        }

        public void GenerateRemoveAPILogItem(string key, BitSet dsWriteOption, object lockHandle, long version, LockAccessType accessType, string providerNames,short onDataSourceItemRemovedCallback, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
             List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("key",key);
                    }
                    else if (parameterName.Equals("dswriteoption", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (providerNames == null)
                            providerNames = "NA";

                        Tuple<string, BitSet> writeThruOptionsTuple = new Tuple<string, BitSet>(providerNames, dsWriteOption);
                        logItemParameters.Add("dswriteoption", writeThruOptionsTuple);
                    }
                    else if (parameterName.Equals("lockhandle",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("lockhandle",lockHandle);
                    }
                    else if (parameterName.Equals("version",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("version",version);
                    }
                    else if (parameterName.Equals("accesstype",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("accesstype",accessType);
                    }
                    else if (parameterName.Equals("onDataSourceItemRemovedCallback", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (onDataSourceItemRemovedCallback != -1)
                        {
                            logItemParameters.Add("ondatadourceitemremovecallback", "Avaliable");
                        }
                        else
                        {
                            logItemParameters.Add("ondatadourceitemremovecallback", "NA");
                        }
                    }
                    else if (parameterName.Equals("onasyncitemremovecallback", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (onDataSourceItemRemovedCallback != -1)
                        {
                            logItemParameters.Add("onasyncitemremovecallback", "Avaliable");

                        }
                        else
                        {

                            logItemParameters.Add("onasyncitemremovecallback", "NA");
                        }
                    }
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GeneratExecuteNonQueryAPILogItem(string query, IDictionary values,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("query",query);
                    }
                    else if (parameterName.Equals("values",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("values",values);
                    }
                   
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);

        }

        public void GeneratRemoveGroupDataAPILogItem(string group, string subGroup,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
           List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("group",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("group",group);
                    }
                    else if (parameterName.Equals("subgroup",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("subgroup",subGroup);
                    }
                   
                }
            }
              logItem.RuntimeParameters = logItemParameters;
             AddAPILogToManager(logItem);
        }

        public void GenerateSearchAPILogItem(string query, IDictionary values,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int resultCount)
        {
            List <Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName =null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem= new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter) {
               
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("query",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("query",query);
                    }
                    else if (parameterName.Equals("values",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("values",values);
                    }
                   
                }
            }
            logItemParameters.Add("result", resultCount);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateraiseCustomAPILogItem(object notifId, object data,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
         Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode =GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("notifid",StringComparison.InvariantCultureIgnoreCase))
                    {
                       logItemParameters.Add("notifid",notifId);
                    }
                    else if (parameterName.Equals("data",StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("data",data);
                    }

                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

       
        public void GenerateCacheCountAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, long result)
        {
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItemParameters.Add("result", result);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateCacheSyncDependencyAPILogItem(string remotecacheid ,string key, string passwrod, string userid,ArrayList dependency, bool isResyncRequired,int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("key", key);
                    }
                    if (parameterName.Equals("isResyncRequired", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("isresyncrequired", isResyncRequired);
                    }
                    if (parameterName.Equals("dependency", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("dependency", dependency);
                    }
                    else if (parameterName.Equals("syncDependency", StringComparison.InvariantCultureIgnoreCase))
                    { 
                        if (remotecacheid!=null)
                        {
                            logItemParameters.Add("remotecacheid", remotecacheid);
                        }
                        if (userid!=null)
                        {
                            logItemParameters.Add("userid", userid);
                        }
                         if (passwrod!=null)
                        {
                            logItemParameters.Add("password", passwrod);
                        }
                    }

                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateCollectionNotification(string collectionName, short addCallback, short updateCallback, short removeCallback, string exceptionMessage, TimeSpan executionTime, string clientId, string clientIp)
        {
            var parameters = new Hashtable();
            var logItem = new APIRuntimeLogItem(exceptionMessage);

            if (string.IsNullOrEmpty(collectionName))
                collectionName = "NA";

            if (string.IsNullOrEmpty(clientId))
                logItem.ClientID = GetClietnProcessID(clientId);

            if (string.IsNullOrEmpty(clientIp))
                logItem.ClientNode = GetClientIP(clientIp);

            logItem.MethodName = _methodName;
            logItem.GeneratedTime = DateTime.Now;
            logItem.ExecutionTime = executionTime;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();

            parameters["collectionName"] = collectionName;
            parameters["addcallback"] = addCallback == -1 ? "NA" : "Available";
            parameters["updatecallback"] = updateCallback == -1 ? "NA" : "Available";
            parameters["removecallback"] = removeCallback == -1 ? "NA" : "Available";

            logItem.RuntimeParameters = parameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateKeyNotificationCallback(int key, short updateCallback, short removeCallback, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("key", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("key", key);
                    }
                    else if (parameterName.Equals("keys", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("keys", key);
                    }
                    else if (parameterName.Equals("updatecallback", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (updateCallback != -1)
                        {
                            logItemParameters.Add("updatecallback", "Avaliable");
                        }
                        else
                            logItemParameters.Add("updatecallback", "NA");

                    }
                    else if (parameterName.Equals("removeCallback", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (removeCallback != -1)
                            logItemParameters.Add("removecallback", "Availiable");
                        else
                            logItemParameters.Add("removecallback", "NA");
                    }

                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateRegisterCacheNotificationCallback(short cachedatanotificationcallback, string eventtype, string datafilter,  int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();

                    if (parameterName.Equals("cachedatanotificationcallback", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (cachedatanotificationcallback != -1)
                        {
                            logItemParameters.Add("cachedatanotificationcallback", "Avaliable");
                        }
                        else
                            logItemParameters.Add("cachedatanotificationcallback", "NA");

                    }

                    else if (parameterName.Equals("eventtype", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("eventtype", eventtype);
                    }
                    else if (parameterName.Equals("datafilter", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("datafilter", datafilter);
                    }

                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateGetReaderChunkCommand(string readerId, int result, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("readerId", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("readerid", readerId);
                    }

                }
            }
            logItemParameters.Add("result", result);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }
      


        public void GenerateCommandManagerLog(string methodName, string clientId, string clientIP, TimeSpan executionTime, string exceptionMesage)
        {
            APIRuntimeLogItem logItem = new APIRuntimeLogItem(exceptionMesage);
            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);
            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);
            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodName = methodName;
            AddAPILogToManager(logItem);
        }

        public void GenerateConnectionManagerLog(ClientManager clientManager, string exceptionMesage)
        {
            APIRuntimeLogItem logItem = new APIRuntimeLogItem(exceptionMesage);
            if (clientManager != null)
            {
                if (clientManager.ClientID != null)
                    logItem.ClientID = GetClietnProcessID(clientManager.ClientID.ToLower());
                if (clientManager.ClientSocketId != null)
                    logItem.ClientNode = GetClientIP(clientManager.ClientSocketId.ToString());
            }
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            AddAPILogToManager(logItem);
        }

        List<Parameters> GetMisMatchAsssembyOVerload (string method,string className)
        {
            List<OverloadInfo> overloadinfo = new List<OverloadInfo>();
            List<Parameters> param = null;
            int prevSequence = 0;
            int methodOverload;
            try
            {
                if (_methodDic != null && _methodDic.Count > 0)
                {
                    IDictionary<string, List<OverloadInfo>> classDetails = _methodDic[className];
                    classDetails.TryGetValue(method, out overloadinfo);
                    if (overloadinfo != null && overloadinfo.Count > 0)
                    {
                        foreach (OverloadInfo overloadMethod in overloadinfo)
                        {
                            if (overloadMethod.MethodParameters.Count > prevSequence)
                            {
                                prevSequence = overloadMethod.MethodParameters.Count;
                                param = overloadMethod.MethodParameters;
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            return param;
        }

        List<Parameters> GetMatchAsssembyOVerload (string method,int overload,string className)
        {
            List<OverloadInfo> overloadinfo = new List<OverloadInfo>();
            List<Parameters> param = null;
            try
            {
                if (_methodDic != null && _methodDic.Count > 0)
                {
                    IDictionary<string, List<OverloadInfo>> classDetails = _methodDic[className];
                    classDetails.TryGetValue(method.Trim(), out overloadinfo);
                    if (overloadinfo != null && overloadinfo.Count > 0)
                    {
                        foreach (OverloadInfo overloadMethod in overloadinfo)
                        {
                            if (overloadMethod.OverLoad == overload)
                            {
                                param = overloadMethod.MethodParameters;
                                break;
                            }
                        }
                    }

                }
            
            }
            catch
            {
                return null;
            }
            return param;
        }


        List<Parameters> GetMethodsParameters (string method, int overload,string className = "Alachisoft.NCache.Web.Caching.Cache")
        {
            overload = 1;      
            try
            {
                if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null)
                {
                    if (Alachisoft.NCache.Management.APILogging.APILogManager.IsVersionMatched)
                    {
                        return (GetMatchAsssembyOVerload(method, overload,className));
                    }
                    else
                    {
                        return (GetMisMatchAsssembyOVerload(method,className));
                    }
                }
         
                
            }
            catch
            {
                return null;
            }
            return null;
        }

        string GetClientIP(string socketID)
        {
            try
            {
                if (socketID != null)
                {
                    string[] split = socketID.Split(':');
                    return split[0];
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        string GetClietnProcessID (string clientid)
        {
            try
            {
                if (clientid != null)
                {
                    string[] split = clientid.Split(':');
                    return split[split.Count() - 1];
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
        }

        void AddAPILogToManager(Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logitem)
        {
            try
            {
                if (Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger != null)
                {
                    lock (_lock)
                    {
                        Alachisoft.NCache.Management.APILogging.APILogManager.APILogManger.LogEntry(logitem);
                    }
                   
                }
            }
            catch
            {

            }
        }

        public Hashtable GetDependencyExpirationAndQueryInfo (ExpirationHint hint, Hashtable queryinfo)
        {
            Hashtable values = new Hashtable();
            if (hint == null)
            {
                values.Add("absolute-expiration", null);
                values.Add("sliding-expiration", null);
                values.Add("dependency", null);

            }
            else
            {
                if (hint is IdleExpiration)
                {
                    values.Add("sliding-expiration", ((IdleExpiration)hint).SlidingTime.Ticks);
                    values.Add("absolute-expiration", null);
                }
                else if (hint is FixedExpiration)
                {
                    values.Add("absolute-expiration", ((FixedExpiration)hint).AbsoluteTime.Ticks);
                    values.Add("sliding-expiration", null);
                }
                else
                {
                    long absExpiration=-1, sldExpiration = -1;
                    values.Add("dependency", GetDependency(hint,ref absExpiration,ref sldExpiration));
                    if (hint is AggregateExpirationHint)
                    {
                        values.Add("sliding-expiration", sldExpiration);
                        values.Add("absolute-expiration",absExpiration);
                    }
                }
            }
            if (queryinfo != null && queryinfo.ContainsKey("named-tag-info"))
                values.Add("named-tags", queryinfo["named-tag-info"]);
            else
                values.Add("named-tags", null);
            if (queryinfo != null &&  queryinfo.ContainsKey("tag-info"))
                
                values.Add("tag-info", queryinfo["tag-info"]);
            else
                values.Add("tag-info", null);

            return values;
        }
        
        public ArrayList GetDependency (ExpirationHint hint, ref long absExpiration, ref long sldExpiration)
        {
            if (hint == null)
                return null;
            ArrayList dependency = new ArrayList();
            try
            {
              
                if (hint is AggregateExpirationHint)
                {
                    AggregateExpirationHint internalHint = (AggregateExpirationHint)hint;
                    dependency.Add("Aggregate Dependency : ");
                    for (int i = 0; i < internalHint.Hints.Count; i++)
                    {
                        dependency.Add(AddHintToDependency(internalHint.Hints[i] ,ref absExpiration,ref sldExpiration));
                    }
                }
                else
                {
                   return AddHintToDependency(hint,ref absExpiration,ref sldExpiration);
                }
            }
            catch
            {
                return new ArrayList();  
            }
            return dependency;
        }

        public ArrayList AddHintToDependency(ExpirationHint hint, ref long absExpiration, ref long sldExpiration)
        {
           if (hint is FixedExpiration)
            {
                absExpiration=  ((FixedExpiration)hint).AbsoluteTime.Ticks;
  
            }
            else if (hint is IdleExpiration)
            {
                sldExpiration = ((IdleExpiration)hint).SlidingTime.Ticks;

            }
            return new ArrayList();
        }
        
        internal void GenerateTouchCommandAPILogItem(int keys, int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload);
            string parameterName = null;
            APIRuntimeLogItem logItem = new APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            foreach (Parameters param in parameter)
            {
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("keys", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("keys", keys);
                    }
                }
            }
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        internal void GeneratePingCommandAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP)
        {
            var logItem = new APIRuntimeLogItem(exceptionMesage);

            if (clientId != null) logItem.ClientID = GetClietnProcessID(clientId);
            if (clientIP != null) logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;            
            logItem.RuntimeParameters = new Hashtable();
            AddAPILogToManager(logItem);
        }

        internal void GeneratePollCommandAPILogItem(int overload, string exceptionMesage, TimeSpan executionTime, string clientId, string clientIP, int updateCount, int removeCount)
        {
            APIRuntimeLogItem logItem = new APIRuntimeLogItem(exceptionMesage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItemParameters.Add("result", updateCount + ":" + removeCount);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateGetCreateOrDeleteTopicAPILogItem(string topicName,TimeSpan executionTime, string clientId, string clientIP,int overload, bool result,string exceptionMessage)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload,APIClassNames.MESSAGING_SERVICE);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMessage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.Class = APIClassNames.MESSAGING_SERVICE;

            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("topicName", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("topicname", topicName);
                    }
                }
            }
            logItemParameters.Add("result", result);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateGetTopicMessagesAPILogItem(TimeSpan executionTime, string clientId, string clientIP, int overload, IDictionary<string, IList<object>> result, string exceptionMessage)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload, APIClassNames.MESSAGING_SERVICE);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMessage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.Class = APIClassNames.MESSAGING_SERVICE;
            StringBuilder details = new StringBuilder();

            if (result != null)
            {
                details.Append("[");

                foreach (var pair in result)
                {
                    if (pair.Key != null)
                        details.Append(pair.Key).Append(" : ");

                    int messageCount = pair.Value != null ? pair.Value.Count : 0;
                    details.Append(messageCount).Append(" , ");
                    
                }
                details.Append("]; ");
            }

            logItemParameters.Add("result", details.ToString());
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateAcknowledgeTopicMessagesAPILogItem(TimeSpan executionTime, string clientId, string clientIP, int overload, Dictionary<string, IList<string>> topicWiseMessages, string exceptionMessage)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload, APIClassNames.MESSAGING_SERVICE);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMessage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.Class = APIClassNames.MESSAGING_SERVICE;

            StringBuilder details = new StringBuilder();

            foreach (Parameters param in parameter)
            {
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("topicWiseMessageIds", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (topicWiseMessages != null)
                        {
                            details.Append("[");

                            foreach (var pair in topicWiseMessages)
                            {
                                if (pair.Key != null && pair.Value != null)
                                    details.Append(pair.Key).Append(" : " + pair.Value.Count).Append(" , ");
                            }
                            details.Append("]; ");
                        }

                        logItemParameters.Add("topicwisemessageids", details);
                    }
                }
            }

            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GenerateSubscribeToTopicAPILogItem(string topicName, SubscriptionType subscriptionType,  TimeSpan executionTime, string clientId, string clientIP, int overload, bool result, string exceptionMessage, string apiClassName)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload, apiClassName);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMessage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.Class = apiClassName;

            foreach (Parameters param in parameter)
            {
                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("topicName", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("topicname", topicName);
                    }

                    if (parameterName.Equals("subscriptonType", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("subscriptontype", subscriptionType.ToString());
                    }
                }
            }
            logItemParameters.Add("result", result);
            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }

        public void GeneratePublishTopicMessageAPILogItem(string topicName,string messageId,int messageSize,DeliveryOption deliveryOption,bool notifyOnFailure,long expiratoin, TimeSpan executionTime, string clientId, string clientIP, int overload, string exceptionMessage)
        {
            List<Parameters> parameter = GetMethodsParameters(_methodName, overload, APIClassNames.TOPIC);
            string parameterName = null;
            Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem logItem = new Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem(exceptionMessage);
            Hashtable logItemParameters = new Hashtable();

            if (clientId != null)
                logItem.ClientID = GetClietnProcessID(clientId);

            if (clientIP != null)
                logItem.ClientNode = GetClientIP(clientIP);

            logItem.ExecutionTime = executionTime;
            logItem.GeneratedTime = DateTime.Now;
            logItem.InstanceID = ServiceConfiguration.BindToIP.ToString();
            logItem.MethodOverload = overload;
            logItem.MethodName = _methodName;
            logItem.Class = APIClassNames.TOPIC;

            foreach (Parameters param in parameter)
            {

                if (param.ParameterName != null)
                {
                    parameterName = param.ParameterName.ToLower();
                    if (parameterName.Equals("topicName", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("topicname", topicName);
                    }
                    if (parameterName.Equals("message", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("message", "[" + messageId + " ; size = " + messageSize + "]");
                    }
                    if (parameterName.Equals("expiration", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string expiry = expiratoin == TimeSpan.MaxValue.Ticks ? "NO_EXPIRATION" : new TimeSpan(expiratoin).ToString();
                        logItemParameters.Add("expiration", expiry );
                    }
                    if (parameterName.Equals("deliverOption", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("deliveroption", deliveryOption.ToString());
                    }
                    if (parameterName.Equals("notifyDeliveryFailure", StringComparison.InvariantCultureIgnoreCase))
                    {
                        logItemParameters.Add("notifydeliveryfailure", notifyOnFailure);
                    }
                }
            }

            logItem.RuntimeParameters = logItemParameters;
            AddAPILogToManager(logItem);
        }
    }
}

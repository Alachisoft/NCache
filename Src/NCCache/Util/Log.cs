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
using Microsoft.Win32;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Caching.Util;
using System.IO;
using System.Reflection;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// Summary description for Log.
    /// </summary>


    internal class Log 
    {
        /// <summary>Configuration file folder name</summary>

        private const string DIRNAME = @"log-files";
        /// <summary>Path of the configuration folder.</summary>
        static private string s_configDir = "";
        static private string path = "";


        /// <summary>
        /// Scans the registry and locates the configuration file.
        /// </summary>
        static Log()
        {
            s_configDir = AppUtil.LogDir;

            try
            {
                s_configDir = System.IO.Path.Combine(s_configDir, DIRNAME);
            }
            catch { }

            if (s_configDir == null || s_configDir.Length == 0)
            {
                s_configDir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName ;
                s_configDir = System.IO.Path.Combine(s_configDir, DIRNAME);
            }
        }

        internal static string GetLogPath()
        {
            if (path.Length < 1)
            {
                path = s_configDir;
            }
            //string filename = context.CacheRoot.Name.ToLower() + "." + 
            //    Environment.MachineName.ToLower() + "." + 
            //    DateTime.Now.ToString("dd-MM-yy HH-mm-ss") + @".log.txt";
            try
            {
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                AppUtil.LogEvent("NCache", e.ToString(),System.Diagnostics.EventLogEntryType.Error,EventCategories.Error,EventID.GeneralError);
            }
            return path;
        }

        /// <summary>
        /// Start the cache logging functionality.
        /// </summary>
        internal static NewTrace Initialize(IDictionary properties, CacheRuntimeContext context, CacheInfo cacheInfo)
        {
            return Initialize(properties, context, cacheInfo, false,false);
        }
        
        /// <summary>
        /// Start the cache logging functionality.
        /// </summary>
        internal static NewTrace Initialize(IDictionary properties, CacheRuntimeContext context, CacheInfo cacheInfo, bool isStartedAsMirror,bool inproc)
        {

            NewTrace nTrace = new NewTrace();

            if (properties.Contains("enabled"))
            {
                bool enabled = Convert.ToBoolean(properties["enabled"]);

                if (!enabled)
                {
                    nTrace.IsFatalEnabled = nTrace.IsErrorEnabled = false;
                    nTrace.IsWarnEnabled = nTrace.isInfoEnabled = false;
                    nTrace.IsDebugEnabled = false;
                    return nTrace;
                }
            }

            try
            {
                string cache_name = cacheInfo.Name;
                if (cacheInfo.CurrentPartitionId != null && cacheInfo.CurrentPartitionId != string.Empty)
                    cache_name += "-" + cacheInfo.CurrentPartitionId;
                
                if (isStartedAsMirror)
                    cache_name += "-" + "replica";

                if (inproc && !isStartedAsMirror) cache_name += "." + System.Diagnostics.Process.GetCurrentProcess().Id;
                nTrace.SetOutputStream(cache_name, Log.GetLogPath());

            }
            catch (Exception e)
            {

                AppUtil.LogEvent("NCache", "Failed to open log. " + e, System.Diagnostics.EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);

            }

            if (properties.Contains("usehptime"))
                nTrace.UseHPTime = Convert.ToBoolean(properties["usehptime"]);

            if (properties.Contains("trace-errors"))
            {
                nTrace.IsErrorEnabled = Convert.ToBoolean(properties["trace-errors"]);
                nTrace.IsCriticalInfoEnabled = Convert.ToBoolean(properties["trace-errors"]);
            }   

            if (properties.Contains("trace-warnings"))
                nTrace.IsWarnEnabled = Convert.ToBoolean(properties["trace-warnings"]);

            if (properties.Contains("trace-debug"))
                nTrace.IsInfoEnabled = Convert.ToBoolean(properties["trace-debug"]);

            nTrace.IsFatalEnabled = nTrace.IsErrorEnabled;
            nTrace.IsDebugEnabled = nTrace.IsWarnEnabled = nTrace.IsInfoEnabled;


            return nTrace;
        }

    }
}

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Integrations.EntityFramework.Util;
using System.Web;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    public sealed class AnalysisPolicyElement : ICloneable
    {
        public static string DefaultLoggingPath = string.Empty;

        CachePolicy.Expirations _defaultExp;

        /// <summary>
        /// Static constructor
        /// </summary>
        static AnalysisPolicyElement()
        {
            try
            {
                string installPath = Path.Combine(GetDirectoryPath("log-files"), "log-files");
                CreateIfNotExists(installPath);
                installPath = Path.Combine(installPath, "EFCachingAnalysisLogs");
                CreateIfNotExists(installPath);
                DefaultLoggingPath = installPath;
            }
            catch (Exception exc)
            {
                Logger.Instance.TraceError(exc.Message);
            }
        }


        private static string GetDirectoryPath (string dirName)
        {
            string path = null;
            bool found = true;
            path = AppUtil.LogDir;
            if (!Directory.Exists(Path.Combine (path, "log-files")))
            {
                found = false;
            }
            if (!found)
                if (HttpContext.Current != null)
                {
                    string approot = HttpContext.Current.Server.MapPath(@"~\");
                    if (approot != null)
                    {
                        path = approot;
                        if (!Directory.Exists(Path.Combine(path, dirName)))
                            found = false;
                    }
                }
            if (!found)
            {
                string roleRootDir = Environment.GetEnvironmentVariable("RoleRoot");
                if (roleRootDir != null)
                {
                    path = roleRootDir + "\\approot\\";
                    if (!Directory.Exists(Path.Combine(path, dirName)))
                    {
                        found = false;
                    }
                    
                }

            }
            if (!found)
            {
                path = Environment.CurrentDirectory;
            }
            return path;


        }
        /// <summary>
        /// Create a directory if it does not exists
        /// </summary>
        /// <param name="installPath">Path of the directory to check</param>
        private static void CreateIfNotExists(string installPath)
        {
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AnalysisPolicyElement()
        {
        }

        /// <summary>
        /// Get the default logging path
        /// </summary>
        public string DefaultLogPath { get { return DefaultLoggingPath; } }

        /// <summary>
        /// Get or set the path at which analysis report will be generated
        /// </summary>
        [ConfigurationAttribute("log-path")]
        public string LogPath { get; set; }

        /// <summary>
        /// Get or set the time (in min) for which analsis will run
        /// </summary>
        [ConfigurationAttribute("analysis-time", "min")]
        public int AnalysisTime { get; set; }

        /// <summary>
        /// Get or set the minimum number of call-count after which caching will be enabled for a query
        /// </summary>
        [ConfigurationAttribute("cache-enable-threshold")]
        public int CacheEnableThreshold { get; set; }

        /// <summary>
        /// Get or set the default expiration type
        /// </summary>
        public CachePolicy.Expirations DefaultExpirationType
        {
            get { return this._defaultExp; }
            set { this._defaultExp = value; }
        }

        /// <summary>
        /// Get or set default expiration type's string value
        /// </summary>
        [ConfigurationAttribute("default-expiration-type")]
        public string DefualtTypeString
        {
            get
            {
                return this._defaultExp.ToString();
            }
            set
            {
                this._defaultExp = CachePolicy.Expirations.Sliding;
                if (value != null)
                {
                    switch (value.ToLower())
                    {
                        case "absolute":
                        case "":
                            this._defaultExp = CachePolicy.Expirations.Absolute;
                            break;
                        case "sliding":
                            this._defaultExp = CachePolicy.Expirations.Sliding;
                            break;
                        default:
                            throw new ConfigurationErrorsException("Unknown \"expiration-type\" specified.");
                    }
                }
            }
        }

        /// <summary>
        /// Get or set default expiration time
        /// </summary>
        [ConfigurationAttribute("default-expiration-time", "sec")]
        public int DefualtExpirationTime { get; set; }

        /// <summary>
        /// Get or set whether database synchronization is enabled or not
        /// </summary>
        [ConfigurationAttribute("dbsyncdependency")]
        public bool DbSyncDependency { get; set; }

        #region ICloneable Members

        public object Clone()
        {
            return new AnalysisPolicyElement()
            {
                AnalysisTime = this.AnalysisTime,
                CacheEnableThreshold = this.CacheEnableThreshold,
                DbSyncDependency = this.DbSyncDependency,
                DefaultExpirationType = this.DefaultExpirationType,
                DefualtExpirationTime = this.DefualtExpirationTime,
                LogPath = this.LogPath
            };
        }

        #endregion
    }
}

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

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Util;
using Alachisoft.NCache.Web;
using System.Web;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    /// <summary>
    /// Event argument data for ConfigurationUpdated event
    /// </summary>
    internal class ConfiguraitonUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Get or set the updated application configuration
        /// </summary>
        public ApplicationConfigurationElement Configuration { get; set; }
    }

    /// <summary>
    /// Responsible to register configuration element and loading/saving configuration
    /// </summary>
    internal class EFCachingConfiguration : IDisposable
    {
        /// <summary>Configuration file name</summary>
        public const string ConfigFileName = "efcaching.ncconf";

        /// <summary>
        /// Occurs when configuration file is updated
        /// </summary>
        public event EventHandler<ConfiguraitonUpdatedEventArgs> ConfigurationUpdated = delegate { };

        private  string DirName = "config";
        private FileSystemWatcher _watcher;

        /// <summary>
        /// Current instance of EFCachingConfiguration
        /// </summary>
        public static EFCachingConfiguration Instance;

        /// <summary>
        /// Static constructor to register root element
        /// </summary>
        static EFCachingConfiguration()
        {
            Instance = new EFCachingConfiguration();
            if (Instance.FindConfig())
            {
                Instance.InitializeWatcher();

                ScanConfiguration();
            }
            //ConfigurationBuilder.RegisterRootConfigurationObject(typeof(EFCachingConfigurationElement));
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        private EFCachingConfiguration()
        {            
        }

        /// <summary>
        /// Determing config path. 
        /// </summary>
        private bool FindConfig()
        {
            string path = GetBaseFilePath(ConfigFileName);
            if (String.IsNullOrEmpty(path))
            {
                return false;
            }
            this.DirPath = path;
            this.FilePath = Path.Combine(this.DirPath, ConfigFileName);
            return true;
        }

        private string GetBaseFilePath(string fileName)
        {
            Search result;
            return SearchLocal(fileName, out result);
        }



        private string SearchLocal(string fileName, out Search result)
        {
            result = Search.LocalSearch;
            String path = null;

            path = Environment.CurrentDirectory;
            if (File.Exists(Path.Combine(path, fileName)))
                return path;
            return SearchLocalConfig(fileName, out result);
        }

        private string SearchLocalConfig(string fileName, out Search result)
        {
            result = Search.LocalConfigSearch;
            String path = null;
            bool found = false;
            if (HttpContext.Current != null)
            {
                string approot = HttpContext.Current.Server.MapPath(@"~\");
                if (approot != null)
                {
                    path = approot;
                    if (!File.Exists(Path.Combine(path, fileName)))
                    {
                        path = Path.Combine(approot + @"\", @"bin\config\");
                        if (!File.Exists(Path.Combine(path, fileName)))
                        {
                            //return null;
                            string configDir = System.Configuration.ConfigurationSettings.AppSettings.Get("NCache.ConfigDir");
                            if (configDir != null)
                            {
                                path = Path.Combine(configDir + @"/");
                                string tempPath = Path.Combine(configDir + @"/", fileName);
                                path = HttpContext.Current.Server.MapPath(@tempPath);
                                if (File.Exists(tempPath))
                                {
                                    //return null;
                                    found = true;
                                }
                            }
                        }
                        else
                            found = true;
                    }
                    else
                        found = true;

                }
            }
            if (!found)
            {
                string roleRootDir = Environment.GetEnvironmentVariable("RoleRoot");
                // string appRootDir = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
                if (roleRootDir != null)
                {
                    path = roleRootDir + "\\approot\\";
                    if (!File.Exists(Path.Combine(path, fileName)))
                    {
                        path = roleRootDir + "\\approot\\bin\\config\\";
                        if (File.Exists(Path.Combine(path, fileName)))
                        {
                            return path;
                        }
                    }
                    else
                        return path;
                }

            }
            else
                return path;
            return SearchGlobal(fileName, out result);
        }

        private static string SearchGlobal(string fileName, out Search result)
        {
            result = Search.GlobalSearch;
            string directoryPath = string.Empty;
            string filePath = string.Empty;
            if (SearchGlobalDirectory("config", false, out directoryPath))
            {
                filePath = Path.Combine(directoryPath, fileName);
                if (File.Exists(filePath))
                    return directoryPath;
            }
            return null;
        }


        private static bool SearchGlobalDirectory(string directoryName, bool createNew, out string path)
        {
            string ncacheInstallDirectory = AppUtil.InstallDir;
            path = string.Empty;

            if (ncacheInstallDirectory == null)
                return false;

            path = Path.Combine(ncacheInstallDirectory, directoryName);
            if (!Directory.Exists(path))
            {
                if (createNew)
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        return true;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                return false;
            }
            return true;
        }



        /// <summary>
        /// Initialize the file watcher and register for file change events.
        /// </summary>
        private void InitializeWatcher()
        {
            this._watcher = new FileSystemWatcher(this.DirPath, ConfigFileName);
            this._watcher.NotifyFilter = NotifyFilters.LastWrite;
            this._watcher.EnableRaisingEvents = true;
            this._watcher.Changed += new FileSystemEventHandler(this._watcher_Changed);
        }

        /// <summary>
        /// Called when configuration file changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            EFCachingConfigurationElement newConfig = this.LoadConfiguration();
            if (newConfig != null)
            {
                ConfigurationUpdated(null, new ConfiguraitonUpdatedEventArgs()
                {
                    Configuration = newConfig.GetAppConfig(Application.Instance.ApplicationId)
                });
            }
        }

        /// <summary>
        /// Get configuration file path
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Get directory path where configuration file resides
        /// </summary>
        public string DirPath { get; private set; }

        /// <summary>
        /// Scan for configuration file
        /// </summary>
        private static void ScanConfiguration()
        {
            try
            {   
                if (!File.Exists(Instance.FilePath))
                {
                    ///Save a dummy config
                    Instance.SaveConfiguration(null);
                }
            }
            catch (Exception exc)
            {
                Logger.Instance.TraceError(exc.Message);
                Instance.FilePath = string.Empty;
            }
        }

        /// <summary>
        /// Save configuration in config file
        /// </summary>
        /// <param name="config">Configuration to save</param>
        public void SaveConfiguration(EFCachingConfigurationElement configuration)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                if (configuration == null)
                {
                    sb.Append("<configuration></configuration>");
                }
                else
                {
                    ConfigurationBuilder cb = new ConfigurationBuilder(new object[] { configuration });
                    cb.RegisterRootConfigurationObject(typeof(EFCachingConfigurationElement));
                    sb.Append(cb.GetXmlString());
                }

                using (StreamWriter stream = new StreamWriter(this.FilePath, false))
                {
                    stream.Write(sb.ToString());

                    stream.Flush();
                    stream.Close();
                }
            }
            catch (Exception exc)
            {
                Logger.Instance.TraceError(exc.Message);
            }
        }

        /// <summary>
        /// Loads the configuration
        /// </summary>
        /// <returns>Root element instance</returns>
        public EFCachingConfigurationElement LoadConfiguration()
        {
            EFCachingConfigurationElement config = null;

            if (!File.Exists(this.FilePath))
            {
                Logger.Instance.TraceError(string.Format("Configuration file '{0}' not found", ConfigFileName));
                return config;
            }

            try
            {
                ConfigurationBuilder configBuilder = new ConfigurationBuilder(string.Empty, this.FilePath);
                configBuilder.RegisterRootConfigurationObject(typeof(EFCachingConfigurationElement));
                configBuilder.ReadConfiguration();

                object[] conf = configBuilder.Configuration;
                if (conf != null && conf.Length > 0)
                {
                    config = conf[0] as EFCachingConfigurationElement;
                }
                if (config == null)
                {
                    Logger.Instance.TraceError("No 'configuration' element found in configuration file");
                }
            }
            catch (Exception exc)
            {
                Logger.Instance.TraceError(exc.ToString());
            }

            return config;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (this._watcher != null)
            {
                this._watcher.Dispose();
            }
        }

        #endregion
    }
}

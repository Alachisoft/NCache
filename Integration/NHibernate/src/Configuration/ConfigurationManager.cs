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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration;
using System.IO;
using System.Web;

using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Client;

namespace Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration
{
    class ConfigurationManager
    {
        private static ConfigurationManager _singleton;
        private static object _sigletonLock = new object();

        private ApplicationConfiguration _appConfig = null;
        private RegionConfigurationManager _regionConfigManager = null;

        private ConfigurationManager()
        {


            string appID = System.Configuration.ConfigurationManager.AppSettings["ncache.application_id"];
            if(string.IsNullOrEmpty(appID))
                throw new ConfigurationException("ncache.application-id not specified in app.config/web.config file");

            string configFilePath = this.GetFilePath("NCacheNHibernate.xml");



            ConfigurationBuilder configBuilder = new ConfigurationBuilder(configFilePath);
            configBuilder.RegisterRootConfigurationObject(typeof(ApplicationConfiguration));
            configBuilder.ReadConfiguration();

            Object[] configuration = configBuilder.Configuration;

            bool appConfigFound = false;
            if (configuration != null && configuration.Length > 0)
            {
                for (int i = 0; i < configuration.Length; i++)
                {
                    _appConfig = configuration[i] as ApplicationConfiguration;
                    if(_appConfig!=null)
                    if (!string.IsNullOrEmpty(_appConfig.ApplicationID) && _appConfig.ApplicationID.ToLower() == appID.ToLower())
                    {
                        appConfigFound = true;
                        break;
                    }
                }
            }
            
            if (!appConfigFound)
                throw new ConfigurationException("Invalid value of NCache.application_id. Applicaion configuration not found for application-id = " + appID);
            if (string.IsNullOrEmpty(_appConfig.DefaultRegion))
                throw new Alachisoft.NCache.Runtime.Exceptions.ConfigurationException("default-region cannot be null for application-id = " + _appConfig.ApplicationID);
            
            _regionConfigManager = new RegionConfigurationManager(_appConfig.CacheRegions);
            if (!_regionConfigManager.Contains(_appConfig.DefaultRegion))

                throw new Alachisoft.NCache.Runtime.Exceptions.ConfigurationException("Region's configuration not specified for default-region : "+_appConfig.DefaultRegion);

        }

        private string GetFilePath(string fileName)
        { 
            string filePath = "";
            if (File.Exists(filePath + fileName))
                filePath = fileName;
            else if (File.Exists(@".\bin\" + fileName))
                filePath = @".\bin\" + fileName;
            else if (File.Exists(System.Environment.GetEnvironmentVariable("NCHome") + @"\config\" + fileName))
                            filePath = System.Environment.GetEnvironmentVariable("NCHome") + @"\config\" + fileName;
            else
            {
                filePath = GetBaseFilePath(fileName);
                if (!string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.Combine(filePath, fileName);
                    return filePath;
                }
                throw new FileNotFoundException(fileName + " file not found." + filePath);

                string envVariableError = "";

                if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("NCHome")))
                    envVariableError = " Envronment variable NCHome not set to a valid directory path.";
                throw new FileNotFoundException(fileName + " file not found." + envVariableError);

            }
            return filePath;
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

        public static ConfigurationManager Instance
        {
            get
            {
                lock (_sigletonLock)
                {
                    if (_singleton == null)
                        _singleton = new ConfigurationManager();
                    return _singleton;
                }
            }
        }

        public RegionConfiguration GetRegionConfiguration(string regionName)
        {
            RegionConfiguration rConfig = _regionConfigManager.GetRegionConfig(regionName);            
            if (rConfig == null)
                rConfig = _regionConfigManager.GetRegionConfig(_appConfig.DefaultRegion);
            return rConfig;
        }

        public string GetCacheKey(object key)
        {
            string cacheKey = null;
            cacheKey = "NHibernateNCache:" + key.ToString();

            if (_appConfig.KeyCaseSensitivity == false)
                cacheKey = cacheKey.ToLower();
            return cacheKey;
        }

        public bool ExceptionEnabled
        {
            get { return _appConfig.CacheExceptionEnabled; }
        }
    }
}

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
using System.IO;
using System.Web;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Runtime.Exceptions;

#if NETCORE
using System.Net.Http;
#endif

namespace Alachisoft.NCache.Client
{
    internal class DirectoryUtil
    {
        /// <summary>
        /// search for the specified file in the executing assembly's working folder
        /// if the file is found, then a path string is returned back. otherwise it returns null.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileLocalPath(string fileName)
        {
            string path = Environment.CurrentDirectory + fileName;
            if (File.Exists(path))
                return path;
            return null;
        }

        /// <summary>
        /// search for the specified file in NCache install directory. if the file is found
        /// then returns the path string from where the file can be loaded. otherwise it returns 
        /// null.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFileGlobalPath(string fileName, string directoryName)
        {
            string directoryPath = string.Empty;
            string filePath = string.Empty;
            if (!SearchGlobalDirectory(directoryName, false, out directoryPath)) return null;
            filePath = Path.Combine(directoryPath, fileName);

            if (!File.Exists(filePath))
                return null;
            return filePath;

        }

        public static ArrayList GetCacheConfig(string cacheId, string userId, string password, bool inproc)
        {
            string filePath = GetBaseFilePath("config.ncconf");
            ArrayList configurationList = null;

            if (filePath != null)
            {
                try
                {
                    configurationList = ThinClientConfigManager.GetCacheConfig(cacheId, filePath, userId, password, inproc);
                }
                catch (ManagementException exception)
                {
                }
            }

            return configurationList;
        }

        public static CacheServerConfig GetCacheDom(string cacheId, string userId, string password, bool inproc)
        {
            string filePath = GetBaseFilePath("config.ncconf");
            CacheServerConfig dom = null;

            if (filePath != null)
            {
                try
                {
                    dom = ThinClientConfigManager.GetConfigDom(cacheId, filePath, userId, password, inproc);
                }
                catch (ManagementException exception)
                {
                }
            }

            return dom;
        }

        public static bool SearchLocalDirectory(string directoryName, bool createNew, out string path)
        {
            path = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;
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

        public static bool SearchGlobalDirectory(string directoryName, bool createNew, out string path)
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

        public static string GetBaseFilePath(string fileName)
        {
            Search result;
            return SearchLocal(fileName, out result);
        }

        public static string GetBaseFilePath(string fileName, Search search, out Search result)
        {
            if (search == Search.LocalSearch)
            {
                return SearchLocal(fileName, out result);
            }
            else
                if (search == Search.LocalConfigSearch)
            {
                return SearchLocalConfig(fileName, out result);
            }
            else
            {
                return SearchGlobal(fileName, out result);
            }

        }

        private static string SearchLocal(string fileName, out Search result)
        {
            result = Search.LocalSearch;
            String path = null;

            path = Environment.CurrentDirectory + Path.DirectorySeparatorChar + fileName;
            if (File.Exists(path))
                return path;
            return SearchLocalConfig(fileName, out result);
        }

        private static string SearchLocalConfig(string fileName, out Search result)
        {
            result = Search.LocalConfigSearch;
            String path = null;
            bool found = false;
#if !NETCORE
            //TODO: ALACHISOFT (HttpContext is missing in .Net Core)
            if (HttpRuntime.AppDomainAppId != null || HttpContext.Current != null)
            {
                string approot = null;

                if (HttpContext.Current == null)
                    approot = HttpRuntime.AppDomainAppPath;
                else
                    approot = HttpContext.Current.Server.MapPath(@"~\");

                if (approot != null)
                {
                    path = approot + fileName;
                    if (!File.Exists(path))
                    {
                        path = Path.Combine(approot + @"\", @"bin\config\" + fileName);
                        if (!File.Exists(path))
                        {
                            path = Path.Combine(approot + @"\", @"bin\" + fileName);
                            if (!File.Exists(path))
                            {
                                string configDir =
                                System.Configuration.ConfigurationSettings.AppSettings.Get("NCache.ConfigDir");
                                if (configDir != null)
                                {
                                    path = Path.Combine(configDir + @"/", fileName);
                                    path = HttpContext.Current.Server.MapPath(@path);
                                    if (File.Exists(path))
                                    {
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
                    else
                        found = true;
                }
            }
#endif
            if (!found)
            {
                string roleRootDir = Environment.GetEnvironmentVariable("RoleRoot");
                if (roleRootDir != null)
                {
                    path = roleRootDir + "\\approot\\" + fileName;
                    if (!File.Exists(path))
                    {
                        path = roleRootDir + "\\approot\\bin\\config\\" + fileName;
                        if (File.Exists(path))
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
                    return filePath;
            }
            return null;
        }
    }
}
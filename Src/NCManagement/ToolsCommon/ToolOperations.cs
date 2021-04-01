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
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management.ServiceControl;
using System.Collections;
using System.IO;


namespace Alachisoft.NCache.Tools.Common
{
    public class ToolOperations
    {
        static private NCacheRPCService NCache = new NCacheRPCService("");
        static ICacheServer cacheServer;

        public CacheServerConfig GetServerConfiguration(string server, int port, string cacheId)
        {
            CacheServerConfig serverConfig=null ;
            if (port != -1)
            {
                NCache.Port = port;
            }
            if (port==-1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort: CacheConfigManager.HttpPort;
            if (server != null && server != string.Empty)
            {
                NCache.ServerName = server;
            }
            cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
            if (cacheServer != null)
            {
                serverConfig = cacheServer.GetCacheConfiguration(cacheId);
                if (serverConfig == null)
                    throw new Exception("Specified cache is not registered on given server.");
            }
            return serverConfig;
        }

        public void RegisterCache(Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig, int port, string cacheId, string userId, string password, bool isHotApply, bool isOverwrite, string server)
        {
            if (port != -1)
            {
                NCache.Port = port;
            }

            if (port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
            if (server != null && server != string.Empty)
            {
                NCache.ServerName = server;
            }

            cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

            byte[] _userId = null;
            byte[] _paswd = null;
            if (cacheServer != null)
            {
                if (userId != string.Empty && password != string.Empty)
                {
                    _userId = EncryptionUtil.Encrypt(userId);
                    _paswd = EncryptionUtil.Encrypt(password);
                }

                if (serverConfig.CacheSettings.CacheType == "clustered-cache")
                {
                    foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                    {
                        NCache.ServerName = node.IpAddress.ToString();
                        try
                        {
                            cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                            cacheServer.RegisterCache(cacheId, serverConfig, "", isOverwrite, isHotApply);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);
                        }
                        finally
                        {
                            cacheServer.Dispose();
                        }
                    }
                }
                else
                {
                    try
                    {
                        cacheServer.RegisterCache(cacheId, serverConfig, "", isOverwrite, isHotApply);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);
                    }
                    finally
                    {
                        cacheServer.Dispose();
                    }
                }
            }
        }

        public bool IsRunningCaches(string server, int port,string userId, string password)
        {
            ArrayList runningCaches=new ArrayList();
            bool isRunning=false;

            if (port != -1)
            {
                NCache.Port = port;
            }

            if (port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
            if (server != null && server != string.Empty)
            {
                NCache.ServerName = server;
            }

            cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

            if (cacheServer != null)
            {
                runningCaches = cacheServer.GetRunningCaches();
            }
            if (runningCaches == null || runningCaches.Count == 0)
                isRunning = false;
            else
                isRunning = true;

            return isRunning;
            
        }

        public bool IsRunningCache(string server, int port, string cacheId, string userId, string password)
        {
            ArrayList runningCaches = new ArrayList();
            bool isRunning = false;

            if (port != -1)
            {
                NCache.Port = port;
            }

            if (port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
            if (server != null && server != string.Empty)
            {
                NCache.ServerName = server;
            }

            cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

            if (cacheServer != null)
            {
                isRunning = cacheServer.IsRunning(cacheId);
            }

            return isRunning;
        }


        public static bool DeployAssembly(string server, int port, string path, string cacheId, string depAsmPath, string userId,string password,LogErrors logError )
        {
            List<FileInfo> files = new List<FileInfo>();  // List that will hold the files and subfiles in path
            string fileName=null;
            byte[] asmData;
            string failedNodes = string.Empty;
            Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = null;

            try
            {
                if (port != -1)
                {
                    NCache.Port = port;
                }

                if (port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                if (server != null || server != string.Empty)
                {
                    NCache.ServerName = server;
                }

                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                if (cacheServer != null)
                {
                    serverConfig = cacheServer.GetNewConfiguration(cacheId);
              
                    if (path != null && path != string.Empty)
                    {
                        if (Path.HasExtension(path))
                        {
                            FileInfo fi = new FileInfo(path);
                            files.Add(fi);
                        }
                        else
                        {
                            DirectoryInfo di = new DirectoryInfo(path);

                            try
                            {
                                foreach (FileInfo f in di.GetFiles("*"))
                                {
                                    if (Path.HasExtension(f.FullName))
                                        files.Add(f);
                                }
                            }
                            catch (Exception ex)
                            {
                                logError("Directory " + di.FullName + "could not be accessed.");
                                return false;  // We already got an error trying to access dir so dont try to access it again
                            }

                        }
                    }

                    if (depAsmPath != null && depAsmPath != string.Empty)
                    {
                        if (Path.HasExtension(depAsmPath))
                        {
                            FileInfo fi = new FileInfo(path);
                            files.Add(fi);
                        }
                        else
                        {
                            DirectoryInfo di = new DirectoryInfo(depAsmPath);

                            try
                            {
                                foreach (FileInfo f in di.GetFiles("*"))
                                {
                                    if (Path.HasExtension(f.FullName))
                                        files.Add(f);
                                }
                            }
                            catch (Exception ex)
                            {
                                logError("Directory " + di.FullName + "could not be accessed.");
                                return false;  // We already got an error trying to access dir so dont try to access it again
                            }

                        }
                    }

                    foreach (FileInfo f in files)
                    {
                        try
                        {

                            FileStream fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read);
                            asmData = new byte[fs.Length];
                            fs.Read(asmData, 0, asmData.Length);
                            fs.Flush();
                            fs.Close();
                            fileName = Path.GetFileName(f.FullName);
                            byte[] _userId = null;
                            byte[] _paswd = null;
                            if (userId != string.Empty && password != string.Empty)
                            {
                                _userId = EncryptionUtil.Encrypt(userId);
                                _paswd = EncryptionUtil.Encrypt(password);
                            }
                            if (serverConfig.CacheSettings.CacheType == "clustered-cache")
                            {
                                foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                                {
                                    NCache.ServerName = node.IpAddress.ToString();
                                    try
                                    {
                                        cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                                        cacheServer.CopyAssemblies(cacheId, fileName, asmData);
                                    }
                                    catch (Exception ex)
                                    {
                                        logError("Failed to Deploy Assembly on "+NCache.ServerName);
                                        logError("Error Detail: "+ex.Message);
                                    }
                                }
                            }
                            else
                                cacheServer.CopyAssemblies(cacheId,fileName,asmData);
                        }
                        catch (Exception e)
                        {
                            string message = string.Format("Could not deploy assembly \"" + fileName + "\". {0}", e.Message);
                            logError("Error : " + message);
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logError("Error : {0}" + e.Message);
            }
            finally
            {
                NCache.Dispose();
            }

            return true;
        }
    }

}

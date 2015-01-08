// Copyright (c) 2015 Alachisoft
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
using System.Data;
using System.Collections;
using System.Globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Collections.Generic;

using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;


using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Enum;
using System.Net;
using Alachisoft.NCache.Config.Dom;
using System.Diagnostics;
using Alachisoft.NCache.Common.Net;
using System.Threading;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Tools.Common;
using System.Text;
using System.IO;
using System.IO.Compression;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Management.ServiceControl;


namespace Alachisoft.NCache.Tools.RemoveQueryIndex
{
    class Application
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                RemoveQueryIndexTool.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    /// <summary>
    /// Summary description for ConfigureQueryIndexTool.
    /// </summary>
    ///

    public class RemoveQueryIndexParam :  Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string _asmPath = string.Empty;
        private string _class = string.Empty;
        private string _attributes = string.Empty;
        private string _cacheId = string.Empty;
        private string _server = string.Empty;
        private int _port = -1;
        
        public RemoveQueryIndexParam()
        {
        }

        [ArgumentAttribute("", "")]
        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [ArgumentAttribute(@"/c", @"/class")]
        public string Class
        {
            get { return _class; }
            set { _class = value; }
        }

        [ArgumentAttribute(@"/L", @"/attrib-list")]
        public string Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        [ArgumentAttribute(@"/s", @"/server")]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        [ArgumentAttribute(@"/p", @"/port")]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        } 
    }
    sealed class RemoveQueryIndexTool
    {

        static private RemoveQueryIndexParam cParam = new RemoveQueryIndexParam();
        static private NCacheRPCService NCache = new NCacheRPCService("");
        static private ICacheServer cacheServer;


        /// <summary>
        /// Validate all parameters in property string.
        /// </summary>
        private static bool ValidateParameters()
        {
            // Validating CacheId
            if (string.IsNullOrEmpty(cParam.CacheId))
            {
                Console.Error.WriteLine("Error: Cache name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(cParam.Class))
            {
                Console.Error.WriteLine("Error: Class name not specified.");
                return false;
            }
           
            AssemblyUsage.PrintLogo(cParam.IsLogo);
            return true;
        }

        ////<summary>
        ////Log an event in event viewer.
        ////</summary>
        private static void LogEvent(string msg)
        {
            EventLogEntryType type = EventLogEntryType.Error;
            using (EventLog ncLog = new EventLog("Application"))
            {
                ncLog.Source = "NCache:RemoveQueryIndex Tool";
                ncLog.WriteEntry(msg, type);
            }
        }

        /// <summary>
        /// The main entry point for the tool.
        /// </summary>ju
        static public void Run(string[] args)
        {
            System.Reflection.Assembly asm = null;
            Alachisoft.NCache.Config.Dom.Class[] queryClasses = null;
            string failedNodes = string.Empty;
            bool sucessful = false;
            string serverName = string.Empty;

            try
            {
                object param = new RemoveQueryIndexParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (RemoveQueryIndexParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }
                if (!ValidateParameters()) return;
                if (cParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                if (cParam.Server != null && cParam.Server != string.Empty)
                {
                    NCache.ServerName = cParam.Server;
                }
                if (cParam.Port != -1)
                {
                    NCache.Port = cParam.Port;
                }

                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                

                if (cacheServer != null)
                {
                    Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = cacheServer.GetNewConfiguration(cParam.CacheId);
                    serverName = cacheServer.GetClusterIP();
                    if (cacheServer.IsRunning(cParam.CacheId))
                        throw new Exception(cParam.CacheId + " is Running on " + serverName + "Stop the cache first.");

                    

                    if (serverConfig == null)
                        throw new Exception("Specified cache is not registered on given server.");

                    
                    Console.WriteLine("Removing query indexes on node '{0}' from cache '{1}'.",
                                    serverName, cParam.CacheId);
                    if (serverConfig.CacheSettings.QueryIndices != null)
                    {

                        if (serverConfig.CacheSettings.QueryIndices.Classes != null)
                        {
                            queryClasses = serverConfig.CacheSettings.QueryIndices.Classes;
                        }
                        else
                            return;

                        if (queryClasses != null)
                        {
                            serverConfig.CacheSettings.QueryIndices.Classes = GetSourceClass(GetClass(queryClasses));
                            if (serverConfig.CacheSettings.QueryIndices.Classes != null)
                            {
                                for (int i = 0; i < serverConfig.CacheSettings.QueryIndices.Classes.Length; i++)
                                {
                                    if (serverConfig.CacheSettings.QueryIndices.Classes[i].AttributesTable.Count < 1)
                                        serverConfig.CacheSettings.QueryIndices.Classes[i] = null;
                                }
                                bool NoClasses = true;
                                foreach (Class cls in serverConfig.CacheSettings.QueryIndices.Classes)
                                {
                                    if (cls != null)
                                    {
                                        NoClasses = false;
                                        break;
                                    }
                                }
                                if (NoClasses)
                                    serverConfig.CacheSettings.QueryIndices = null;
                            }
                            else
                            {

                            }

                        }
                    }
                    else
                    {
                        throw new Exception("No such Query Index class found. ");
                        return;
                    }
                    if (serverConfig.CacheSettings.CacheType == "clustered-cache")
                    {
                        foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                        {
                            NCache.ServerName = node.IpAddress.ToString();
                            try
                            {
                                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                                if (cacheServer.IsRunning(cParam.CacheId))
                                    throw new Exception(cParam.CacheId + " is Running on " + serverName + "Stop the cache first.");
                                cacheServer.RegisterCache(cParam.CacheId, serverConfig, "", true, cParam.IsHotApply);
                            }
                            catch (Exception ex)
                            {
                              
                                Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);
                                failedNodes = failedNodes + "/n" + node.IpAddress.ToString();
                                LogEvent(ex.Message);
                                sucessful = false;
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
                            cacheServer.RegisterCache(cParam.CacheId, serverConfig, "", true, cParam.IsHotApply);
                        }
                        catch (Exception ex)
                        {
                          
                            Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);
                            LogEvent(ex.Message);
                            sucessful = false;
                        }
                        finally
                        {
                            cacheServer.Dispose();
                        }
                    }
                }
                sucessful = true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to Remove Query Index on node '{0}'. ", serverName);
                Console.Error.WriteLine("Error : {0}", e.Message);
                LogEvent(e.Message);
                sucessful = false;
            }
            finally
            {
         
                NCache.Dispose();
                if (sucessful && !cParam.IsUsage)
                {
                    Console.WriteLine("Query indexes successfully removed."); 
                }
            }
        }

        static public Hashtable GetAttributes(Hashtable attrib)
        {
            string[] str = cParam.Attributes.Split(new char[] { '$' });
            
            if (attrib.Count != 0 && attrib != null)
            {
                foreach (string st in str)
                {
                    if (attrib.Contains(st))
                        attrib.Remove(st);
                }
            }
            return attrib;
        }

        static public Attrib[] GetClassAttributes(Hashtable attrib)
        {
            Attrib[] a = new Attrib[attrib.Count];
            IDictionaryEnumerator enu = attrib.GetEnumerator();
            int index = 0;
            Attrib attribValue=new Attrib();

            while (enu.MoveNext())
            {
                a[index] = new Attrib();
                attribValue=(Attrib)enu.Value;
                a[index].Name = (string)attribValue.Name;
                a[index].ID = (string)attribValue.ID;
                a[index].Type = (string)attribValue.Type;
                index++;
            }
            return a;
        }
        static public Hashtable GetClass(Alachisoft.NCache.Config.Dom.Class[] cl)
        {
            Hashtable hash = new Hashtable();
            Hashtable att = new Hashtable();
            Alachisoft.NCache.Config.Dom.Class c = new Alachisoft.NCache.Config.Dom.Class();
            
            if (cl != null)
            {
                hash = ClassToHashtable(cl);

            }

            Class existingClass = null;

            if (cParam.Attributes == null || cParam.Attributes == string.Empty)
            {
                if (hash.Contains(cParam.Class))
                {
                    hash.Remove(cParam.Class);
                }
                else
                {
                    throw new Exception("No query index found against class " + cParam.Class + ".");
                }
            }
            else if (cParam.Attributes != null && cParam.Attributes != string.Empty)
            {
                if (hash.Contains(cParam.Class))
                {
                   existingClass = (Class)hash[cParam.Class];
                    att = AttribToHashtable(existingClass.Attributes);
                }
                existingClass.Attributes = GetClassAttributes(GetAttributes(att));
                hash[existingClass.Name] = existingClass;
            }

            return hash;
        }

        static public Class[] GetSourceClass(Hashtable pParams)
        {
            Class[] param = new Class[pParams.Count];
            IDictionaryEnumerator enu = pParams.GetEnumerator();
            int index = 0;
            while (enu.MoveNext())
            {
                param[index] = new Class();
                param[index].Name = (string)enu.Key;
                param[index] = (Class)enu.Value;
                index++;
            }
            return param;
        }


        static public bool ValidateClass(string cl, ArrayList cc)
        {
            foreach (Class c in cc)
            {
                if (c.Name.Equals(cl))
                    return false;
            }
            return true;
        }

        static public Hashtable ClassToHashtable(Alachisoft.NCache.Config.Dom.Class[] cl)
        {
            Hashtable hash = new Hashtable();
            for (int i = 0; i < cl.Length; i++)
            {
                hash.Add(cl[i].Name, cl[i]);
            }
            return hash;
        }
        static public Hashtable AttribToHashtable(Alachisoft.NCache.Config.Dom.Attrib[] cl)
        {
            Hashtable hash = new Hashtable();
            for (int i = 0; i < cl.Length; i++)
            {
                hash.Add(cl[i].Name, cl[i]);
            }
            return hash;
        }
    }
}

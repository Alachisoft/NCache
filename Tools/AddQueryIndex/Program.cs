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


namespace Alachisoft.NCache.Tools.AddQueryIndex
{
    class Application
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                ConfigureQueryIndexTool.Run(args);
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

    public class ConfigureQueryIndexParam :  Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string _asmPath = string.Empty;
        private string _class = string.Empty;
        private string _attributes=string.Empty;
        private string _cacheId = string.Empty;
        private string _server = string.Empty;
        private int _port = -1;
        private bool _nodeploy = false;
        private string _depAsmPath = string.Empty;

        public ConfigureQueryIndexParam()
        {
        }

        [ArgumentAttribute(@"/a", @"/assembly-path")]
        public string AsmPath
        {
            get { return _asmPath; }
            set { _asmPath = value; }
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
    sealed class ConfigureQueryIndexTool
    {

        static private ConfigureQueryIndexParam cParam = new ConfigureQueryIndexParam();
        static private NCacheRPCService NCache = new NCacheRPCService("");
        static private ICacheServer cacheServer;
        ToolOperations toolOp = new ToolOperations();
        static LogErrors logErr = new LogErrors(PrintMessage);
        
        /// <summary>
        /// Validate all parameters in property string.
        /// </summary>
        private static bool ValidateParameters()
        {
            // Validating CacheId
            
            if (string.IsNullOrEmpty(cParam.CacheId))
            {
                Console.Error.WriteLine("\nError: Cache name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(cParam.Class))
            {
                Console.Error.WriteLine("\nError: Class name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(cParam.AsmPath))
            {
                Console.Error.WriteLine("\nError: Assembly path not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(cParam.Attributes))
            {
                Console.Error.WriteLine("\nError: Attributes not specified.");
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
                ncLog.Source = "NCache:AddQueryIndex Tool";
                ncLog.WriteEntry(msg, type);
            }
        }

        private static void PrintMessage(string msg)
        {
            Console.Error.WriteLine(msg);
        }

        /// <summary>
        /// The main entry point for the tool.
        /// </summary>ju
        static public void Run(string[] args)
        {
            bool successful = true;
            System.Reflection.Assembly asm = null;
            Alachisoft.NCache.Config.Dom.Class[] queryClasses=null;
            string failedNodes = string.Empty;
            string serverName = string.Empty;
            try
            {
                object param = new ConfigureQueryIndexParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (ConfigureQueryIndexParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }
                if (!ValidateParameters()) 
                {
                    successful = false;
                    return;
                }
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

                string extension = ".dll";
                if (cacheServer != null)
                {
                    serverName = cacheServer.GetClusterIP();
                    if (cacheServer.IsRunning(cParam.CacheId))
                        throw new Exception(cParam.CacheId + " is Running on " + serverName +
                                            "\nStop the cache first and try again.");

                    Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig =
                        cacheServer.GetNewConfiguration(cParam.CacheId);

                    if (serverConfig == null)
                        throw new Exception("Specified cache is not registered on the given server.");

                    try
                    {
                        asm = System.Reflection.Assembly.LoadFrom(cParam.AsmPath);

                        extension = Path.GetExtension(asm.FullName);
                    }
                    catch (Exception e)
                    {
                        string message = string.Format("Could not load assembly \"" + cParam.AsmPath + "\". {0}",
                            e.Message);
                        Console.Error.WriteLine("Error : {0}", message);
                        LogEvent(e.Message);
                        successful = false;
                        return;
                    }

                    if (asm == null)
                        throw new Exception("Could not load specified Assembly");

                    System.Type type = asm.GetType(cParam.Class, true);


                    if (serverConfig.CacheSettings.QueryIndices == null)
                    {
                        serverConfig.CacheSettings.QueryIndices = new Alachisoft.NCache.Config.Dom.QueryIndex();
                        serverConfig.CacheSettings.QueryIndices.Classes = queryClasses;
                    }

                    queryClasses = serverConfig.CacheSettings.QueryIndices.Classes;

                    serverConfig.CacheSettings.QueryIndices.Classes = GetSourceClass(GetClass(queryClasses, asm));

                    if (serverConfig.CacheSettings.CacheType == "clustered-cache")
                    {
                        foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                        {
                            NCache.ServerName = node.IpAddress.ToString();
                            try
                            {
                                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                                if (cacheServer.IsRunning(cParam.CacheId))
                                    throw new Exception(cParam.CacheId + " is Running on " + serverName +
                                                        "\nStop the cache first.");

                                Console.WriteLine("Adding query indexes on node '{0}' to cache '{1}'.", serverName, cParam.CacheId);
                                cacheServer.RegisterCache(cParam.CacheId, serverConfig, "", true,
                                    cParam.IsHotApply);
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine("Failed to Add Query Index on '{0}'. ", serverName);
                                Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);
                                failedNodes = failedNodes + "/n" + node.IpAddress.ToString();
                                LogEvent(ex.Message);
                                successful = false;
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
                            Console.WriteLine("Adding query indexes on node '{0}' to cache '{1}'.", serverName, cParam.CacheId);
                            cacheServer.RegisterCache(cParam.CacheId, serverConfig, "", true, cParam.IsHotApply);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Failed to Add Query Index on '{0}'. ", NCache.ServerName);
                            Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);
                            LogEvent(ex.Message);
                            successful = false;
                        }
                        finally
                        {
                            cacheServer.Dispose();
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error : {0}", e.Message);
                LogEvent(e.Message);
                successful = false;
            }
            finally
            {
                NCache.Dispose();
                if (successful && !cParam.IsUsage)
                    Console.WriteLine("Query indexes successfully added.");
            }
        }

        static public Hashtable GetAttributes(Hashtable attrib)
        {
            string[] str = cParam.Attributes.Split(new char[] { '$' });
            Hashtable hash = new Hashtable();
            foreach (string st in str)
            {
                hash.Add(st, st);
            }
            //for merging attributes
            if (attrib.Count != 0 && attrib != null)
            {
                IDictionaryEnumerator ide = attrib.GetEnumerator();
                while (ide.MoveNext())
                {
                    hash[(string)ide.Key] =(string) ide.Value;
                }
            }
            return hash;
        }

        static public Attrib[] GetClassAttributes(Hashtable attrib, System.Type type)
        {
            System.Collections.Generic.List<Attrib> a = new System.Collections.Generic.List<Attrib>();
            IDictionaryEnumerator enu = attrib.GetEnumerator();
            System.Reflection.PropertyInfo pi = null;
            System.Reflection.FieldInfo fi = null;
            string dt = null;
            string _unsupportedtypes = "";
            bool _nonPrimitiveAttSpecified = false;

            while (enu.MoveNext())
            {
                pi = type.GetProperty(enu.Key.ToString());
                if(pi!=null)
                {
                    dt = pi.PropertyType.FullName;
                }
                if (pi == null)
                {
                    fi = type.GetField(enu.Key.ToString());
                    if(fi!=null)
                    dt = fi.FieldType.FullName;
                }
                if (pi != null || fi != null)
                {
                    Attrib tempAttrib = new Attrib();

                    tempAttrib.Name = (string)enu.Key;
                    tempAttrib.ID = (string)enu.Value;
                    tempAttrib.Type = dt;
                    System.Type currentType =System.Type.GetType(dt);
                    if (currentType != null && !currentType.IsPrimitive && currentType.FullName != "System.DateTime" && currentType.FullName != "System.String" && currentType.FullName != "System.Decimal")
                    {
                        _nonPrimitiveAttSpecified = true;
                        _unsupportedtypes += currentType.FullName + "\n";
                    }
                    if (currentType == null)
                    {
                        _nonPrimitiveAttSpecified = true;
                        _unsupportedtypes += "Unknown Type\n";
                    }
                    a.Add(tempAttrib);
                }
                else
                {
                    string message = "Invalid class attribute(s) specified '" + enu.Key.ToString() + "'.";
                    throw new Exception(message );
                }
                pi = null;
                fi = null;
            }
            if (_nonPrimitiveAttSpecified)
                throw new Exception("NCache Queries only support primitive types. The following type(s) is/are not supported:\n"+_unsupportedtypes);

            return (Attrib[])a.ToArray();
        }
        static public Hashtable GetClass(Alachisoft.NCache.Config.Dom.Class[] cl, System.Reflection.Assembly asm)
        {
            Hashtable hash = new Hashtable();
            Hashtable att = new Hashtable();

            Alachisoft.NCache.Config.Dom.Class c = new Alachisoft.NCache.Config.Dom.Class();
            c.Name = cParam.Class;

            System.Type type = asm.GetType(cParam.Class, true);
            string assemblySrt = null;

            assemblySrt = asm.FullName;          
       
            c.ID = cParam.Class;

            if (cl != null)
            {
                hash=ClassToHashtable(cl);
            }

            if(hash.Contains(c.Name))
            {
                Class existingClass = (Class)hash[c.Name];
                att = AttribToHashtable(existingClass.Attributes);
            }

            if (cParam.Attributes != null || cParam.Attributes != string.Empty)
            {
                c.Attributes = GetClassAttributes(GetAttributes(att),type);
            }
         
            hash[c.Name]= c;
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
                hash.Add(cl[i].ID, cl[i].Name);
            }
            return hash;
        }
    }
}

using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Alachisoft.NCache.Caching.Queries.Lucene;
using Alachisoft.NCache.Common.Queries.Lucene;
using Spatial4n.Core.Context;
using Alachisoft.NCache.Caching.Queries.Lucene.Util;
using Alachisoft.NCache.Common.AssemblyBrowser;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Add, "LuceneQueryIndex")]
    public class AddLuceneQueryIndexBase : AddLuceneQueryIndexParameters, IConfiguration
    {
        
        private string TOOLNAME = "AddLuceneQueryIndex Tool";
        private NCacheRPCService NCache = new NCacheRPCService("");
        Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig;
        private ICacheServer cacheServer;
        private LuceneAttributes defaults;
        ToolOperations toolOp = new ToolOperations();
        public bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(CacheName))
            {
                OutputProvider.WriteErrorLine("\nError: Cache name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(Class))
            {
                OutputProvider.WriteErrorLine("\nError: Class name not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(AssemblyPath))
            {
                OutputProvider.WriteErrorLine("\nError: Assembly path not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(Attributes))
            {
                OutputProvider.WriteErrorLine("\nError: Attributes not specified.");
                return false;
            }
            

            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }
        public void AddLuceneQueryIndex()
        {
            if (!ValidateParameters())
                return;
            bool successful = true;
            AssemblyDef asm = null;
            //ArrayList cc = new ArrayList();
            Alachisoft.NCache.Config.Dom.Class[] queryClasses = null;
            string failedNodes = string.Empty;
            string serverName = string.Empty;

            try
            {

                if (Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                if (Server != null && Server != string.Empty)
                {

                    NCache.ServerName = Server;
                }
                if (Port != -1)
                {
                    NCache.Port = Port;
                }
                try
                {
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception e)
                {
                    OutputProvider.WriteErrorLine("Error: NCache service could not be contacted on server.");
                    return;
                }

                ToolsUtil.VerifyClusterConfigurations(cacheServer.GetNewConfiguration(CacheName), CacheName);


                string extension = ".dll";
                if (cacheServer != null)
                {
                    serverName = cacheServer.GetClusterIP();
                    if (cacheServer.IsRunning(CacheName))
                        throw new Exception(CacheName + " is Running on " + serverName +
                                            "\nStop the cache first and try again.");

                    serverConfig = cacheServer.GetNewConfiguration(CacheName);
                    //ConfiguredCacheInfo[] configuredCaches = cacheServer.GetAllConfiguredCaches();

                    if (serverConfig == null)
                        throw new Exception("Specified cache is not registered on the given server.");

                    //if (! Unregister)
                    //{
                    try
                    {
                        asm = AssemblyDef.LoadFrom(AssemblyPath);

                        extension = Path.GetExtension(asm.FullName);
                    }
                    catch (Exception e)
                    {
                        string message = string.Format("Could not load assembly \"" + AssemblyPath + "\". {0}", e.Message);
                        OutputProvider.WriteErrorLine("Error : {0}", message);
                        successful = false;
                        return;
                    }

                    if (asm == null)
                        throw new Exception("Could not load specified Assembly");

                    TypeDef type = asm.GetType(Class);


                    if (serverConfig.CacheSettings.QueryIndices == null)
                    {
                        serverConfig.CacheSettings.QueryIndices = new Alachisoft.NCache.Config.Dom.QueryIndex();
                        serverConfig.CacheSettings.QueryIndices.Classes = queryClasses;
                    }

                    queryClasses = serverConfig.CacheSettings.QueryIndices.Classes;

                    serverConfig.CacheSettings.QueryIndices.Classes = GetSourceClass(GetClass(queryClasses, asm));


                    byte[] userId = null;
                    byte[] paswd = null;
                    if (UserId != string.Empty && Password != string.Empty)
                    {
                        userId = EncryptionUtil.Encrypt(UserId);
                        paswd = EncryptionUtil.Encrypt(Password);
                    }
                    serverConfig.ConfigVersion++;
                    if (serverConfig.CacheSettings.CacheType == "clustered-cache")
                    {
                        foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                        {
                            NCache.ServerName = node.IpAddress.ToString();
                            try
                            {
                                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                                if (cacheServer.IsRunning(CacheName))
                                    throw new Exception(CacheName + " is Running on " + serverName +
                                                       "\nStop the cache first and try again.");

                                OutputProvider.WriteLine("Adding query indexes on node '{0}' to cache '{1}'.", node.IpAddress, CacheName);
                                cacheServer.RegisterCache(CacheName, serverConfig, "", true, userId, paswd, false);
                            }
                            catch (Exception ex)
                            {
                                OutputProvider.WriteErrorLine("Failed to Add Query Index on '{0}'. ", serverName);
                                OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);
                                failedNodes = failedNodes + "/n" + node.IpAddress.ToString();
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
                            OutputProvider.WriteLine("Adding query indexes on node '{0}' to cache '{1}'.", serverName, CacheName);
                            cacheServer.RegisterCache(CacheName, serverConfig, "", true, userId, paswd, false);
                        }
                        catch (Exception ex)
                        {
                            OutputProvider.WriteErrorLine("Failed to Add Query Index on '{0}'. ", serverName);
                            OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);
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
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);
                successful = false;
            }
            finally
            {
                NCache.Dispose();
                if (successful && !IsUsage)
                    OutputProvider.WriteLine("Query indexes successfully added.");
            }
        }
        private void ValidateLuceneAttribute(LuceneAttrib attrib)
        {
            // TODO : [Mehreen] Insert Khubsurti for validating all here
        }
        public void ConflictWithNormalAttributes(Attrib[] attribs, Hashtable luceneAttribs)
        {
            foreach (Attrib attrib in attribs)
            {
                if(luceneAttribs.ContainsKey(attrib.ID))
                    throw new Exception("Attribute " + attrib.ID + " already has a query index defined");
            }
        }

        public Attrib[] GetClassAttributes(Hashtable attrib, TypeDef type)
        {
            System.Collections.Generic.List<Attrib> a = new System.Collections.Generic.List<Attrib>();
            IDictionaryEnumerator enu = attrib.GetEnumerator();
            PropertyDef pi = null;
            FieldDef fi = null;
            string dt = null;
            string _unsupportedtypes = "";
            bool _nonPrimitiveAttSpecified = false;

            while (enu.MoveNext())
            {
                pi = type.GetProperty(enu.Key.ToString());
                if (pi != null)
                {
                    dt = pi.PropertyType.FullName;
                }
                if (pi == null)
                {
                    fi = type.GetField(enu.Key.ToString());
                    if (fi != null)
                        dt = fi.FieldType.FullName;
                }
                if (pi != null || fi != null)
                {
                    Attrib tempAttrib = new Attrib();

                    tempAttrib.Name = (string)enu.Key;
                    tempAttrib.ID = (string)enu.Value;
                    tempAttrib.Type = dt;
                    System.Type currentType = System.Type.GetType(dt);
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
                    throw new Exception(message);
                }
                pi = null;
                fi = null;
            }
            if (_nonPrimitiveAttSpecified)
                throw new Exception("NCache Queries only support primitive types. The following type(s) is not supported:\n" + _unsupportedtypes);

            return (Attrib[])a.ToArray();
        }
        public Hashtable GetClass(Alachisoft.NCache.Config.Dom.Class[] cl, AssemblyDef asm)
        {
            Hashtable hash = new Hashtable();
            Hashtable latt = new Hashtable();
            Alachisoft.NCache.Config.Dom.Class c = new Alachisoft.NCache.Config.Dom.Class();
            //c.Assembly = asm.ToString(); //cg

            c.Name = Class;
            TypeDef type = asm.GetType(Class);
            string assemblySrt = null;
            assemblySrt = asm.FullName;//= c.Assembly ; //cg

            String fullVersion = String.Empty;

            if (!String.IsNullOrEmpty(assemblySrt))
            {
                String version = assemblySrt.Split(',')[1];
                fullVersion = version.Split('=')[1];
            }
            c.ID = Class;
            if (cl != null)
            {
                hash = ClassToHashtable(cl);

            }
            defaults = CreateLuceneDefaults();
            if (hash.Contains(c.Name))
            {
                Class existingClass = (Class)hash[c.Name];
                if (existingClass.LuceneSection != null)
                {
                    defaults = existingClass.LuceneSection;
                    latt = LuceneAttribToHashtable(existingClass.LuceneSection.Attributes);
                    if (existingClass.Attributes != null)
                        c.Attributes = existingClass.Attributes;
                }
            }
            c.LuceneSection = defaults;

            if (Attributes != null || Attributes != string.Empty)
            {
                latt = LuceneAttributesToAdd(latt,type);
                if(c.Attributes != null)
                    ConflictWithNormalAttributes(c.Attributes, latt);
                c.LuceneSection.Attributes = latt.Values.Cast<LuceneAttrib>().ToArray();
            }
            

            hash[c.Name] = c;
            return hash;
        }

        private bool ValidateDataType(System.Type type)
        {
            if (type != null && !type.IsPrimitive && type.FullName != "System.DateTime" && type.FullName != "System.String" && type.FullName != "System.Decimal")
                return false;
            if (type == null)
                return false;
            return true;
        }

        private Dictionary<string, LuceneDeployment> GetDeployements(LuceneDepType depTypes)
        {
            Dictionary<string, LuceneDeployment> deployments = new Dictionary<string, LuceneDeployment>();
            foreach (LuceneDeployment dep in depTypes.Providers)
            {
                deployments.Add(dep.Name, dep);
            }
            return deployments;
        }
        private LuceneAttrib GetLuceneAttrib(string attribName, TypeDef classType)
        {
            PropertyDef pi = null;
            FieldDef fi = null;
            LuceneAttrib tempAttrib = new LuceneAttrib();
            string dt = null;
            pi = classType.GetProperty(attribName);
            if (pi != null)
            {
                dt = pi.PropertyType.FullName;
            }
            if (pi == null)
            {
                fi = classType.GetField(attribName);
                if (fi != null)
                    dt = fi.FieldType.FullName;
            }
            if (pi != null || fi != null)
            {
                AttributeValidator validator = new AttributeValidator(defaults);
                tempAttrib.Name = attribName;
                tempAttrib.ID = attribName;
                tempAttrib.Type = dt;
                tempAttrib.LuceneType = LuceneType;
                tempAttrib.LuceneAnalyzer = Analyzer;
                tempAttrib.TermVector = TermVector;
                tempAttrib.Pattern = Pattern;
                tempAttrib.StopWords = StopWords;
                

                if (!validator.ValidateTermVector(tempAttrib.TermVector))
                    throw new Exception("Invalid Term Vector specified");
                System.Type currentType = System.Type.GetType(dt);
                if (!ValidateDataType(currentType))
                {
                    //TODO: [Mehreen] :Insert Khubsurti here so it can collectively throw one exception
                    //_nonPrimitiveAttSpecified = true;
                    //_unsupportedtypes += currentType.FullName + "\n";
                    throw new Exception("NCache only supports premitive types. Type " + currentType.FullName + " is not supported");
                }
                else if (currentType == null)
                {
                    //_nonPrimitiveAttSpecified = true;
                    //_unsupportedtypes += "Unknown Type\n";
                    throw new Exception("NCache only supports premitive types. Type Unknown Type\n");
                }
                else if (!validator.ValidateLuceneType(tempAttrib.LuceneType, currentType))
                {
                    throw new Exception("Can not create Lucene Index of type " + tempAttrib.LuceneType + " on data type " + currentType.FullName);
                }
                else if (tempAttrib.LuceneType == LuceneUtil.SPATIAL)
                {
                    tempAttrib.SpaStrategy = SpatialStrategy != null ? SpatialStrategy : LuceneUtil.SpatialStrategy.BBOX;
                    tempAttrib.SpaPrefixTree = SpatialPrefixTree != null ? SpatialPrefixTree : LuceneUtil.SpatialPrefixTre.GEOHASH;
                    if (!AttributeValidator.ValidateSpatialStrategy(tempAttrib.SpaStrategy, tempAttrib.Name, tempAttrib.SpaPrefixTree, SpatialContext.GEO))
                        throw new Exception("Spatial Startegy or Prefix Tree are not valid");
                }
                else if (tempAttrib.LuceneType == LuceneUtil.STRING)
                {
                    if (serverConfig.CacheSettings.LuceneSettings != null)
                    {
                        if (!validator.ValidateAnalyzer(tempAttrib.LuceneAnalyzer, GetDeployements(serverConfig.CacheSettings.LuceneSettings.Analyzers)))
                            throw new Exception("Analyzer " + tempAttrib.LuceneAnalyzer + " is not deployed");
                        if (!validator.ValidatePattern(tempAttrib.Pattern, GetDeployements(serverConfig.CacheSettings.LuceneSettings.Patterns)))
                            throw new Exception("Pattern " + tempAttrib.Pattern + " is not deployed");
                        if (!validator.ValidateStopWords(tempAttrib.StopWords, GetDeployements(serverConfig.CacheSettings.LuceneSettings.StopWordFiles)))
                            throw new Exception("Pattern " + tempAttrib.StopWords + " is not deployed");
                    }
                    else
                    {
                        if (!validator.ValidateAnalyzer(tempAttrib.LuceneAnalyzer, null))
                            throw new Exception("Analyzer " + tempAttrib.LuceneAnalyzer + " is not deployed");
                        if (!validator.ValidatePattern(tempAttrib.Pattern, null))
                            throw new Exception("Pattern " + tempAttrib.Pattern + " is not deployed");
                        if (!validator.ValidateStopWords(tempAttrib.StopWords, null))
                            throw new Exception("Pattern " + tempAttrib.StopWords + " is not deployed");
                    }
                }

            
            }
            else
            {
                string message = "Invalid class attribute(s) specified '" + attribName + "'.";
                throw new Exception(message);
            }
            return tempAttrib;
        }
        //private LuceneAttrib[] GetClassLuceneAttributes(Hashtable lattrib, System.Type type)
        //{
        //    System.Collections.Generic.List<LuceneAttrib> a = new System.Collections.Generic.List<LuceneAttrib>();
        //    IDictionaryEnumerator enu = lattrib.GetEnumerator();
        //    System.Reflection.PropertyInfo pi = null;
        //    System.Reflection.FieldInfo fi = null;
        //    string dt = null;
        //    string _unsupportedtypes = "";
        //    bool _nonPrimitiveAttSpecified = false;

        //    while (enu.MoveNext())
        //    {
        //        pi = type.GetProperty(enu.Key.ToString());
        //        if (pi != null)
        //        {
        //            dt = pi.PropertyType.FullName;
        //        }
        //        if (pi == null)
        //        {
        //            fi = type.GetField(enu.Key.ToString());
        //            if (fi != null)
        //                dt = fi.FieldType.FullName;
        //        }
        //        if (pi != null || fi != null)
        //        {
        //            LuceneAttrib tempAttrib = new LuceneAttrib();

        //            tempAttrib.Name = (string)enu.Key;
        //            tempAttrib.ID = (string)enu.Value;
        //            tempAttrib.Type = dt;
        //            tempAttrib.LuceneType = LuceneType;
        //            tempAttrib.LuceneAnalyzer = Analyzer;
        //            tempAttrib.TermVector = TermVector;
        //            tempAttrib.FieldStore = FieldStore;
        //            tempAttrib.Pattern = Pattern;
        //            tempAttrib.StopWords = StopWords;
        //            System.Type currentType = System.Type.GetType(dt);
        //            if (!ValidateDataType(currentType))
        //            {
        //                _nonPrimitiveAttSpecified = true;
        //                _unsupportedtypes += currentType.FullName + "\n";
        //            }
        //            else if (currentType == null)
        //            {
        //                _nonPrimitiveAttSpecified = true;
        //                _unsupportedtypes += "Unknown Type\n";
        //            }
        //            else if (!ValidateLuceneType(tempAttrib.LuceneType, currentType)) 
        //            {
        //                throw new Exception("Can not create Lucene Index of type " + tempAttrib.LuceneType + " on data type " + currentType.FullName);
        //            }
        //            a.Add(tempAttrib);
        //        }
        //        else
        //        {
        //            string message = "Invalid class attribute(s) specified '" + enu.Key.ToString() + "'.";
        //            throw new Exception(message);
        //        }
        //        pi = null;
        //        fi = null;
        //    }
        //    if (_nonPrimitiveAttSpecified)
        //        throw new Exception("NCache Queries only support primitive types. The following type(s) is not supported:\n" + _unsupportedtypes);

        //    return (LuceneAttrib[])a.ToArray();
        //}

        private Hashtable LuceneAttributesToAdd(Hashtable latt, TypeDef classType)
        {
            string[] str = Attributes.Split(new char[] { '$' });
            foreach (string st in str)
            {
                latt[st] = GetLuceneAttrib(st, classType);
            }
            return latt;
        }

        private LuceneAttributes CreateLuceneDefaults()
        {
            LuceneAttributes lAttribdefault = new LuceneAttributes();
            lAttribdefault.LuceneType = LuceneUtil.STRING;
            lAttribdefault.MergeFactor = 5;
            lAttribdefault.LuceneAnalyzer = LuceneUtil.DefaultAnalyzers.STANDARD;
            lAttribdefault.TermVector = LuceneUtil.TermVectors.NO;
            lAttribdefault.Pattern = LuceneUtil.DEFAULT;
            lAttribdefault.StopWords = LuceneUtil.DEFAULT;
            return lAttribdefault;
        }

        public Class[] GetSourceClass(Hashtable pParams)
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


        public bool ValidateClass(string cl, ArrayList cc)
        {
            foreach (Class c in cc)
            {
                if (c.Name.Equals(cl))
                    return false;

            }
            return true;
        }

        public Hashtable ClassToHashtable(Alachisoft.NCache.Config.Dom.Class[] cl)
        {
            Hashtable hash = new Hashtable();
            for (int i = 0; i < cl.Length; i++)
            {
                hash.Add(cl[i].Name, cl[i]);
            }
            return hash;
        }
        public Hashtable LuceneAttribToHashtable(Alachisoft.NCache.Config.Dom.LuceneAttrib[] la)
        {
            Hashtable hash = new Hashtable();
            for (int i = 0; i < la.Length; i++)
            {
                hash.Add(la[i].ID, la[i]);
            }
            return hash;
        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }
#if NETCORE
        private static System.Reflection.Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            string final = "";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string bin = directoryInfo.Parent.Parent.FullName;
                final = System.IO.Path.Combine(bin, "service");/// from where you neeed the assemblies
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string installDir = directoryInfo.Parent.FullName; //linux install directory
                directoryInfo = Directory.GetParent(installDir); //go back one directory
                installDir = directoryInfo.FullName;
                final = Path.Combine(installDir, "lib");
            }
            return System.Reflection.Assembly.LoadFrom(Path.Combine(final, new AssemblyName(args.Name).Name + ".dll"));
        }
#endif

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Add-LuceneQueryIndex Cmdlet";
                AddLuceneQueryIndex();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }
    }
}

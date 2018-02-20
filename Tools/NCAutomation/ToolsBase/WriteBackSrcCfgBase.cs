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

using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.IO;
using System.Reflection;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommunications.Write, "BackingSourceConfig")]
    public class WriteBackSrcCfgBase : WriteBackSrcCfgParameter, IConfiguration
    {

        private string TOOLNAME = "WriteBackingSourceConfig Tool";
        bool successFull = true;
        private BackingSource backingSource = new BackingSource();
        private bool _isBatching = false;
        private int _operationDelay = 0;
        private int _batchInterval = 5;
        private int _operationPerSecond = 500;
        private int _operationQueueLimit = 5000;
        private int _operationEvictionRatio = 5;

        public bool ValidateParameters()
        {
            if ((!ReadThru) && (!WriteThru))
            {
                OutputProvider.WriteErrorLine("Error: ReadThru/WriteThru not specified.");
                return false;
            }

            if(!string.IsNullOrEmpty(OutputFile))
            {
                string path = Path.GetExtension(OutputFile);
                if (!(path.Contains(".txt") || path.Contains(".xml")))
                {
                    OutputProvider.WriteErrorLine("Invalid file. Please specify path of .xml or .txt file.");
                    return false;
                }
            }

            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }

        public void WriteBackingSourceConfig()
        {
            if (!ValidateParameters())
                return;

            System.Reflection.Assembly asm = null;
            Alachisoft.NCache.Config.Dom.Provider[] prov = new Provider[1];

            try
            {
                asm = System.Reflection.Assembly.LoadFrom(AssemblyPath);
            }
            catch (Exception e)
            {
                successFull = false;
                string message = string.Format("Could not load assembly \"" + AssemblyPath + "\". {0}", e.Message);
                OutputProvider.WriteErrorLine("Error: {0}", message);
                return;
            }

            if (asm == null)
            {
                successFull = false;
                throw new Exception("Could not load specified assembly.");
            }

            System.Type type = asm.GetType(Class, true);

            prov[0] = new Provider();
            prov[0].ProviderName = ProviderName;
            prov[0].AssemblyName = asm.FullName;
            prov[0].ClassName = Class;
            prov[0].FullProviderName = asm.GetName().Name + ".dll";
            if (!string.IsNullOrEmpty(Parameters))
                prov[0].Parameters = GetParams(Parameters);
            if(DefaultProvider == true)
                prov[0].IsDefaultProvider = true;
            else
                prov[0].IsDefaultProvider = false;


            if (ReadThru)
            {
                System.Type typeProvider = type.GetInterface("IReadThruProvider");

                if (typeProvider == null)
                {
                    successFull = false;
                    OutputProvider.WriteErrorLine("Error: Specified class does not implement IReadThruProvider.");
                    return;
                }
                else
                {
                    backingSource.Readthru = new Readthru();
                    backingSource.Readthru.Enabled = true;
                    backingSource.Readthru.Providers = prov;
                    
                }
            }
            else if (WriteThru)
            {
                System.Type typeProvider = type.GetInterface("IWriteThruProvider");

                if (typeProvider == null)
                {
                    successFull = false;
                    OutputProvider.WriteErrorLine("Error: Specified class does not implement IWriteThruProvider.");
                    return;
                }
                else
                {
                    backingSource.Writethru = new Writethru();
                    backingSource.Writethru.Enabled = true;
                    backingSource.Writethru.Providers = prov;
                    backingSource.Writethru.WriteBehind = new WriteBehind();
                    backingSource.Writethru.WriteBehind.Mode = "non-batch";
                    backingSource.Writethru.WriteBehind.Throttling = _operationPerSecond.ToString();
                    backingSource.Writethru.WriteBehind.RequeueLimit = _operationQueueLimit.ToString();
                    backingSource.Writethru.WriteBehind.Eviction = _operationEvictionRatio.ToString();
                }
            }

            ConfigurationBuilder cfg = new ConfigurationBuilder();
            string output = cfg.GetSectionXml(backingSource, "backing-source", 1);

            if (string.IsNullOrEmpty(OutputFile))
            {
                OutputProvider.WriteLine(output);
            }
            else
            {
                StringBuilder xml = new StringBuilder();
                xml.Append(output);
                WriteXmlToFile(xml.ToString());
                OutputProvider.WriteLine("BackingSource config for Class " + Class + " is generated at " + OutputFile);
                OutputProvider.WriteLine(System.Environment.NewLine);
            }

        }

        public void WriteXmlToFile(string xml)
        {
            if (OutputFile.Length == 0)
            {
                throw new ManagementException("Can not locate path for writing config.");
            }

            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = new FileStream(OutputFile, FileMode.Create);
                sw = new StreamWriter(fs);

                sw.Write(xml);
                sw.Flush();
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
            finally
            {
                if (sw != null)
                {
                    try
                    {
                        sw.Close();
                    }
                    catch (Exception)
                    {
                    }
                    sw.Dispose();
                    sw = null;
                }
                if (fs != null)
                {
                    try
                    {
                        fs.Close();
                    }
                    catch (Exception)
                    {
                    }
                    fs.Dispose();
                    fs = null;
                }
            }
        }

        public Parameter[] GetParams(string param)
        {

            Hashtable hash = new Hashtable();
            string[] st = param.Split(new char[] { '$' });
            for (int i = 0; i < st.Length; i++)
            {
                string[] str = st[i].Split(new char[] { '=' }, 2);
                hash.Add(str[0], str[1]);
            }

            Parameter[] _parameter = new Parameter[hash.Count];
            IDictionaryEnumerator enu = hash.GetEnumerator();
            int index = 0;
            while (enu.MoveNext())
            {
                _parameter[index] = new Parameter();
                _parameter[index].Name = (string)enu.Key;
                _parameter[index].ParamValue = (string)enu.Value;

                index++;
            }
            return _parameter;
        }

        public Parameter[] GetProviderParams(Hashtable pParams)
        {
            Parameter[] param = new Parameter[pParams.Count];
            IDictionaryEnumerator enu = pParams.GetEnumerator();
            int index = 0;
            while (enu.MoveNext())
            {
                param[index] = new Parameter();
                param[index].Name = (string)enu.Key;
                param[index].ParamValue = (string)enu.Value;
                index++;
            }
            return param;
        }


        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Write-BackingSourceConfig Cmdlet";
                WriteBackingSourceConfig();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

        protected override void ProcessRecord()
        {
            try { }
            catch { }
        }

#if NETCORE
        private static System.Reflection.Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            string final = "";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string bin = directoryInfo.Parent.Parent.FullName; //bin folder
                final = System.IO.Path.Combine(bin, "service"); /// from where you neeed the assemblies
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
    }
}



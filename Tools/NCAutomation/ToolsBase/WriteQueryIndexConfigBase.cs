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
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommunications.Write, "QueryIndexConfig")]
    public class WriteQueryIndexConfigBase : WriteQueryIndexConfigParameters, IConfiguration
    {
        private string TOOLNAME = "WriteQueryIndexConfig Tool";
        bool _successful = true;
        ToolOperations toolOp = new ToolOperations();


        /// <summary>
        /// Validate all parameters in property string.
        /// </summary>
        public bool ValidateParameters()
        {
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
            if (!OutputFile.Equals(string.Empty) && !(Path.GetExtension(OutputFile).Equals(".txt") || Path.GetExtension(OutputFile).Equals(".xml")))
            {
                OutputProvider.WriteErrorLine("\nError: Extension for the file specified is not valid.");
                return false;

            }
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;

        }
        private void WriteXmlToFile(string xml)
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
        public void GenerateQueryIndex()
        {
            if (!ValidateParameters())
                return;

            System.Reflection.Assembly asm = null;
            Alachisoft.NCache.Config.Dom.Class[] queryClasses = null;
            string failedNodes = string.Empty;
            string serverName = string.Empty;

            try
            {
                string extension = ".dll";
                try
                {
                    asm = System.Reflection.Assembly.LoadFrom(AssemblyPath);
                    extension = Path.GetExtension(asm.FullName);
                }
                catch (Exception e)
                {
                    string message = string.Format("Could not load assembly \"" + AssemblyPath + "\". {0}", e.Message);
                    OutputProvider.WriteErrorLine("Error : {0}", message);
                    _successful = false;
                    return;
                }

                if (asm == null)
                    throw new Exception("Could not load specified Assembly");

                System.Type type = asm.GetType(Class, true);

                QueryIndex queryIndices = new QueryIndex();
                queryIndices.Classes = GetSourceClass(GetClass(queryClasses, asm));

                ConfigurationBuilder cfg = new ConfigurationBuilder();
                string configurationString = cfg.GetSectionXml(queryIndices, "query-indexes", 1);

                if (OutputFile == null || OutputFile.Equals(string.Empty))
                {
                    if (_successful)
                        OutputProvider.WriteLine(configurationString);
                }
                else
                {
                    WriteXmlToFile(configurationString);
                    if (_successful)
                        OutputProvider.WriteLine("Query Indexes generated for Class '{0}' at {1}", Class, OutputFile);
                }


            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);
                _successful = false;
            }
            finally
            {

                OutputProvider.WriteLine(Environment.NewLine);
            }



        }

        public Attrib[] GetClassAttributes(Hashtable attrib, System.Type type)
        {
            System.Collections.Generic.List<Attrib> a = new System.Collections.Generic.List<Attrib>();
            IDictionaryEnumerator enu = attrib.GetEnumerator();
            System.Reflection.PropertyInfo pi = null;
            System.Reflection.FieldInfo fi = null;
            string dt = null;
            string _unsupportedtypes = "";

            while (enu.MoveNext())
            {

                bool _nonPrimitiveAttSpecified = false;
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
                    if (!_nonPrimitiveAttSpecified)
                    {
                        a.Add(tempAttrib);
                    }
                }
                else
                {
                    string message = "Invalid class attribute(s) specified '" + enu.Key.ToString() + "'.";
                }
                pi = null;
                fi = null;
            }

            return (Attrib[])a.ToArray();
        }
        public Hashtable GetClass(Alachisoft.NCache.Config.Dom.Class[] cl, System.Reflection.Assembly asm)
        {
            Hashtable hash = new Hashtable();
            Hashtable att = new Hashtable();
            Alachisoft.NCache.Config.Dom.Class c = new Alachisoft.NCache.Config.Dom.Class();

            c.Name = Class;
            System.Type type = asm.GetType(Class, true);
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

            if (hash.Contains(c.Name))
            {
                Class existingClass = (Class)hash[c.Name];
                att = AttribToHashtable(existingClass.Attributes);
            }

            Hashtable attributeList = new Hashtable();

            foreach (PropertyInfo pInfo in asm.GetType(Class).GetProperties())
            {
                attributeList.Add(pInfo.Name, pInfo.Name);
            }

            c.Attributes = GetClassAttributes(attributeList, type);
            if (c.Attributes.Count() == 0)
            {
                OutputProvider.WriteLine("No indexable property exists in '{0}'", Class);
                _successful = false;
            }

            hash[c.Name] = c;
            return hash;
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
        public Hashtable AttribToHashtable(Alachisoft.NCache.Config.Dom.Attrib[] cl)
        {
            Hashtable hash = new Hashtable();
            for (int i = 0; i < cl.Length; i++)
            {
                hash.Add(cl[i].ID, cl[i].Name);
            }
            return hash;
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
                TOOLNAME = "Add-QueryIndex Cmdlet";
                GenerateQueryIndex();
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

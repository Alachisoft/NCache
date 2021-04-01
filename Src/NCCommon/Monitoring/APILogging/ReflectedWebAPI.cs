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
using Alachisoft.NCache.Common.Monitoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Alachisoft.NCache.Common.Monitoring.APILogging
{
    public class ReflectedWebAPI
    {
        string _assemblyversion;
   
        public ReflectedWebAPI ()
        {
           
        }
        public string AssemblyVersion
        {
            get { return _assemblyversion; }
        }
        XmlDocument LoadXML ()
        {
            
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                Stream s = asm.GetManifestResourceStream("Alachisoft.NCache.Common.Monitoring.APILogging.api-reflection.xml");
                XmlDocument mappingFile = new XmlDocument();
                mappingFile.Load(s);
                s.Close();
                return mappingFile;
            
        }

        IDictionary<string, IDictionary<string, List<OverloadInfo>>> ReadXML(XmlDocument xmlDocument)
        {
            IDictionary<string, IDictionary<string, List<OverloadInfo>>> classWiseMethodInformation = new Dictionary<string, IDictionary<string, List<OverloadInfo>>>(StringComparer.InvariantCultureIgnoreCase);
            try
            {
                XmlNodeList apiNode = xmlDocument.ChildNodes;
                foreach (XmlNode node in apiNode)
                {
                    foreach (XmlAttribute attribute in node.Attributes)
                    {
                        if (attribute.Name.ToLower() == "assembly-version")
                        {
                            _assemblyversion = attribute.Value;
                        }
                    }
                }

                foreach (XmlNode classNode in apiNode[0].ChildNodes)
                {
                    string className = classNode.Attributes["name"].Value;
                    XmlNodeList methods = classNode.ChildNodes;
                    IDictionary<string, List<OverloadInfo>> methodDIc = new Dictionary<string, List<OverloadInfo>>(StringComparer.InvariantCultureIgnoreCase);
                   
                    foreach (XmlNode method in methods)
                    {
                        string methodName = null;
                        foreach (XmlAttribute attribute in method.Attributes)
                        {

                            if (attribute.Name.ToLower() == "title")
                            {
                                methodName = attribute.Value;

                            }
                        }
                        List<OverloadInfo> list = GetMethodsOverloadInformation(method.ChildNodes);
                        methodDIc.Add(methodName.ToLower(), list);

                    }

                    classWiseMethodInformation.Add(className, methodDIc);
                }
                return classWiseMethodInformation;
            }
            catch
            {
                return null;
            }
        }

        List<OverloadInfo> GetMethodsOverloadInformation(XmlNodeList overloadNodes)
        {
            try
            {
                List<OverloadInfo> overloadInfo = new List<OverloadInfo>();

                foreach (XmlNode overload in overloadNodes)
                {
                    OverloadInfo info = new OverloadInfo();

                    foreach (XmlAttribute att in overload.Attributes)
                    {
                        if (att.Name.ToLower() == "number")
                        {
                            if (!String.IsNullOrEmpty(att.Value))
                                info.OverLoad = Convert.ToInt16(att.Value);
                            else
                                continue;
                        }
                    }
                    XmlNodeList parameters = overload.ChildNodes;
                    List<Parameters> parameterList = new List<Parameters>();
                    foreach (XmlNode parameterValue in parameters)
                    {
                        Parameters param = new Parameters();
                        foreach (XmlAttribute paramAttribute in parameterValue.Attributes)
                        {
                            if (paramAttribute.Name.ToLower() == "name")
                            {
                                param.ParameterName = paramAttribute.Value;
                            }

                            if (paramAttribute.Name.ToLower() == "type")
                            {
                                param.ParameterType = paramAttribute.Value;
                            }
                            if (paramAttribute.Name.ToLower() == "sequence-no")
                            {
                                param.ParameterSequence = Convert.ToInt16(paramAttribute.Value);
                            }
                        }
                        parameterList.Add(param);

                    }

                    info.MethodParameters = parameterList;
                    overloadInfo.Add(info);
                }
                return overloadInfo;
            }
            catch
            {
                return null;
            }
        }       

        public IDictionary<string, IDictionary<string, List<OverloadInfo>>> Initialize ()
        {
           return ReadXML(LoadXML());
        }

    }
}

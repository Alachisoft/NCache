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
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using System.IO;
#if !NETCORE
using System.Runtime.Remoting;
#endif
using System.Globalization;
using System.Threading;

namespace Alachisoft.NCache.Common.Configuration
{
    /// <summary>
    /// This class is the key component of the tag based generic configuration handling framework.
    /// The framework handles cofiugration setting specified in XML format. All the properties should
    /// be specified in the form of an attributes e.g
    /// <example>
    /// <Cache id="myCache" retry-interval="5Sec">
    ///  <server name="myPC" priority="1"/>
    ///  <server name="yourPC" priority="2"/>
    /// </Cache>
    /// </example>
    /// The cofiguration framework can load a configuration from XML to an equivalted .Net object model
    /// and can convert back into the XML. The equivalent .Net object should have the same skeleton as
    /// XML do have i.e. hierarchy. .Net class which represents the configuration in object form need
    /// to specify certain custom attributes called cofiguration attributes. These custom attributes
    /// help the framework undersand which .Net object is equivalent to the a specific XML tag. To
    /// see <see cref="Alachisoft.NCache.Common.Configuration.ConfiugrationAttributes."/>. Below is example
    /// .Net class which is populated by the framework from the the above XML.
    /// <example>
    ///  [ConfigurationRoot("Cache")]
   
    public class ConfigurationBuilder
    {
        private Hashtable _baseConfigurationMap = Hashtable.Synchronized(new Hashtable());
        private static string[] _excludedText = new string[] { "sec", "%", "mb" };
        private Dictionary<string, DynamicConfigType> _dynamicSectionTypeMap = new Dictionary<string, DynamicConfigType>();
        private ArrayList _lastLoadedConfiugration = new ArrayList();
        string _file;
        string _path;
        const string DYNAMIC_CONFIG_SECTION = "dynamic-config-object";

        string config="client.ncconf";

        public ConfigurationBuilder(object[] configuration)
        {
            Configuration = configuration;

        }

        public ConfigurationBuilder(string file)
        {
            _file = file;
        }
		/// <summary>
		///In this case user will provide the xml data at the time of Reading
		/// </summary>
		public ConfigurationBuilder()
		{
		}

        public ConfigurationBuilder(string file, string path)
        {
            _file = file;
            _path = path;

        }
        public object[] Configuration
        {
            get { return _lastLoadedConfiugration.ToArray(); }
			
            internal
			set 
            {
                if (value != null)
                {
                    lock (_lastLoadedConfiugration.SyncRoot)
                    {
                        _lastLoadedConfiugration.Clear();
                        for (int i = 0; i < value.Length; i++)
                        {
                            string rootAttrib = ValidateForRootConfiguration(value[i].GetType());
                            if (rootAttrib != null)
                            {
                                _lastLoadedConfiugration.Add(value[i]);
                            }
                            else
                                throw new Exception(value[i].GetType() + " is not marked as RootConfiguration");
                        }
                    }
                }
            }
        }

        public object HttpUtility { get; private set; }

        /// <summary>
        /// Registers a type to be matched for root configuration. ConfigurationBuilder map an XML config
        /// to a .Net class if it is registered with the framework. 
        /// </summary>
        /// <param name="type">type of the object which is to be mapped to a XML section. Registering
        /// object should have a ConfigurationRootAttribute.</param>
        public void RegisterRootConfigurationObject(Type type)
        {
            string rootConfiguratinAttrib = ValidateForRootConfiguration(type);
            if (rootConfiguratinAttrib == null)
                throw new Exception(type.ToString() + " is not marked as RootConfiguration");
            else
                _baseConfigurationMap[rootConfiguratinAttrib.ToLower()] = type;
        }

        private static string ValidateForRootConfiguration(Type type)
        {
            string rootAttrib = null;
            object[] customAttributes = type.GetCustomAttributes(true);

            if (customAttributes != null)
            {
                foreach (Attribute attrib in customAttributes)
                {
                    if (attrib is ConfigurationRootAttribute)
                    {
                        rootAttrib = ((ConfigurationRootAttribute)attrib).RootSectionName;
                        break;
                    }
                }
            }
            return rootAttrib;
        }
        public void ReadConfiguration()
        {
            ReadConfiguration(_file, _path);
        }

		public void ReadConfiguration(string xml)
		{
			ReadConfiguration(_file, _path);
		}

        public void ReadConfiguration(XmlNode bridges)
        {
            XmlNodeList nodeList = bridges.ChildNodes;
            if ((nodeList != null) && (nodeList.Count > 0))
            {
                for (int i = 0; i < nodeList.Count; i++)
                {
                    XmlNode node = nodeList[i];
                    if (((node.NodeType != XmlNodeType.CDATA) && (node.NodeType != XmlNodeType.Comment)) && (node.NodeType != XmlNodeType.XmlDeclaration))
                    {
                        this.ReadConfigurationForNode(node);
                    }
                }
            }
        }


        private void ReadConfiguration(string file, string path)
        {
            if (file == null)
                throw new Exception("File name can not be null");

            path = path == null ? "" : path;

            _lastLoadedConfiugration = new ArrayList();
            string fileName = path + file;
            if (!File.Exists(fileName))
            {
                throw new Exception("File " + fileName + " not found");
            }

            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(fileName);
            }
            catch (Exception e)
            {
                AppUtil.LogEvent("Can not open file : " + fileName + ". Error" + e.ToString(), System.Diagnostics.EventLogEntryType.Error);
                new Exception("Can not open " + fileName + " Error:" + e.ToString());
            }

			ReadConfiguration(document);

        }

		public void ReadConfiguration(XmlDocument xmlDocument)
		{
			XmlNodeList nodeList = xmlDocument.ChildNodes;
			if (nodeList != null && nodeList.Count > 0)
			{
				for (int i = 0; i < nodeList.Count; i++)
				{
					XmlNode node = nodeList[i];

					if (node.NodeType == XmlNodeType.CDATA || node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.XmlDeclaration)
						continue;
					ReadConfigurationForNode(node);
				}
			}
		}
        private Object GetConfigurationObject(string cofingStr)
        {
            Object cfgObject = null;
            if (_baseConfigurationMap.Contains(cofingStr.ToLower()))
            {
                Type type = _baseConfigurationMap[cofingStr.ToLower()] as Type;
                cfgObject = Activator.CreateInstance(type);
                lock (_lastLoadedConfiugration.SyncRoot)
                {
                    _lastLoadedConfiugration.Add(cfgObject);
                }
            }
            return cfgObject;
        }
        private void ReadConfigurationForNode(XmlNode node)
        {
            Object cfgObject = GetConfigurationObject(node.Name.ToLower());
            XmlNodeList nodeList = node.ChildNodes;
            for (int i = 0; i < nodeList.Count; i++)
            {
                XmlNode node1 = nodeList[i];
                if (node1.Name.ToLower() == DYNAMIC_CONFIG_SECTION)
                {
                    ExtractDyanamicConfigSectionObjectType(node1);
                }
            }
            if (cfgObject != null)
            {
                PopulateConfiugrationObject(cfgObject, node);
            }
            else
            {
                for (int i = 0; i < nodeList.Count; i++)
                {
                    node = nodeList[i];
                    ReadConfigurationForNode(node);
                }
            }
        }

        private void PopulateConfiugrationObject(Object config, XmlNode node)
        {
            if (node == null || config == null) return;

            XmlAttributeCollection attribColl = node.Attributes;
            foreach (XmlAttribute xmlAttrib in attribColl)
            {
                FillConfigWithAttribValue(config, xmlAttrib);
            }

            XmlNodeList nodeList = node.ChildNodes;
            Hashtable sameSections = new Hashtable();
            for (int i = 0; i < nodeList.Count; i++)
            {
                XmlNode sectionNode = nodeList[i];
                Type sectionType = null;

                if (sectionNode.Name.ToLower() == DYNAMIC_CONFIG_SECTION && sectionNode.HasChildNodes)
                {
                    ExtractDyanamicConfigSectionObjectType(sectionNode);
                }
            }
            for (int i = 0; i < nodeList.Count; i++)
            {
                XmlNode sectionNode = nodeList[i];
                Type sectionType = null;
                if (sectionNode.Name.ToLower() == DYNAMIC_CONFIG_SECTION) continue;

                sectionType = GetConfigSectionObjectType(config, sectionNode.Name);

                if (sectionType != null)
                {
                    if (sectionType.IsArray)
                    {
                        string nonArrayType = sectionType.FullName.Replace("[]", "");
                        ArrayList sameSessionList = null;
                        Hashtable tmp = null;
                        if (!sameSections.Contains(sectionType))
                        {
                            tmp = new Hashtable();
                            tmp.Add("section-name", sectionNode.Name);
                            
                            sameSessionList = new ArrayList();
                            tmp.Add("section-list", sameSessionList);
                            sameSections.Add(sectionType, tmp);
                        }
                        else
                        {
                            tmp = sameSections[sectionType] as Hashtable;
                            sameSessionList = tmp["section-list"] as ArrayList;
                        }

#if !NETCORE
                        ObjectHandle objHandle = Activator.CreateInstance(sectionType.Assembly.FullName,nonArrayType);
                        object singleSessionObject = objHandle.Unwrap();
#elif NETCORE
                        var singleSessionObject = Activator.CreateInstance(sectionType.GetElementType()); //TODO: ALACHISOFT (This method is changed)
#endif
                        PopulateConfiugrationObject(singleSessionObject, sectionNode);
                        sameSessionList.Add(singleSessionObject);
                    }
                    else
                    {
#if !NETCORE
                        ObjectHandle objHandle = Activator.CreateInstance(sectionType.Assembly.FullName, sectionType.FullName);
                        object sectionConfig = objHandle.Unwrap();
#elif NETCORE
                        var sectionConfig = Activator.CreateInstance(sectionType); //TODO: ALACHISOFT (This method is changed)
#endif
                        PopulateConfiugrationObject(sectionConfig, sectionNode);
                        SetConfigSectionObject(config, sectionConfig, sectionNode.Name);
                    }
                }
            }
            if (sameSections.Count > 0)
            {
                Hashtable tmp;
                IDictionaryEnumerator ide = sameSections.GetEnumerator();
                while (ide.MoveNext())
                {
                    Type arrType = ide.Key as Type;
                    tmp = ide.Value as Hashtable;
                    ArrayList sameSessionList = tmp["section-list"] as ArrayList;
                    string sectionName = tmp["section-name"] as string;
                    object[] sessionArrayObj = Activator.CreateInstance(arrType, new object[] { sameSessionList.Count }) as object[];
                    if (sessionArrayObj != null)
                    {
                        for (int i = 0; i < sameSessionList.Count; i++)
                        {
                            sessionArrayObj[i] = sameSessionList[i];
                        }
                        SetConfigSectionObject(config, sessionArrayObj, sectionName);
                    }
                }
            }

        }

        private Object GetConfigSectionObject(object config, string sectionName)
        {
            Type type = config.GetType();
            PropertyInfo[] objProps = type.GetProperties();

            if (objProps != null)
            {
                for (int i = 0; i < objProps.Length; i++)
                {
                    PropertyInfo propInfo = objProps[i];
                    Object[] customAttribs = propInfo.GetCustomAttributes(typeof(ConfigurationSectionAttribute), false);
                    if (customAttribs != null && customAttribs.Length > 0)
                    {
                        ConfigurationSectionAttribute configSection = customAttribs[0] as ConfigurationSectionAttribute;
                        if (configSection != null &&  String.Compare(configSection.SectionName, sectionName, true) == 0)
                        {
                            return Activator.CreateInstance(propInfo.PropertyType);
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Gets the type of the section object.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        private Type GetConfigSectionObjectType(object config, string sectionName)
        {
            Type sectionType = null;
            Type type = config.GetType();
            PropertyInfo[] objProps = type.GetProperties();

            if (objProps != null)
            {
                for (int i = 0; i < objProps.Length; i++)
                {
                    PropertyInfo propInfo = objProps[i];
                    Object[] customAttribs = propInfo.GetCustomAttributes(typeof(ConfigurationSectionAttribute), false);
                    if (customAttribs != null && customAttribs.Length > 0)
                    {
                        ConfigurationSectionAttribute configSection = customAttribs[0] as ConfigurationSectionAttribute;
                        if (configSection != null &&  String.Compare(configSection.SectionName, sectionName, true) == 0)
                        {
                            sectionType = propInfo.PropertyType;
                            break;
                        }
                    }
                }
            }

            if (sectionType == null)
            {
                if (_dynamicSectionTypeMap.ContainsKey(sectionName.ToLower()))
                {
                    sectionType = _dynamicSectionTypeMap[sectionName.ToLower()].Type;
                }
            }

            return sectionType;
        }
        /// <summary>
        /// Gets the type of the section object.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="sectionName"></param>
        /// <returns></returns>
        private void ExtractDyanamicConfigSectionObjectType(XmlNode node)
        {
            Type sectionType = null;
            if (node != null)
            {
                string assemblyName = null;
                string className = null;
                bool isArray = false;
                string sectionid = null;

                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (attribute.Name.ToLower() == "assembly")
                        assemblyName = attribute.Value;
                    
                    if (attribute.Name.ToLower() == "class")
                        className = attribute.Value;

                    if (attribute.Name.ToLower() == "section-id")
                        sectionid = attribute.Value;

                    if (attribute.Name.ToLower() == "is-array")
                        isArray = Boolean.Parse(attribute.Value);
                }

                if (className == null || sectionid == null)
                    return;
                //Assembly qualified name ; for ref: http://msdn.microsoft.com/en-us/library/system.type.assemblyqualifiedname.aspx


                string assebmlyQualifiedName = null;
                if (assemblyName != null)
                    assebmlyQualifiedName = className + "," + assemblyName;
                else
                    assebmlyQualifiedName = className; 

                sectionType = Type.GetType(assebmlyQualifiedName,true,true);
                
                if (sectionType != null && !string.IsNullOrEmpty(sectionid))
                {
                    _dynamicSectionTypeMap[sectionid] = new DynamicConfigType(sectionType, isArray);
                }
            }
        }
        private void SetConfigSectionObject(object config, object sectionConfig, string sectionName)
        {
            Type type = config.GetType();
            PropertyInfo[] objProps = type.GetProperties();
            try
            {
                if (objProps != null)
                {
                    for (int i = 0; i < objProps.Length; i++)
                    {
                        PropertyInfo propInfo = objProps[i];
                        Object[] customAttribs = propInfo.GetCustomAttributes(typeof(ConfigurationSectionAttribute), false);

                        if (customAttribs != null && customAttribs.Length > 0)
                        {
                            ConfigurationSectionAttribute configSection = customAttribs[0] as ConfigurationSectionAttribute;
                            try
                            {
                                if (configSection != null &&  String.Compare(configSection.SectionName, sectionName, true) == 0)
                                {
                                    propInfo.SetValue(config, sectionConfig, null);

                                }
                            }
                            catch (Exception e)
                            {
                                throw new Runtime.Exceptions.ConfigurationException("Duplicate cache entries in " + config);
                            }
                        }


                        customAttribs = propInfo.GetCustomAttributes(typeof(DynamicConfigurationSectionAttribute), false);
                        if (customAttribs != null && customAttribs.Length > 0)
                        {
                            DynamicConfigurationSectionAttribute configSection = customAttribs[0] as DynamicConfigurationSectionAttribute;
                            try
                            {
                                if (configSection != null && String.Compare(configSection.SectionName, sectionName, true) == 0)
                            {
                                propInfo.SetValue(config, sectionConfig, null);
                            }
                            }
                            catch (Exception e)
                            {
                                throw new Runtime.Exceptions.ConfigurationException("Duplicate cache entries in " + config);
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        public object ConvertToPrimitive(Type type, string value, string appendedText)
        {
            object primitiveValue = null;
            
            if (appendedText != null && appendedText != string.Empty)
                value = value.ToLower().Replace(appendedText.ToLower(), "");

            Type targetType = type;
            bool isNullable = false;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = type.GetGenericArguments()[0];
                isNullable = true;
            }
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
            new System.Globalization.CultureInfo("en-US");
                if (targetType.IsPrimitive)
                {
                    switch (targetType.FullName)
                    {
                        case "System.Byte":
                            if (isNullable)
                                primitiveValue = value != null ? Convert.ToByte(value) : (byte?)null;
                            else
                                primitiveValue = Convert.ToByte(value);
                            break;

                        case "System.Int16":
                            if (isNullable)
                                primitiveValue = value != null ? Convert.ToInt16(value) : (Int16?)null;
                            else
                                primitiveValue = Convert.ToInt16(value);
                            break;

                        case "System.Int32":
                            if (isNullable)
                                primitiveValue = value != null ? Convert.ToInt32(value) : (Int32?)null;
                            else
                                primitiveValue = Convert.ToInt32(value);
                            break;

                        case "System.Int64":
                            if (isNullable)
                                primitiveValue = value != null ? Convert.ToInt64(value) : (Int64?)null;
                            else
                                primitiveValue = Convert.ToInt64(value);
                            break;

                        case "System.Single":
                            if (isNullable)
                                primitiveValue = value != null ? Convert.ToSingle(value) : (Single?)null;
                            else
                                primitiveValue = Convert.ToSingle(value);
                            break;

                        case "System.Double":
                            if (isNullable)
                                primitiveValue = value != null ? Convert.ToDouble(value) : (Double?)null;
                            else
                                primitiveValue = Convert.ToDouble(value);
                            break;

                        case "System.Boolean":
                            //[bug-id: 1434] in case of boolean we can ignore the the case as "True", "true" and "tRue" are the same
                            if (isNullable)
                                primitiveValue = value != null ? Convert.ToBoolean(value) : (Boolean?)null;
                            else
                                primitiveValue = Convert.ToBoolean(value.ToLower());
                            break;

                        case "System.Char":
                            if (isNullable)
                                primitiveValue = value != null ? Convert.ToChar(value) : (Char?)null;
                            else
                                primitiveValue = Convert.ToChar(value);
                            break;
                    }

                }

                if (type.FullName == "System.Decimal")
                    if (isNullable)
                        primitiveValue = value != null ? Convert.ToDecimal(value) : (Decimal?)null;
                    else
                        primitiveValue = Convert.ToDecimal(value);

                if (targetType.FullName == "System.String")
                    primitiveValue = value;
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = cultureInfo;
            }
            return primitiveValue;
        }

        private string ExcludeExtraText(string input)
        {
            string output = input;
            if (input != null)
            {
                input = input.ToLower();
                for (int i = 0; i < _excludedText.Length; i++)
                {
                    if (input.IndexOf(_excludedText[i]) >= 0)
                    {
                        output = input.Replace(_excludedText[i], "");
                        break;
                    }
                }
            }
            return output;
        }
        private void FillConfigWithAttribValue(Object config, XmlAttribute xmlAttrib)
        {
            Type type = config.GetType();
            PropertyInfo[] objProps = type.GetProperties();

            if (objProps != null)
            {
                for (int i = 0; i < objProps.Length; i++)
                {
                    PropertyInfo propInfo = objProps[i];
                    Object[] customAttribs = propInfo.GetCustomAttributes(typeof(ConfigurationAttributeAttribute), false);
                    if (customAttribs != null && customAttribs.Length > 0)
                    {
                        ConfigurationAttributeAttribute configAttrib = customAttribs[0] as ConfigurationAttributeAttribute;
                        try
                        {
                            if (configAttrib != null && String.Compare(configAttrib.AttributeName, xmlAttrib.Name, true) == 0)
                            {
                                propInfo.SetValue(config, ConvertToPrimitive(propInfo.PropertyType, xmlAttrib.Value, configAttrib.AppendedText), null);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Can not set the value for attribute " + configAttrib.AttributeName + " Errror :" + e.ToString());
                        }

                    }
                }
            }
        }

        public string GetXmlString()
        {
            StringBuilder sb = new StringBuilder();
            if (_lastLoadedConfiugration != null)
            {
                foreach (object cfgObject in _lastLoadedConfiugration)
                {
                    sb.Append(GetXmlString(cfgObject));
                }
            }
            return sb.ToString();
        }

        public string GetXmlString(object cfgObject)
        {
            StringBuilder sb = new StringBuilder();
            string rootXmlStr = null;
            Type type = cfgObject.GetType();
            object[] cfgObjCustomAttribs = type.GetCustomAttributes(true);
            if (cfgObjCustomAttribs != null && cfgObjCustomAttribs.Length > 0)
            {
                for (int i = 0; i < cfgObjCustomAttribs.Length; i++)
                {
                    ConfigurationRootAttribute rootAttrib = cfgObjCustomAttribs[i] as ConfigurationRootAttribute;
                    if (rootAttrib != null)
                    {
                        rootXmlStr = rootAttrib.RootSectionName;

                    }
                }
            }
            return GetSectionXml(cfgObject, rootXmlStr, 1);

        }

        private string GetSectionXml(Object configSection, string sectionName, int indent)
        {
            string endStr = "\r\n";
            string preStr = "".PadRight(indent * 2);

            StringBuilder sb = new StringBuilder(preStr + "<" + sectionName);
            Type type = configSection.GetType();

            PropertyInfo[] propertiesInfo = type.GetProperties();

            if (propertiesInfo != null && propertiesInfo.Length > 0)
            {
                for (int i = 0; i < propertiesInfo.Length; i++)
                {
                    PropertyInfo property = propertiesInfo[i];
                    object[] customAttribs = property.GetCustomAttributes(true);

                    if (customAttribs != null && customAttribs.Length > 0)
                    {
                        for (int j = 0; j < customAttribs.Length; j++)
                        {
                            ConfigurationAttributeAttribute attrib = customAttribs[j] as ConfigurationAttributeAttribute;
                            if (attrib != null)
                            {
                                Object propertyValue = property.GetValue(configSection, null);
                                string appendedText = attrib.AppendedText != null ? attrib.AppendedText : "";
                                if (propertyValue != null)
                                {
                                    string encodedPropertyValue = null;
                                    if (sectionName.Equals("parameters", StringComparison.InvariantCultureIgnoreCase))
                                        encodedPropertyValue = System.Web.HttpUtility.HtmlEncode(propertyValue.ToString());
                                    else
                                        encodedPropertyValue = propertyValue.ToString();
                                        sb.Append(" " + attrib.AttributeName + "=\"" + encodedPropertyValue + appendedText + "\"");
                                }
                                else
                                {
                                    if (attrib.WriteIfNull)
                                        sb.Append(" " + attrib.AttributeName + "=\"\"");
                                    else
                                        sb.Append("");
                                }
                            }
                        }
                    }
                }
            }
            bool subsectionsFound = false;
            bool firstSubSection = true;
            StringBuilder comments = null;

            //get xml string for sub-sections if exists
            if (propertiesInfo != null && propertiesInfo.Length > 0)
            {
                for (int i = 0; i < propertiesInfo.Length; i++)
                {
                    PropertyInfo property = propertiesInfo[i];
                    object[] customAttribs = property.GetCustomAttributes(true);

                    if (customAttribs != null && customAttribs.Length > 0)
                    {
                        for (int j = 0; j < customAttribs.Length; j++)
                        {
                            ConfigurationCommentAttribute commentAttrib = customAttribs[j] as ConfigurationCommentAttribute;
                            if (commentAttrib != null)
                            {
                                Object propertyValue = property.GetValue(configSection, null);
                                if (propertyValue != null)
                                {
                                    string propStr = propertyValue as string;
                                    if (!string.IsNullOrEmpty(propStr))
                                    {
                                        if (comments == null)
                                        {
                                            comments = new StringBuilder();
                                        }
                                        comments.AppendFormat("{0}<!--{1}-->{2}", preStr, propStr, endStr);
                                    }
                                }
                            }

                            ConfigurationSectionAttribute attrib = customAttribs[j] as ConfigurationSectionAttribute;
                            if (attrib != null)
                            {
                                Object propertyValue = property.GetValue(configSection, null);
                                if (propertyValue != null)
                                {
                                    subsectionsFound = true;
                                    if (firstSubSection)
                                    {
                                        sb.Append(">" + endStr);
                                        firstSubSection = false;
                                    }
                                    if (propertyValue.GetType().IsArray)
                                    {
                                        Array array = propertyValue as Array;
                                        Object actualSectionObj;
                                        for (int k = 0; k < array.Length; k++)
                                        {
                                            actualSectionObj = array.GetValue(k);
                                            if (actualSectionObj != null)
                                            {
                                                sb.Append(GetSectionXml(actualSectionObj, attrib.SectionName, indent + 1));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        sb.Append(GetSectionXml(propertyValue, attrib.SectionName, indent + 1));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (subsectionsFound)
                sb.Append(preStr+ "</" + sectionName + ">" + endStr);
            else
                sb.Append("/>" + endStr);

            string xml = string.Empty;
            if (comments != null)
            {
                xml = comments.ToString() + sb.ToString();
            }
            else
            {
                xml = sb.ToString();
            }

            return xml;
        }
    }
}

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
using System.Xml.XPath;
using System.Collections;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Exceptions;



namespace Alachisoft.NCache.Config
{ 
	/// <summary>
	/// Utility class to help read properties from an XML configuration file and convert it into a HashMap of properties, which is later used by various classes for configurations.
	/// </summary>
	public class XmlConfigReader : ConfigReader
	{
        private string _cacheId = string.Empty;
		/// <summary> Path of the xml configuration file. </summary>
		private string			_configFileName;

		/// <summary> Cache-config section name to use. </summary>
		private string			_configSection;

		/// <summary> Characters to trim from end of values. </summary>
		private static readonly char[] _trimChars = new char[] { ' ',  '\t', '\r', '\n', '\'',  '"' };

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="configFileName">path of the xml configuration file.</param>
		/// <param name="configSection">cache-configuration section name to use.</param>
		public XmlConfigReader(string configFileName, string configSection)
		{
			_configFileName = configFileName;
			_configSection = configSection;
		}

		/// <summary>
		/// path of the xml configuration file.
		/// </summary>
		public string ConfigFileName
		{
			get { return _configFileName; }
		}

		/// <summary>
		/// cache-configuration section name to use.
		/// </summary>
		public string ConfigSection
		{
			get { return _configSection; }
		}

		/// <summary>
		/// returns the properties collection
		/// </summary>
		override public Hashtable Properties
		{
			get { return GetProperties(_configFileName, _configSection); }
		}

        public ArrayList PropertiesList
        {
            get { return GetAllProperties(_configFileName, _configSection); }
        }

		/// <summary>
		/// Returns the property string from the current properties xml.
		/// </summary>
		/// <returns>property-string.</returns>
		public string ToPropertiesString()
		{
			return ConfigReader.ToPropertiesString(Properties);
		}


		/// <summary>
		/// Returns the attributes of a node, if specified.
		/// </summary>
		/// <param name="navigator"></param>
		/// <returns></returns>
		protected Hashtable GetAttributesOfNode(XPathNavigator navigator)
		{
			Hashtable attributes = new Hashtable();
			if(!navigator.MoveToFirstAttribute())
			{
				return attributes;
			}
			do
			{
				attributes.Add(navigator.Name.ToLower(), navigator.Value);
			}
			while(navigator.MoveToNextAttribute());
			navigator.MoveToParent();
			return attributes;
		}

		/// <summary>
		/// Creates a hashtable out of an xml node. Elements are added recursively to the hashtable.
		/// </summary>
		/// <param name="properties"></param>
		/// <param name="navigator"></param>
		/// <returns></returns>
		protected virtual Hashtable BuildHashtable(Hashtable properties, XPathNavigator navigator)
		{
			do
			{
				string name = navigator.Name.ToLower();
				XPathNodeIterator childNavigator = navigator.SelectChildren(XPathNodeType.Element);
				Hashtable attributes = GetAttributesOfNode(childNavigator.Current);

				if(childNavigator.MoveNext())
				{
					Hashtable subproperties = BuildHashtable(new Hashtable(), childNavigator.Current);
					if(attributes.ContainsKey("id"))
					{
						string id = attributes["id"].ToString();
						subproperties.Add("id", id);
						subproperties.Add("type", name);
						name = id.ToLower();
					}
					properties[name] = subproperties;
				}
				else
				{
					if(navigator.NodeType == XPathNodeType.Element)
					{
						properties[name] = navigator.Value.Trim(_trimChars);
					}
				}
			}
			while(navigator.MoveToNext());
			navigator.MoveToParent();
			return properties;
		}

        /// <summary>
        /// Creates a hashtable out of an xml node. Elements are added recursively to the hashtable.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="navigator"></param>
        /// <returns></returns>
        protected virtual Hashtable BuildHashtable2(Hashtable properties, XPathNavigator navigator)
        {
            do
            {
                string name = navigator.Name.ToLower();
                XPathNodeIterator childNavigator = navigator.SelectChildren(XPathNodeType.Element);
                Hashtable attributes = GetAttributesOfNode(childNavigator.Current);
                
                Hashtable subprops = new Hashtable();
                IDictionaryEnumerator attribEnum = attributes.GetEnumerator();
                while (attribEnum.MoveNext())
                {
                    subprops.Add(attribEnum.Key, attribEnum.Value);
                }

                if (attributes.ContainsKey("id"))
                {
                    string id = attributes["id"].ToString();
                    subprops.Add("type", name);
                    
                    if (id != "internal-cache")
                    {
                        _cacheId = id;
                    }
                    name = id.ToLower();
                }

                if (childNavigator.MoveNext())
                {
                    Hashtable subproperties = BuildHashtable2(subprops, childNavigator.Current);
                    if (name.ToLower() == "cache")
                    {
                        subproperties["class"] = _cacheId;
                        subproperties["name"] = _cacheId;
                    }
                    if (name != string.Empty)
                        properties[name] = subproperties;
                }
                else
                {
                    if (name != string.Empty)
                        properties[name] = subprops;
                }
            }
            while (navigator.MoveToNext());
            navigator.MoveToParent();
            return properties;
        }

		/// <summary>
		/// Responsible for parsing the specified xml document and returning a 
		/// HashMap representation of the properties specified in it.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="configSection"></param>
		/// <returns></returns>
		internal Hashtable GetProperties(string fileName, string configSection)
		{
			Hashtable properties = new Hashtable();
			try
			{

                LoadConfig(fileName, ref properties);
                return (Hashtable)properties[configSection];
			}
			catch(Exception e)
			{
				throw new ConfigurationException("Error occurred while reading configuration", e);
			}
			throw new ConfigurationException(@"Specified config section '" + configSection + @"' not found in file '" + fileName + @"'. If it is a cache, it must be registered properly on this machine.");
		}

        private static CacheServerConfig[] LoadConfig(string fileName)
        {
            ConfigurationBuilder builder = new ConfigurationBuilder(fileName);
            builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
            builder.ReadConfiguration();
            Alachisoft.NCache.Config.NewDom.CacheServerConfig[] newCaches = new Alachisoft.NCache.Config.NewDom.CacheServerConfig[builder.Configuration.Length];
            builder.Configuration.CopyTo(newCaches, 0);

            return convertToOldDom(newCaches);
        }

        private static void LoadConfig(string fileName, ref Hashtable properties)
        {
            ConfigurationBuilder builder = new ConfigurationBuilder(fileName);
            builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
            builder.ReadConfiguration();
            Alachisoft.NCache.Config.NewDom.CacheServerConfig[] newCaches = new NewDom.CacheServerConfig[builder.Configuration.Length];
            builder.Configuration.CopyTo(newCaches, 0);
            properties = ConfigConverter.ToHashtable(convertToOldDom(newCaches));
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private static Alachisoft.NCache.Config.Dom.CacheServerConfig[] convertToOldDom(Alachisoft.NCache.Config.NewDom.CacheServerConfig[] newCacheConfigsList) throws Exception
        private static Alachisoft.NCache.Config.Dom.CacheServerConfig[] convertToOldDom(Alachisoft.NCache.Config.NewDom.CacheServerConfig[] newCacheConfigsList)
        {
            Alachisoft.NCache.Config.Dom.CacheServerConfig[] oldCacheConfigsList = new CacheServerConfig[newCacheConfigsList.Length];

            for (int index = 0; index < newCacheConfigsList.Length; index++)
            {
                try
                {
                    oldCacheConfigsList[index] = Alachisoft.NCache.Config.NewDom.DomHelper.convertToOldDom(newCacheConfigsList[index]);
                }
                catch (Exception ex) 
                {
                
                }
            }
            return oldCacheConfigsList;


        }

        public Hashtable GetProperties(string fileName, string configSection, string partId)
        {
            Hashtable properties = new Hashtable();
            try
            {

                LoadConfig(fileName, ref properties);
                return (Hashtable)properties[configSection];
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Error occurred while reading configuration", e);
            }
            throw new ConfigurationException(@"Specified config section '" + configSection + @"' not found in file '" + fileName + @"'. If it is a cache, it must be registered properly on this machine.");
        }

        public Hashtable GetProperties2(string fileName, string configSection, string partId)
        {
            Hashtable properties = new Hashtable();
            try
            {
                XPathDocument doc = new XPathDocument(fileName);
                XPathNavigator navigator = doc.CreateNavigator();

                XPathNodeIterator i = navigator.Select(@"/configuration/cache-config");
                while (i.MoveNext())
                {
                    Hashtable attributes = GetAttributesOfNode(i.Current);
                    
                    string name = Convert.ToString(attributes["name"]).ToLower();
                    if (name.CompareTo(configSection.ToLower()) != 0) continue;

                    if (i.Current.MoveToFirstChild())
                    {
                        Hashtable section = new Hashtable();

                        BuildHashtable2(section, i.Current);

                        section.Add("name", attributes["name"]);
                        section.Add("inproc", attributes["inproc"]);

                        properties.Add(attributes["name"].ToString().ToLower(), section);
                    }
                }
                return properties;
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Error occurred while reading configuration", e);
            }
        }

        /// <summary>
        /// Get CacheServerConfig for the specific cache
        /// </summary>
        /// <returns></returns>
        public CacheServerConfig GetConfigDom()
        {
            try
            {                
                CacheServerConfig[] configs = LoadConfig(this._configFileName);
                CacheServerConfig configDom = null;
                if (configs != null)
                {
                    foreach (CacheServerConfig config in configs)
                    {
                        if (config.Name != null && config.Name.Equals(this._configSection, StringComparison.OrdinalIgnoreCase))
                        {
                            configDom = config;
                            break;
                        }
                    }
                }

                if (configDom != null)
                {
                    return configDom;
                }
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Error occurred while reading configuration", e);
            }
            throw new ConfigurationException(@"Specified config section '" + this._configSection + @"' not found in file '" + this._configFileName + @"'. If it is a cache, it must be registered properly on this machine.");        
        }

        /// <summary>
        /// Responsible for parsing the specified xml document and returning a 
        /// HashMap representation of the properties specified in it.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="configSection"></param>
        /// <returns></returns>
        internal ArrayList GetAllProperties(string fileName, string configSection)
        {
            ArrayList propsList = new ArrayList();
            Hashtable properties = new Hashtable();
            
            try
            {

                LoadConfig(fileName, ref properties);
                if (properties.Contains(configSection.ToLower()))
                    propsList.Add(properties[configSection.ToLower()]);
                return propsList;
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Error occurred while reading configuration", e);
            }
            throw new ConfigurationException(@"Specified config section '" + configSection + @"' not found in file '" + fileName + @"'. If it is a cache, it must be registered properly on this machine.");
        }

        /// <summary>
        /// Responsible for parsing the specified xml document and returning a 
        /// HashMap representation of the properties specified in it.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="configSection"></param>
        /// <returns></returns>
        internal ArrayList GetAllProperties2(string fileName, string configSection)
        {
            ArrayList propsList = new ArrayList();
            Hashtable properties = new Hashtable();

            try
            {
                XPathDocument doc = new XPathDocument(fileName);
                XPathNavigator navigator = doc.CreateNavigator();

                XPathNodeIterator i = navigator.Select(@"/configuration/cache-config");
                while (i.MoveNext())
                {
                    Hashtable attributes = GetAttributesOfNode(i.Current);
                    if (attributes["name"] == null) continue;

                    string name = Convert.ToString(attributes["name"]).ToLower();
                    if (name.CompareTo(configSection.ToLower()) != 0) continue;

                    if (i.Current.MoveToFirstChild())
                    {
                        Hashtable section = new Hashtable();

                        BuildHashtable2(section, i.Current);

                        section.Add("name", attributes["name"]);
                        section.Add("inproc", attributes["inproc"]);

                        propsList.Add(section.Clone() as Hashtable);
                    }
                }
                return propsList;
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Error occurred while reading configuration", e);
            }
            throw new ConfigurationException(@"Specified config section '" + configSection + @"' not found in file '" + fileName + @"'. If it is a cache, it must be registered properly on this machine.");
        }

		/// <summary>
		/// Responsible for parsing the specified xml document and returning a 
		/// HashMap representation of the properties specified in it.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="configSection"></param>
		/// <returns></returns>
		public IDictionary GetProperties(string fileName)
		{
			Hashtable properties = new Hashtable();
			try
			{

                LoadConfig(fileName, ref properties);
                return properties;
			}
			catch(Exception e)
			{
				throw new ConfigurationException("Error occurred while reading configuration", e);
			}
		}

        /// <summary>
        /// Responsible for parsing the specified xml document and returning a 
        /// HashMap representation of the properties specified in it.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="configSection"></param>
        /// <returns></returns>
        public IDictionary GetProperties2(string fileName)
        {
            Hashtable properties = new Hashtable();
            try
            {
                XPathDocument doc = new XPathDocument(fileName);
                XPathNavigator navigator = doc.CreateNavigator();

                XPathNodeIterator i = navigator.Select(@"/configuration/cache-config");
                while (i.MoveNext())
                {
                    Hashtable attributes = GetAttributesOfNode(i.Current);
                    if (i.Current.MoveToFirstChild())
                    {
                        Hashtable section = new Hashtable();

                        BuildHashtable2(section, i.Current);

                        section.Add("name", attributes["name"]);
                        section.Add("inproc", attributes["inproc"]);

                        properties.Add(attributes["name"].ToString().ToLower(), section);
                    }
                }
                return properties;
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Error occurred while reading configuration", e);
            }
        }
	}
}

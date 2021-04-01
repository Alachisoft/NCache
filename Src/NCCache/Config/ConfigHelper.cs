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
using System.IO;
using System.Text;
using System.Collections;

using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config.NewDom;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using System.Collections.Generic;
using System.Net;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Config.Dom;



namespace Alachisoft.NCache.Config
{
    /// <summary>
	/// Deals in tasks specific to configuration.
	/// </summary>

	public class ConfigHelper
	{

        private const string DIRNAME = @"config";
        /// <summary>Configuration file name</summary>
        private const string FILENAME = @"config.ncconf";

        public static OnClusterConfigUpdate OnConfigUpdated;
        
        /// <summary>
		/// Returns name of Cache from the property string.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>cache name.</returns>
		static internal CacheInfo GetCacheInfo(string propstring)
		{
			PropsConfigReader pr = new PropsConfigReader(propstring);
			CacheInfo inf = GetCacheInfo(pr.Properties);
			inf.ConfigString = propstring;
			return inf;
		}

		/// <summary>
		/// Returns name of Cache from the property map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>cache name.</returns>
		static private CacheInfo GetCacheInfo(IDictionary properties)
		{

			if(!properties.Contains("cache"))
				throw new ConfigurationException("Missing configuration attribute 'cache'");

			CacheInfo inf = new CacheInfo();
			IDictionary cacheConfig = (IDictionary)properties[ "cache" ];

			string schemeName = "";
			if(cacheConfig.Contains("name"))
				inf.Name = Convert.ToString(cacheConfig["name"]).Trim();

			if(!cacheConfig.Contains("class"))
				throw new ConfigurationException("Missing configuration attribute 'class'");
				
			schemeName = Convert.ToString(cacheConfig[ "class" ]);
			if(inf.Name.Length < 1)
				inf.Name = schemeName;

			if(!cacheConfig.Contains("cache-classes"))
				throw new ConfigurationException("Missing configuration section 'cache-classes'");
			IDictionary cacheClasses = (IDictionary)cacheConfig[ "cache-classes" ];
			
			if(!cacheClasses.Contains(schemeName.ToLower()))
                throw new ConfigurationException("Cannot find cache class '" + schemeName + "'");
			IDictionary schemeProps = (IDictionary)cacheClasses[ schemeName.ToLower() ];

			if(!schemeProps.Contains("type"))
                throw new ConfigurationException("Cannot find the type of cache, invalid configuration for cache class '" + schemeName + "'");

			inf.ClassName = Convert.ToString(schemeProps["type"]);
			return inf;
		}

		/// <summary>
		/// Returns an xml config given a properties map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>xml config.</returns>
		static public string CreatePropertiesXml(IDictionary properties, int indent, bool format)
		{
			IDictionaryEnumerator it  = properties.GetEnumerator();
			StringBuilder returnStr = new StringBuilder(8096);
			StringBuilder nestedStr = new StringBuilder(8096);

			string preStr = format ? "".PadRight(indent * 2):"";
			string endStr = format ? "\n":"";
            
			while(it.MoveNext())
			{
				DictionaryEntry pair = (DictionaryEntry)it.Current;

				string keyName = pair.Key as String;
				string attributes = "";

				if (pair.Value is Hashtable)
				{
					Hashtable subproperties = (Hashtable) pair.Value;

                    if (keyName == "cluster" && OnConfigUpdated != null)
                    {
                        OnConfigUpdated(subproperties["group-id"].ToString(), pair);
                    }
					if(subproperties.Contains("type") && subproperties.Contains("id"))
					{
						keyName = (string) subproperties["type"];
						
						if (subproperties.Contains("partitionId"))
						{
							attributes = @" id='" + subproperties["id"] + "'" + @" partitionId='" + subproperties["partitionId"] + "'";
						}
						else
						{
							attributes = @" id='" + subproperties["id"] + "'";
						}

						subproperties = (Hashtable) subproperties.Clone();
						subproperties.Remove("id");
						subproperties.Remove("type");
					}
					nestedStr.Append(preStr).Append("<" + keyName + attributes + ">").Append(endStr);
					nestedStr.Append(CreatePropertiesXml(subproperties, indent + 1, format)).Append(preStr).Append("</" + keyName + ">").Append(endStr);
				}
				else
				{
					returnStr.Append(preStr).Append("<" + keyName + ">").Append(pair.Value).Append("</" + keyName + ">").Append(endStr);
				}
			}
			returnStr.Append(nestedStr.ToString());
            
            return returnStr.ToString();
		}


        static public string CreatePropertiesXml2(IDictionary properties, int indent, bool format)
        {
            IDictionaryEnumerator it = properties.GetEnumerator();
            StringBuilder returnStr = new StringBuilder(8096);
            StringBuilder nestedStr = new StringBuilder(8096);

            string preStr = format ? "".PadRight(indent * 2) : "";
            string endStr = format ? "\n" : "";

            while (it.MoveNext())
            {
                DictionaryEntry pair = (DictionaryEntry)it.Current;

                string keyName = pair.Key as String;
                string attributes = "";
                string cacheName = "";

                if (pair.Value is Hashtable)
                {
                    Hashtable subproperties = (Hashtable)pair.Value;

                  
                    if (subproperties.Contains("type") && subproperties.Contains("name"))
                    {
                        cacheName = subproperties["name"] as string;
                        keyName = (string)subproperties["type"];

                        subproperties = (Hashtable)subproperties.Clone();
                        subproperties.Remove("type");
                    }
                                        
                    nestedStr.Append(preStr).Append("<" + keyName + BuildAttributes(subproperties));
                    if (subproperties.Count == 0)
                    {
                        nestedStr.Append("/>").Append(endStr);
                    }
                    else
                    {
                        if (subproperties.Count == 1)
                        {
                            IDictionaryEnumerator ide = subproperties.GetEnumerator();
                            while (ide.MoveNext())
                            {
                                if (((string)ide.Key).ToLower().CompareTo(cacheName) == 0 && ide.Value is IDictionary)
                                {
                                    subproperties = ide.Value as Hashtable;
                                }
                            }
                        }
                        nestedStr.Append(">").Append(endStr);
                        nestedStr.Append(CreatePropertiesXml2(subproperties, indent + 1, format)).Append(preStr).Append("</" + keyName + ">").Append(endStr);
                    }
                }
            }
            returnStr.Append(nestedStr.ToString());

            return returnStr.ToString();
        }

        static private string BuildAttributes(Hashtable subProps)
        {
            StringBuilder attributes = new StringBuilder();
            string preString = " ";
           
            Hashtable tmp = subProps.Clone() as Hashtable;

            IDictionaryEnumerator ide = tmp.GetEnumerator();
            while (ide.MoveNext())
            {
                string key = ide.Key as string;
                if (!(ide.Value is Hashtable))
                {
                    attributes.Append(preString).Append(key).Append("=").Append("\"").Append(ide.Value).Append("\"");
                    subProps.Remove(key);
                }
            }
            return attributes.ToString();
        }

		/// <summary>
		/// Returns an xml config given a properties map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>xml config.</returns>
		static public string CreatePropertiesXml(IDictionary properties)
		{
			return CreatePropertiesXml(properties, 0, false);
		}

		static public string CreatePropertiesXml(IDictionary properties, string configid)
		{
			StringBuilder returnStr = new StringBuilder(@"<cache-configuration id='");
			returnStr.Append(configid).Append("'>");
			returnStr.Append(CreatePropertiesXml(properties, 0, false)).Append("</cache-configuration>");
			return returnStr.ToString();
		}

		/// <summary>
		/// Returns a property string given a properties map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>property string</returns>
		static public string CreatePropertyString(IDictionary properties)
		{
			return CreatePropertyString(properties, 0, false);
		}

		/// <summary>
		/// Returns a property string given a properties map.
		/// </summary>
		/// <param name="properties">properties map</param>
		/// <returns>property string</returns>
		static public string CreatePropertyString(IDictionary properties, int indent, bool format)
		{
			IDictionaryEnumerator it  = properties.GetEnumerator();
			StringBuilder returnStr = new StringBuilder(8096);
			StringBuilder nestedStr = new StringBuilder(8096);

			string preStr = format ? "".PadRight(indent * 2):"";
			string endStr = format ? "\n":"";

			while(it.MoveNext())
			{
				DictionaryEntry pair = (DictionaryEntry)it.Current;
				if (pair.Value is Hashtable)
				{
					Hashtable subproperties = (Hashtable) pair.Value;

                    if ((string)pair.Key == "cluster" && OnConfigUpdated != null)
                    {
                        OnConfigUpdated(subproperties["group-id"].ToString(), pair);
                    }

					if(subproperties.Contains("type") && subproperties.Contains("id"))
					{
						nestedStr.Append(preStr).Append(subproperties["id"]);
						nestedStr.Append("=").Append(subproperties["type"]).Append(endStr);

						subproperties = (Hashtable) subproperties.Clone();
						subproperties.Remove("id");
						subproperties.Remove("type");
					}
					else
						nestedStr.Append(preStr).Append(pair.Key.ToString()).Append(endStr);

					nestedStr.Append(preStr).Append("(").Append(endStr)
						.Append(CreatePropertyString(subproperties, indent + 1, format))
						.Append(preStr).Append(")").Append(endStr);
				}
				else
				{
					returnStr.Append(preStr).Append(pair.Key.ToString());
					if(pair.Value is string)
					{
						returnStr.Append(@"='").Append(pair.Value).Append(@"';").Append(endStr);
					}
					else
					{
						returnStr.Append("=").Append(pair.Value).Append(";").Append(endStr);
					}
				}
			}
			returnStr.Append(nestedStr.ToString());
			return returnStr.ToString();
		}

        /// <summary>
        /// Returns a property string given a properties map.
        /// </summary>
        /// <param name="properties">properties map</param>
        /// <returns>property string</returns>
        static public string CreatePropertyString2(IDictionary properties, int indent, bool format)
        {
            IDictionaryEnumerator it = properties.GetEnumerator();
            StringBuilder returnStr = new StringBuilder(8096);
            StringBuilder nestedStr = new StringBuilder(8096);

            string preStr = format ? "".PadRight(indent * 2) : "";
            string endStr = format ? "\n" : "";

            while (it.MoveNext())
            {
                DictionaryEntry pair = (DictionaryEntry)it.Current;
                if (pair.Value is Hashtable)
                {
                    Hashtable subproperties = (Hashtable)pair.Value;
                    if (subproperties.Contains("type") && subproperties.Contains("id"))
                    {
                        nestedStr.Append(preStr).Append(subproperties["id"]);
                        nestedStr.Append("=").Append(subproperties["type"]).Append(endStr);

                        subproperties = (Hashtable)subproperties.Clone();
                        subproperties.Remove("id");
                        subproperties.Remove("type");
                    }
                    else
                        nestedStr.Append(preStr).Append(pair.Key.ToString()).Append(endStr);

                    nestedStr.Append(preStr).Append("(").Append(endStr)
                        .Append(CreatePropertyString(subproperties, indent + 1, format))
                        .Append(preStr).Append(")").Append(endStr);
                }
                else
                {
                    returnStr.Append(preStr).Append(pair.Key.ToString());
                    if (pair.Value is string)
                    {
                        returnStr.Append(@"='").Append(pair.Value).Append(@"';").Append(endStr);
                    }
                    else
                    {
                        returnStr.Append("=").Append(pair.Value).Append(";").Append(endStr);
                    }
                }
            }
            returnStr.Append(nestedStr.ToString());
            return returnStr.ToString();
        }

		/// <summary>
		/// Finds and returns a cache scheme specified as attribute of other caches schemes.
		/// Handles the case of a ref to a scheme as well as inline cache definitions.
		/// the returned propmap contains the props for the intended scheme.
		/// </summary>
		/// <param name="cacheSchemes"></param>
		/// <param name="properties"></param>
		/// <param name="caheName"></param>
		/// <returns></returns>
		public static IDictionary GetCacheScheme(IDictionary cacheClasses, IDictionary properties, string cacheName)
		{
			IDictionary cacheProps = null;
			// check if a reference to some scheme is specified
			if(properties.Contains(cacheName + "-ref"))
			{
				string cacheScheme = Convert.ToString(properties[ cacheName + "-ref" ]).ToLower();
				if(!cacheClasses.Contains(cacheScheme))
                    throw new ConfigurationException("Cannot find cache class '" + cacheScheme + "'");
				// get the properties from the scheme
				cacheProps = (IDictionary)cacheClasses[ cacheScheme ];
			}
			else if(properties.Contains(cacheName))
			{
				// no reference specified, i.e., inline definition is specified.
				cacheProps = (IDictionary)properties[ cacheName ];
			}
			if((cacheProps == null) || !cacheProps.Contains("type"))
                throw new ConfigurationException("Cannot find the type of cache, invalid configuration for cache class");

			return cacheProps;
		}


		public static string SafeGet(IDictionary h, string key)
		{
			return SafeGet(h, key, null);
		}
		public static string SafeGet(IDictionary h, string key, object def)
		{
			object res = null;
			if(h != null)
			{
				res = h[key];
			}
			if(res == null) res = def;
			if(res == null) return String.Empty;
			return res.ToString();
		}

		public static string SafeGetPair(IDictionary h, string key, object def)
		{
			string res = SafeGet(h, key, def);
			if(res == "") 
				return res;

			StringBuilder b = new StringBuilder(64);
			b.Append(key).Append("=").Append(res.ToString()).Append(";");
			return b.ToString();
		}

#if SERVER 
		/// <summary>
		/// Builds and returns a property string understood by the lower layer, i.e., Cluster
		/// Uses the properties specified in the configuration, and defaults for others.
		/// </summary>
		/// <param name="properties">cluster properties</param>
		/// <returns>property string used by Cluster</returns>
		public static string GetClusterPropertyString(IDictionary properties,long opTimeout)
		{
			bool udpCluster = true;
			
			// check if a reference to some scheme is specified
			string cacheScheme = SafeGet(properties, "class").ToLower();
			if(cacheScheme == "tcp") udpCluster = false;

			if(!properties.Contains("channel"))
			{
				throw new ConfigurationException("Cannot find channel properties");
			}

			IDictionary channelprops = properties["channel"] as IDictionary;
			if(udpCluster)
			{
				return ChannelConfigBuilder.BuildUDPConfiguration(channelprops);
			}
			return ChannelConfigBuilder.BuildTCPConfiguration(channelprops,opTimeout);
		}

		public static string GetClusterPropertyString(IDictionary properties,long opTimeout,bool isPor)
		{
			bool udpCluster = true;

			// check if a reference to some scheme is specified

			string cacheScheme = SafeGet(properties, "class").ToLower();
			if (cacheScheme == "tcp") udpCluster = false;

			if (!properties.Contains("channel"))
			{
				throw new ConfigurationException("Cannot find channel properties");
			}

			IDictionary channelprops = properties["channel"] as IDictionary;
			if (udpCluster)
			{
				return ChannelConfigBuilder.BuildUDPConfiguration(channelprops);
			}
			return ChannelConfigBuilder.BuildTCPConfiguration(channelprops, opTimeout,isPor);
		}

        public static ISet<IPAddress> GetServerListFromConfig(string cacheId)
        {
            var serverList = new HashSet<IPAddress>();
            string path = Path.Combine(AppUtil.InstallDir, DIRNAME, FILENAME);
            try
            {
                var builder = new ConfigurationBuilder(path);
                builder.RegisterRootConfigurationObject(typeof(NewDom.CacheServerConfig));
                builder.ReadConfiguration();
                var newCaches = new NewDom.CacheServerConfig[builder.Configuration.Length];
                builder.Configuration.CopyTo(newCaches, 0);
                
                foreach (var cacheServerConfig in newCaches)
                {
                    if (cacheServerConfig.Name.Equals(cacheId,StringComparison.CurrentCultureIgnoreCase))
                    {
                        List<Address> list = cacheServerConfig.CacheDeployment.Servers.GetAllConfiguredNodes();
                        foreach(Address item in list)
                            serverList.Add(item.IpAddress);
                        break;
                    }                        
                }
                return serverList;
            }
            catch { return serverList; }
        }

       

#endif
    }
}

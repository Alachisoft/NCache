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

using Alachisoft.NCache.Config;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Config.Dom; 

namespace Alachisoft.NCache.Caching
{
	/// <summary>
	/// A cache factory is an object that creates named cache objects. It provides abstraction 
	/// by shielding various cache creation and initialization tasks.
	/// </summary>
	public class CacheFactory : MarshalByRefObject
	{
		/// <summary>
		/// Creates a cache object by reading in cofiguration parameters from a .NET XML file.
		/// </summary>
		/// <param name="configFileName">Name and/or path of the configuration file.</param>
		/// <param name="configSection">Name and/or ID of the section in the configuration file.</param>
		/// <returns>return the Cache object</returns>
		static public Cache CreateFromXmlConfig(string configFileName, string configSection)
		{
			ConfigReader xmlReader = new XmlConfigReader(configFileName, configSection);
			return CreateFromProperties(xmlReader.Properties, null,null);
		}

		/// <summary>
		/// Creates a cache object by reading in cofiguration parameters from a .NET XML file.
		/// </summary>
		/// <param name="configFileName">Name and/or path of the configuration file.</param>
		/// <param name="configSection">Name and/or ID of the section in the configuration file.</param>
		/// <param name="itemAdded">item added handler</param>
		/// <param name="itemRemoved">item removed handler</param>
		/// <param name="itemUpdated">item updated handler</param>
		/// <param name="cacheMiss">cache miss handler</param>
		/// <param name="cacheCleared">cache cleared handler</param>
		/// <returns>return the Cache object</returns>
		static public Cache CreateFromXmlConfig(string configFileName, 
											string configSection,
                                            CustomRemoveCallback customRemove,
                                            CustomUpdateCallback customUpdate)
		{
			ConfigReader xmlReader = new XmlConfigReader(configFileName, configSection);
			return CreateFromProperties(xmlReader.Properties,customRemove,customUpdate);
		}

	    /// <summary>
        /// This overload is used to call the Internal method that actually creates the cache.
        /// </summary>
        /// <param name="propertyString"></param>
        /// <returns></returns>
        static public Cache CreateFromPropertyString(string propertyString,CacheServerConfig config, bool isStartedAsMirror, bool twoPhaseInitialization)
        {
            ConfigReader propReader = new PropsConfigReader(propertyString);
            return CreateFromProperties(propReader.Properties,config, null, null, isStartedAsMirror,twoPhaseInitialization);
        }

		///// <summary>
		///// Creates a cache object by parsing configuration string passed as parameter.
		///// </summary>
		///// <param name="propertyString">property string provided by the user </param>
		///// <returns>return the Cache object</returns>
		static public Cache CreateFromPropertyString(string propertyString)
		{
			ConfigReader propReader = new PropsConfigReader(propertyString);
			return CreateFromProperties(propReader.Properties, null, null);
		}

		/// <summary>
		/// Creates a cache object by parsing configuration string passed as parameter.
		/// </summary>
		/// <param name="propertyString">property string provided by the user </param>
		/// <param name="itemAdded">item added handler</param>
		/// <param name="itemRemoved">item removed handler</param>
		/// <param name="itemUpdated">item updated handler</param>
		/// <param name="cacheMiss">cache miss handler</param>
		/// <param name="cacheCleared">cache cleared handler</param>
		/// <returns>return the Cache object</returns>
		static public Cache CreateFromPropertyString(string propertyString,
                                            CustomRemoveCallback customRemove,
                                            CustomUpdateCallback customUpdate)
		{
			ConfigReader propReader = new PropsConfigReader(propertyString);
			return CreateFromProperties(propReader.Properties, customRemove,customUpdate);
		}
     
		/// <summary>
		/// Internal method that actually creates the cache. A HashMap containing the config parameters 
		/// is passed to this method.
		/// </summary>
		/// <param name="propertyTable">contains the properties provided by the user in the for of Hashtable</param>
		/// <param name="itemAdded">item added handler</param>
		/// <param name="itemRemoved">item removed handler</param>
		/// <param name="itemUpdated">item updated handler</param>
		/// <param name="cacheMiss">cache miss handler</param>
		/// <param name="cacheCleared">cache cleared handler</param>
		/// <returns>return the Cache object</returns>
        static private Cache CreateFromProperties(IDictionary properties,
                                        CustomRemoveCallback customRemove,
                                        CustomUpdateCallback customUpdate)
        {
            return CreateFromProperties(properties,
                null,
                customRemove,
                customUpdate,
                false,
                false);
        }

		/// <summary>
		/// Internal method that actually creates the cache. A HashMap containing the config parameters 
		/// is passed to this method.
		/// </summary>
		/// <param name="propertyTable">contains the properties provided by the user in the for of Hashtable</param>
		/// <param name="itemAdded">item added handler</param>
		/// <param name="itemRemoved">item removed handler</param>
		/// <param name="itemUpdated">item updated handler</param>
		/// <param name="cacheMiss">cache miss handler</param>
		/// <param name="cacheCleared">cache cleared handler</param>
		/// <returns>return the Cache object</returns>
		static private Cache CreateFromProperties(IDictionary properties,
                                        CacheServerConfig config,
										CustomRemoveCallback customRemove,
										CustomUpdateCallback customUpdate,
                                        bool isStartingAsMirror,
                                        bool twoPhaseInitialization)
		{
			Cache cache = new Cache();
            cache.Configuration = config;
            if (customRemove != null)
                cache.CustomRemoveCallbackNotif += customRemove;
            if (customUpdate != null)
                cache.CustomUpdateCallbackNotif += customUpdate;

            cache.Initialize(properties, true, isStartingAsMirror,twoPhaseInitialization);
			return cache;
		}
	}
}

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
using Alachisoft.NCache.Config;
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
			return CreateFromProperties(xmlReader.Properties, null, null, null, null,null,null);
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
											ItemAddedCallback itemAdded,
											ItemRemovedCallback itemRemoved,
											ItemUpdatedCallback itemUpdated,
											CacheClearedCallback cacheCleared,
                                            CustomRemoveCallback customRemove,
                                            CustomUpdateCallback customUpdate)
		{
			ConfigReader xmlReader = new XmlConfigReader(configFileName, configSection);
			return CreateFromProperties(xmlReader.Properties, itemAdded, itemRemoved, itemUpdated, cacheCleared,customRemove,customUpdate);
		}

        /// <summary>
        /// This overload is used to pass on the security credentials of the user to the clustering layer
        /// to avoid the possibility of joining a cluster to non-authorized nodes.
        /// </summary>
        /// <param name="propertyString"></param>
        /// <returns></returns>
        static public Cache CreateFromPropertyString(string propertyString, string userId, string password)
        {
            return CreateFromPropertyString(propertyString, userId, password, false);
        }
        
        /// <summary>
		/// This overload is used to pass on the security credentials of the user to the clustering layer
		/// to avoid the possibility of joining a cluster to non-authorized nodes.
		/// </summary>
		/// <param name="propertyString"></param>
		/// <returns></returns>
		static public Cache CreateFromPropertyString(string propertyString, string userId, string password, bool isStartedAsMirror)
		{
			ConfigReader propReader = new PropsConfigReader(propertyString);
            //Make old style config-hashtable
            return CreateFromProperties(propReader.Properties, null, null, null, null, null, null, null, userId, password, isStartedAsMirror, false);
		}

        /// <summary>
        /// This overload is used to pass on the security credentials of the user to the clustering layer
        /// to avoid the possibility of joining a cluster to non-authorized nodes.
        /// </summary>
        /// <param name="propertyString"></param>
        /// <returns></returns>
        static public Cache CreateFromPropertyString(string propertyString,CacheServerConfig config, string userId, string password, bool isStartedAsMirror,bool twoPhaseInitialization)
        {
            ConfigReader propReader = new PropsConfigReader(propertyString);
            return CreateFromProperties(propReader.Properties,config, null, null, null, null, null, null, userId, password, isStartedAsMirror,twoPhaseInitialization);
        }

		///// <summary>
		///// Creates a cache object by parsing configuration string passed as parameter.
		///// </summary>
		///// <param name="propertyString">property string provided by the user </param>
		///// <returns>return the Cache object</returns>
		static public Cache CreateFromPropertyString(string propertyString)
		{
			ConfigReader propReader = new PropsConfigReader(propertyString);
			return CreateFromProperties(propReader.Properties, null, null, null, null, null, null);
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
											ItemAddedCallback itemAdded,
											ItemRemovedCallback itemRemoved,
											ItemUpdatedCallback itemUpdated,
											CacheClearedCallback cacheCleared,
                                            CustomRemoveCallback customRemove,
                                            CustomUpdateCallback customUpdate)
		{
			ConfigReader propReader = new PropsConfigReader(propertyString);
			return CreateFromProperties(propReader.Properties, itemAdded, itemRemoved, itemUpdated, cacheCleared,customRemove,customUpdate);
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
										ItemAddedCallback itemAdded,
										ItemRemovedCallback itemRemoved,
										ItemUpdatedCallback itemUpdated,
                                        CacheClearedCallback cacheCleared,
                                        CustomRemoveCallback customRemove,
                                        CustomUpdateCallback customUpdate)
		{
			Cache cache = new Cache();

			if(itemAdded != null)
				cache.ItemAdded += itemAdded;
			if(itemRemoved != null)
				cache.ItemRemoved += itemRemoved;
			if(itemUpdated != null)
				cache.ItemUpdated += itemUpdated;
			if(cacheCleared != null)
				cache.CacheCleared += cacheCleared;

            if (customRemove != null)
                cache.CustomRemoveCallbackNotif += customRemove;
            if (customUpdate != null)
                cache.CustomUpdateCallbackNotif += customUpdate;

			cache.Initialize(properties,true);
            return cache;
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
                                        ItemAddedCallback itemAdded,
                                        ItemRemovedCallback itemRemoved,
                                        ItemUpdatedCallback itemUpdated,
                                        CacheClearedCallback cacheCleared,
                                        CustomRemoveCallback customRemove,
                                        CustomUpdateCallback customUpdate,
                                        string userId,
                                        string password)
        {
            return CreateFromProperties(properties,
                null,
                itemAdded,
                itemRemoved,
                itemUpdated,
                cacheCleared,
                customRemove,
                customUpdate,
                userId,
                password,
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
										ItemAddedCallback itemAdded,
										ItemRemovedCallback itemRemoved,
										ItemUpdatedCallback itemUpdated,
										CacheClearedCallback cacheCleared,
										CustomRemoveCallback customRemove,
										CustomUpdateCallback customUpdate,
										string userId,
										string password,
                                        bool isStartingAsMirror,
                                        bool twoPhaseInitialization)
		{
			Cache cache = new Cache();
            cache.Configuration = config;

			if (itemAdded != null)
				cache.ItemAdded += itemAdded;
			if (itemRemoved != null)
				cache.ItemRemoved += itemRemoved;
			if (itemUpdated != null)
				cache.ItemUpdated += itemUpdated;
			if (cacheCleared != null)
				cache.CacheCleared += cacheCleared;
  
            if (customRemove != null)
                cache.CustomRemoveCallbackNotif += customRemove;
            if (customUpdate != null)
                cache.CustomUpdateCallbackNotif += customUpdate;

            cache.Initialize(properties, true, isStartingAsMirror,twoPhaseInitialization);
			return cache;
		}
	}
}

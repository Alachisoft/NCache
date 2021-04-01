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
using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Configuration;
using System.Xml;

namespace Alachisoft.NCache.Web.SessionStateManagement
{
    /// <summary>
    /// Reads page setting from NCache configuration section in condig file
    /// </summary>
    public class NCacheSessionStateConfigReader
    {
        private NCacheSessionStateConfigReader()
        { }

        private static NCacheSessionStateSettings _settings;

        public static NCacheSessionStateSettings LoadSessionLocationSettings()
        {
            if (_settings == null)
            {
                NCacheSection section = null;

                try
                {
                    section = new NCacheSection();
                }
                catch (Exception e)
                {

                    throw new System.Configuration.ConfigurationException("Error reading NCache Section. Exception: "+e.ToString());

                }

                XmlNode ncacheSection = section.NCacheConfigSection;

                if (ncacheSection != null)
                {
                    NCacheSessionStateSettings sessionStateSettings = new NCacheSessionStateSettings();
                    XmlNode sessionLocation = ncacheSection.SelectSingleNode("sessionLocation");

                    //muds:
                    if (sessionLocation.Attributes["secondary-connection-recycle-interval"] != null)
                    {
                        sessionStateSettings.RecycleInterval = Convert.ToInt32(sessionLocation.Attributes["secondary-connection-recycle-interval"].Value);
                    }

                    if (sessionLocation != null)
                    {
                        XmlNode primaryCache = sessionLocation.SelectSingleNode("primaryCache");
                        if (primaryCache.Attributes["id"] != null)
                        {
                            string cacheId = primaryCache.Attributes["id"].Value.ToLower();

                            string sidPrefix = primaryCache.Attributes["sid-prefix"].Value.ToLower();
                            if (sidPrefix == null || sidPrefix.Length != 4)
                                throw new System.Configuration.ConfigurationException("Invalid sid-prefix value specified for \"" + cacheId + "\".");
                            sessionStateSettings.PrimaryCache.Add(sidPrefix, cacheId);
                        }
                        else
                        {
                            throw new System.Configuration.ConfigurationException("Missing attribute 'id' in primaryCache section.");
                        }

                        XmlNodeList nodeList = sessionLocation.SelectNodes("secondaryCache");
                        foreach (XmlNode secondaryCache in nodeList)
                        {
                            if (secondaryCache.Attributes["id"] != null)
                            {
                                string cacheId = secondaryCache.Attributes["id"].Value.ToLower();
                                string sidPrefix = secondaryCache.Attributes["sid-prefix"].Value.ToLower();
                                if (sidPrefix == null || sidPrefix.Length != 4)
                                    throw new System.Configuration.ConfigurationException("Invalid sid-prefix value specified for \"" + cacheId + "\".");
                                if (!sessionStateSettings.SecondaryCaches.Contains(sidPrefix))
                                {
                                    sessionStateSettings.SecondaryCaches.Add(sidPrefix, cacheId);
                                }
                            }
                            else
                            {
                                throw new System.Configuration.ConfigurationException("Missing attribute 'id' in SecondaryCache section.");
                            }
                        }
                    }
                    _settings = sessionStateSettings;
                }
            }
            return _settings;
        }
    }
}
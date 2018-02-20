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
// limitations under the License

using System;
using System.Collections;
using System.Configuration;
using System.Xml;

namespace Alachisoft.NCache.Web.NOutputCache
{
    /// <summary>
    /// Reads page setting from NCache configuration section in condig file
    /// </summary>
    internal class OutPutCacheConfigReader
    {
        private Hashtable _pageProps;

        /// <summary>
        /// Initializes instance of reader
        /// </summary>
        public OutPutCacheConfigReader()
        {
            this._pageProps = new Hashtable();
        }

        /// <summary>
        /// Get page settings
        /// </summary>
        /// <param name="name">Name of page</param>
        /// <returns>Page settings</returns>
        public PageSettings GetPageSettings(string name)
        {
            PageSettings pageSettings = this._pageProps[name.ToLower()] as PageSettings;
            return pageSettings;
        }

        public OutputCacheSettings LoadCacheSettings()
        {
            NCacheSection section = new NCacheSection();
            XmlNode ncacheSection = section.NCacheConfigSection;

            OutputCacheSettings outputCacheSettings = new OutputCacheSettings();

            if (ncacheSection != null)
            {
                XmlNode outputCache = ncacheSection.SelectSingleNode("outputCacheSettings");
                if (outputCache != null)
                {
                    if (outputCache.Attributes["cacheName"] != null && outputCache.Attributes["cacheName"].Value != string.Empty)
                    {
                        outputCacheSettings.CacheName = outputCache.Attributes["cacheName"].Value.ToLower();
                    }
                    else
                    {
                        throw new ConfigurationException("The 'cacheName' attribute in outputCacheSettings section cannot be null or empty string");
                    }

                    string[] boolValStrings = {"enableClientCacheSync", "exceptionsEnabled",
                        "enableLogs", "enableDetailLogs"};
                    string configVal = null;
                    bool value = false;

                    for (int i = 0; i < boolValStrings.Length; i++)
                    {
                        if (outputCache.Attributes[boolValStrings[i]] != null)
                        {
                            configVal = outputCache.Attributes[boolValStrings[i]].Value;
                            if (configVal != "true" && configVal != "false")
                            {
                                throw new ConfigurationException("The '" + boolValStrings[i] + "' attribute must be one of the following values: true, false.");
                            }
                            else
                            {
                                value = Convert.ToBoolean(configVal);
                                switch (i)
                                {
 
                                    case 0: outputCacheSettings.ExceptionsEnabled = value; break;
                                    case 1: outputCacheSettings.EnableLogs = value; break;
                                    case 2: outputCacheSettings.EnableDetailedLogs = value; break;
                                }
                            }

                        }
                    }
                }
            }

            return outputCacheSettings;
        }

        /// <summary>
        /// Loads page settings from config
        /// </summary>
        public void LoadPageSettings()
        {
            NCacheSection section = new NCacheSection();
            XmlNode ncacheSection = section.NCacheConfigSection;

            if (ncacheSection != null)
            {
                XmlNode settings = ncacheSection.SelectSingleNode("outputCacheSettings");
                if (settings != null)
                {
                    XmlNode outputCachePages = settings.SelectSingleNode("outputCachePages");
                    if (outputCachePages != null)
                    {
                        XmlNodeList pageList = outputCachePages.SelectNodes("add");

                        foreach (XmlNode node in pageList)
                        {
                            PageSettings pageSettings = new PageSettings();
                            if (node.Attributes["name"] != null && node.Attributes["name"].Value != string.Empty)
                            {
                                pageSettings.PageName = node.Attributes["name"].Value.ToLower();
                            }
                            else
                            {
                                throw new ConfigurationException("The 'name' attribute in outputCachePages section cannot be null or empty string");
                            }

                            if (node.Attributes["duration"] != null)
                            {
                                try
                                {
                                    pageSettings.ExpirationTime = Convert.ToInt32(node.Attributes["duration"].Value);
                                }
                                catch (Exception exc)
                                {
                                    throw new ConfigurationException("The value of property 'duration' cannot be parsed. The error is: " + exc.Message);
                                }
                            }
                            else
                            {
                                throw new ConfigurationException("The 'duration' attribute in outputCachePages section cannot be null");
                            }

                            if (node.Attributes["enabled"] != null)
                            {
                                if (node.Attributes["enabled"].Value != "true" && node.Attributes["enabled"].Value != "false")
                                {
                                    throw new ConfigurationException("The 'enabled' attribute in outputCachePages section must be one of the following values: true, false.");
                                }
                                else
                                {
                                    pageSettings.CachingEnabled = Convert.ToBoolean(node.Attributes["enabled"].Value);
                                }
                            }

                            if (node.Attributes["requestType"] != null)
                            {
                                string requestType = node.Attributes["requestType"].Value.ToLower();
                                if (!NOutputCache.IsNullOrEmpty(requestType))
                                {
                                    string[] split = requestType.Split('/');

                                    for (int i = 0; i < split.Length; i++)
                                    {
                                        switch (split[i].Trim())
                                        {
                                            case "get":
                                                pageSettings.Get = true;
                                                break;
                                            case "post":
                                                pageSettings.Post = true;
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    pageSettings.Get = true;///by default
                                }
                            }

                            if (node.Attributes["varyByParam"] != null)
                            {
                                pageSettings.ParseVaryByParams(node.Attributes["varyByParam"].Value);
                            }

                            if (node.Attributes["varyByHeader"] != null)
                            {
                                pageSettings.ParseVaryByHeaders(node.Attributes["varyByHeader"].Value);
                            }

                            if (node.Attributes["varyByCustom"] != null)
                            {
                                pageSettings.VaryByCustom = node.Attributes["varyByCustom"].Value.ToLower();
                            }

                            this._pageProps[pageSettings.PageName] = pageSettings;
                        }
                    }
                }
            }
        }
    }
}
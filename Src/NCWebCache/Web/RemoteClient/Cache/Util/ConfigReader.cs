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

using System;
using System.IO;
using System.Xml;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Globalization;
using System.Threading;

namespace Alachisoft.NCache.Web.Caching.Util
{
    internal sealed class ConfigReader
    {
        internal string fileName = null;


        /// <summary>
        /// Reads client configuration file for value of specified item. The function
        /// only reads boolean items, false will be returned otherwise.
        /// </summary>
        /// <param name="fileName">client configuration file name</param>
        /// <param name="cacheId">cache id</param>
        /// <param name="name">item name</param>
        /// <returns>boolean flag specifying items value</returns>
        internal static bool ReadItemValue(string fileName, string cacheId, string itemName)
        {
            FileStream fs = null;

            try
            {
                string filePath = GetClientConfigurationPath();

                if (filePath == null) return false;
                if (!File.Exists(filePath)) return false;

                XmlDocument configuration = new XmlDocument();

                FileInfo fileInfo = new FileInfo(filePath);
                fs = fileInfo.OpenRead();
                configuration.Load(fs);
                fileInfo = null;

                XmlNodeList cacheList = configuration.GetElementsByTagName("cache");
                XmlNodeList cacheConfig = null;
                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                try
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture =
                        new System.Globalization.CultureInfo("en-US");
                    for (int i = 0; i < cacheList.Count; i++)
                    {
                        XmlNode cache = cacheList.Item(i);
                        if (cache.Attributes.GetNamedItem("id").Value.ToLower().Equals(cacheId.ToLower()))
                        {
                            if (cache.Attributes.GetNamedItem(itemName) != null)
                            {
                                string itemValue = cache.Attributes[itemName].InnerText;
                                return itemValue == "true";
                            }
                        }
                    }
                }
                finally
                {
                    System.Threading.Thread.CurrentThread.CurrentCulture = cultureInfo;
                }
            }

            catch (Runtime.Exceptions.ConfigurationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ConfigurationException("An error occurred while reading client.ncconf. " + e.Message);
            }

            finally
            {
                if (fs != null) fs.Close();
            }

            return false;
        }


        /// <summary>
        /// Reads client configuration file for value of specified item. The function
        /// only reads boolean items, false will be returned otherwise.
        /// </summary>
        /// <param name="fileName">client configuration file name</param>
        /// <param name="cacheId">cache id</param>
        /// <param name="name">item name</param>
        /// <returns>boolean flag specifying items value</returns>
        internal static string ReadItem(string fileName, string cacheId, string itemName)
        {
            FileStream fs = null;

            try
            {
                string filePath = GetClientConfigurationPath();

                if (filePath == null) return string.Empty;
                if (!File.Exists(filePath)) return string.Empty;

                XmlDocument configuration = new XmlDocument();

                FileInfo fileInfo = new FileInfo(filePath);
                fs = fileInfo.OpenRead();
                configuration.Load(fs);
                fileInfo = null;

                XmlNodeList cacheList = configuration.GetElementsByTagName("cache");
                XmlNodeList cacheConfig = null;

                for (int i = 0; i < cacheList.Count; i++)
                {
                    XmlNode cache = cacheList.Item(i);
                    if (cache.Attributes.GetNamedItem("id").Value.ToLower().Equals(cacheId.ToLower()))
                    {
                        if (cache.Attributes.GetNamedItem(itemName) != null)
                        {
                            string itemValue = cache.Attributes[itemName].InnerText;
                            return itemValue;
                        }
                    }
                }
            }

            catch (Runtime.Exceptions.ConfigurationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ConfigurationException("An error occurred while reading client.ncconf. " + e.Message);
            }

            finally
            {
                if (fs != null) fs.Close();
            }

            return string.Empty;
        }

        /// <summary>
        /// see if the client.ncconf is present in the executing assembly local folder. if yes 
        /// then returns the local path. otherwise search for the client.ncconf in NCache install folder 
        /// and if found returns the file global path.
        /// </summary>
        /// <returns>returns the client.ncconf local or global path if found. otherwise returns null.</returns>
        internal static string GetClientConfigurationPath()
        {
            return DirectoryUtil.GetBaseFilePath("client.ncconf");
        }
    }
}
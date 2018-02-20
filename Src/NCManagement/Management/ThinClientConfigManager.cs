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
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Globalization;
using System.Threading;

namespace Alachisoft.NCache.Management
{
    public class ThinClientConfigManager : CacheConfigManager
    {
        public new const int DEF_TCP_PORT = 8250;
       
        /// <summary>
        /// Initialize a registered cache given by the ID.
        /// </summary>
        /// <param name="cacheId">A string identifier of configuration.</param>
        static public CacheServerConfig GetConfigDom(string cacheId, string filePath, bool inProc)
        {
            try
            {
                XmlConfigReader configReader = new XmlConfigReader(filePath, cacheId);
                CacheServerConfig config = configReader.GetConfigDom();
                CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                if (config == null)
                {
                    return config;
                }

                if (!inProc)
                {
                    inProc = config.InProc;
                }

                if (inProc)
                {
                     return config;
                }
                return null;
            }

            

            catch (ManagementException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }


        /// <summary>
        /// Initialize a registered cache given by the ID.
        /// </summary>
        /// <param name="cacheId">A string identifier of configuration.</param>
        static public ArrayList GetCacheConfig(string cacheId, string filePath,  bool inProc)
        {
            try
            {
                XmlConfigReader configReader = new XmlConfigReader(filePath, cacheId);
                ArrayList propsList = configReader.PropertiesList;
                ArrayList configsList = CacheConfig.GetConfigs(propsList, DEF_TCP_PORT);

                foreach (CacheConfig config in configsList)
                {
                    if (!inProc) inProc = config.UseInProc;
                    break;
                }

                if (inProc)
                {

                    bool isAuthorize = false;
                    Hashtable ht = (Hashtable)propsList[0];
                    Hashtable cache = (Hashtable)ht["cache"];

        

                    return configsList;
                }
                return null;
            }


            catch (ManagementException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }     
    }
}
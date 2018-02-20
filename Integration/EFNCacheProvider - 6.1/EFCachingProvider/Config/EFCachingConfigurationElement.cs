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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alachisoft.NCache.Common.Configuration;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    /// <summary>
    /// Represent "configuration" element in efcaching config
    /// </summary>
    [ConfigurationRoot("configuration")]
    public sealed class EFCachingConfigurationElement : ICloneable
    {
        /// <summary>
        /// Get or set 'app-config' element from configuration element
        /// </summary>
        [ConfigurationSection("app-config")]
        public ApplicationConfigurationElement[] AppConfig { get; set; }

        /// <summary>
        /// Get application configuration for a specific application
        /// </summary>
        /// <param name="appName">Application id</param>
        /// <returns>Application configuration setting for specific application</returns>
        public ApplicationConfigurationElement GetAppConfig(string appId)
        {
            if (!appId.IsNullOrEmpty() && this.AppConfig != null)
            {
                return this.AppConfig.FirstOrDefault<ApplicationConfigurationElement>(
                    new Func<ApplicationConfigurationElement, bool>(
                        conf =>
                        {
                            if (conf.Id == null) return false;
                            return conf.Id.Equals(appId, StringComparison.OrdinalIgnoreCase);
                        }));
            }
            return null;
        }

        #region ICloneable Members

        public object Clone()
        {
            return new EFCachingConfigurationElement()
            {
                AppConfig = this.AppConfig.DeepClone() as ApplicationConfigurationElement[]
            };
            //EFCachingConfigurationElement clone = new EFCachingConfigurationElement();
            //if (this.AppConfig != null)
            //{
            //    clone.AppConfig = new ApplicationConfigurationElement[this.AppConfig.Length];

            //    for (int i = 0; i < this.AppConfig.Length; i++)
            //    {
            //        clone.AppConfig[i] = this.AppConfig[i].Clone() as ApplicationConfigurationElement;
            //    }
            //}
            //return clone;
        }

        #endregion
    }
}

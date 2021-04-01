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
using System.Configuration;
using System.Xml;
namespace Alachisoft.NCache.Web.SessionStateManagement
{
    /// <summary>
    /// Provides Configuration system support for the NCache configuration section
    /// </summary>
    /// <remarks>
    ///		<para>Section name must be 'ncache'</para>
    /// </remarks>
    internal class NCacheSection : IConfigurationSectionHandler
    {
        /// <summary>Confiuration section name</summary>
         

        private const string SectionName = "ncache";
        private static XmlNode _ncsection = null;

        /// <summary>
        /// Initializes the <see cref="CacheRegionElement"/> class.
        /// </summary>
        static NCacheSection()
        {
            GetSection();
        }

        /// <summary>
        /// Get NCache confiuration section
        /// </summary>
        public XmlNode NCacheConfigSection
        {
            get { return _ncsection; }
        }
        /// <summary>
        /// Gets the <see cref="NCacheSection"/> from the configuration
        /// </summary>
        /// <returns>The configured <see cref="NCacheSection"/></returns>
        public static NCacheSection GetSection()
        {
            return ConfigurationSettings.GetConfig(SectionName) as NCacheSection;
        }

        #region IConfigurationSectionHandler Members

        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            _ncsection = section;
            return section;
        }

        #endregion
    }
}
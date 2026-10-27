//  Copyright (c) 2026 Alachisoft
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
using System.Xml.Serialization;

namespace Alachisoft.NCache.Licensing.DOM
{
    /// <summary>
    /// The &lt;product-info&gt; element of ncinfo.xml (Linux) and the matching registry values (Windows):
    /// where and how NCache is installed. The activation fields this element used to carry (install
    /// code, activation id, edition, evaluation violation) are gone with licensing; XmlSerializer skips
    /// those elements when an older ncinfo.xml still has them.
    /// </summary>
    public class Product
    {
        [XmlAttribute("dotnet-install-mode")]
        public string DotNetInstallMode { get; set; }

        [XmlAttribute("http-port")]
        public string HttpPort { get; set; }

        [XmlElement("install-dir")]
        public string InstallDir { get; set; }

        [XmlElement("platform")]
        public string Platform { get; set; }

        [XmlElement("framework")]
        public string Framework { get; set; }

        [XmlElement("sp-version")]
        public string SPVersion { get; set; }

        [XmlElement("tcp-port")]
        public string TcpPort { get; set; }

        [XmlElement("name")]
        public string ProductName { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("install-type")]
        public string InstallType { get; set; }

        [XmlElement("script-directory")]
        public string ScriptDirectory { get; set; }
    }
}

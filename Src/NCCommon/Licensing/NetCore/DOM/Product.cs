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
using Alachisoft.NCache.Common.XmlSerialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Alachisoft.NCache.Licensing.NetCore.DOM
{
    public class Product
    {
        [XmlAttribute("dotnet-install-mode")]
        public string DotNetInstallMode { get; set; }

        [XmlAttribute("http-port")]
        public string HttpPort { get; set; }

        [XmlElement("install-code")]
        public string InstallCode { get; set; }

        [XmlElement("install-dir")]
        public string InstallDir { get; set; }

        [XmlElement("last-report-time")]
        public string LastReportTime { get; set; }

        [XmlElement("platform")]
        public string Platform { get; set; }

        [XmlElement("sp-version")]
        public string SPVersion { get; set; }

        [XmlElement("tcp-port")]
        public string TcpPort { get; set; }

        [XmlElement("act-voil")]
        public string ActVoil { get; set; }
     
        //Not in the Registry Editor Window
        [XmlIgnore]
        public string ProductName { get; set; }

        [XmlIgnore]
        public string ActivationId { get; set; }

        [XmlIgnore]
        public string Edition { get; set; }

        [XmlIgnore]
        public string Version { get; set; }

        [XmlIgnore]
        public int VersionId { get; set; }

        [XmlIgnore]
        public bool Reactivation { get; set; }

        [XmlIgnore]
        public string PrevCoreCount { get; set; }

        [XmlIgnore]
        public string PrevPhysicalCPUS { get; set; }

        [XmlIgnore]
        public bool AutoRenewal { get; set; }

        [XmlIgnore]
        public string LatestVersion { get; set; }

        [XmlIgnore]
        public string MinorVersion { get; set; }

        //public int Load()
        //{
        //    var licenseProperties = XMLManager.ReadConfiguration<LicenseProperties>(RegistryUtil.RegUtil.GetRegPath());
        //    if (licenseProperties.Product == null)
        //        return - 1;

        //    ProductName = licenseProperties.Product.ProductName;
        //    return 0;
        //}

        public string ToLinearText(string licenseKey, string deactivateCode)
        {
            string reactivationString = Reactivation ? "true" : "false";
            string autoRenewalString = AutoRenewal ? "true" : "false";
            string text = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                ProductName, Edition,
                Version, licenseKey,
                ActivationId, deactivateCode, reactivationString, LatestVersion, autoRenewalString, MinorVersion);

            return text;
        }

        public string ToXml(string licenseKey, string deactivateCode)
        {
            string xml = "<product-info>";

            xml += "<name>";
            xml += ProductName;
            xml += "</name>";

            xml += "<edition>";
            xml += Edition;
            xml += "</edition>";

            xml += "<version>";
            xml += Version;
            xml += "</version>";

            xml += "<license-key>";
            xml += licenseKey;
            xml += "</license-key>";

            xml += "<act-id>";
            xml += ActivationId;
            xml += "</act-id>";

            xml += "<deactivate-code>";
            xml += deactivateCode;
            xml += "</deactivate-code>";

            xml += "<reactivation>";
            xml += Reactivation ? "true" : "false";
            xml += "</reactivation>";

            xml += "</product-info>";
            return xml;
        }
    }
}

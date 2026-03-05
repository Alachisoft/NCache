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
using Alachisoft.NCache.Common.XmlSerialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Alachisoft.NCache.Licensing.DOM
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

        [XmlElement("framework")]
        public string Framework { get; set; }
        [XmlElement("sp-version")]
        public string SPVersion { get; set; }

        [XmlElement("tcp-port")]
        public string TcpPort { get; set; }

        [XmlElement("act-voil")]
        public string ActVoil { get; set; }
        [XmlElement("voil-date")]
        public string VoilDate { get; set; }

        //Not in the Registry Editor Window
        [XmlElement("name")]
        public string ProductName { get; set; }

        [XmlElement("act-id")]
        public string ActivationId { get; set; }

        [XmlElement("edition")]
        public string Edition { get; set; }

        [XmlElement("version")]
        public string Version { get; set; }

        [XmlIgnore]
        public int VersionId { get; set; }

        [XmlIgnore]
        public bool Reactivation { get; set; }

        [XmlIgnore]
        public string LicenseDuration { get; set; }

        [XmlIgnore]
        public string PrevLicenses { get; set; }

        [XmlIgnore]
        public string PrevLogicalCores { get; set; }

        [XmlIgnore]
        public bool AutoRenewal { get; set; }

        [XmlIgnore]
        public string LatestVersion { get; set; }

        [XmlIgnore]
        public string MinorVersion { get; set; }

        [XmlIgnore]
        public string PrevPhysicalCores { get; set; }

        [XmlIgnore]
        public bool EvalExtension { get; set; }

        [XmlIgnore]
        public bool Recurring { get; set; }

        [XmlElement("install-type")]
        public string InstallType { get; set; }

        [XmlElement("script-directory")]
        public string ScriptDirectory { get; set; }

        public string ToLinearText(string licenseKey, string deactivateCode)
        {
            string reactivationString = Reactivation ? "true" : "false";
            string autoRenewalString = AutoRenewal ? "true" : "false";
            string recurringString = Recurring ? "true" : "false";

            string text = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}",
                ProductName, Edition,
                Version, licenseKey,
                ActivationId, deactivateCode, reactivationString, LatestVersion, autoRenewalString, MinorVersion, SPVersion, EvalExtension,
                Common.Util.InstallTypeUtil.GetInstallType(InstallType),//12
                LicenseDuration, //13 
                recurringString //14 
                );


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

            xml += "</product-info>";
            return xml;
        }
    }
}

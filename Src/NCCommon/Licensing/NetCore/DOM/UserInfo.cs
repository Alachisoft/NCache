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
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Alachisoft.NCache.Licensing.NetCore.DOM
{
    public class UserInfo
    {
        private string _authCode = string.Empty;
        
        [XmlElement("address")]
        public string Address { get; set; }

        //        [XmlElement("auth-code")]
        [XmlIgnore]
        public string AuthCode { get { return _authCode; } set { _authCode = value ?? string.Empty; } }

        [XmlElement("city")]
        public string City { get; set; }

        [XmlElement("company")]
        public string Company { get; set; }

        [XmlElement("country")]
        public string Country { get; set; }

        [XmlElement("deactivate-code")]
        public string DeactivateCode { get; set; }

        [XmlElement("email")]
        public string Email { get; set; }

        [XmlElement("ext-code")]
        public string ExtCode { get; set; }

        [XmlElement("first-name")]
        public string FirstName { get; set; }

        [XmlElement("last-name")]
        public string LastName { get; set; }

        // [XmlElement("license-key")]
        [XmlIgnore]
        public string LicenseKey { get; set; }

        [XmlElement("phone")]
        public string Phone { get; set; }

        [XmlElement("state")]
        public string State { get; set; }

        [XmlElement("zip")]
        public string Zip { get; set; }

        public string ToLinearText()
        {
            string text = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                FirstName, LastName,
                Email, Company,
                Address, City,
                State, Zip,
                Country, Phone);
            return text;
        }

        public string ToXml()
        {
            string xml = "<user-info>";

            xml += "<first-name>";
            xml += FirstName;
            xml += "</first-name>";

            xml += "<last-name>";
            xml += LastName;
            xml += "</last-name>";

            xml += "<email>";
            xml += Email;
            xml += "</email>";

            xml += "<company>";
            xml += Company;
            xml += "</company>";

            xml += "<address>";
            xml += Address;
            xml += "</address>";

            xml += "<city>";
            xml += City;
            xml += "</city>";

            xml += "<state>";
            xml += State;
            xml += "</state>";

            xml += "<zip>";
            xml += Zip;
            xml += "</zip>";

            xml += "<country>";
            xml += Country;
            xml += "</country>";

            xml += "<phone>";
            xml += Phone;
            xml += "</phone>";

            xml += "</user-info>";
            return xml;
        }
    }
}

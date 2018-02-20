using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Alachisoft.NCache.Common.Registry.NetCore.DOM
{
    [XmlRoot("license-config")]
    public class LicenseProperties
    {
        [XmlElement("product-info")]
        public Product Product { get; set; }

        [XmlElement("user-info")]
        public UserInfo UserInfo { get; set; }
    }
}

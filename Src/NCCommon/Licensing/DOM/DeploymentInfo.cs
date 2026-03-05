using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace Alachisoft.NCache.Licensing.DOM
{
    public class DeploymentInfo
    {
        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("client-cpus")]
        public int ClientCpus { get; set; }

        private string GetLocalIP()
        {
            try
            {
                var server = Dns.GetHostEntry(Dns.GetHostName());
                var address = server.AddressList.ToList().Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault().ToString();
                if (!string.IsNullOrEmpty(address))
                    return address;
            }
            catch (Exception ex)
            {

            }
            return IPAddress.Loopback.ToString();
        }

        public string ToLinearText()
        {
            string text = string.Format("{0}\t{1}\t{2}",
                Name, ClientCpus, GetLocalIP()
                );
            return text;
        }

        public string ToXml()
        {
            string xml = "<deployemnt>";

            xml += "<name>";
            xml += this.Name;
            xml += "</name>";

            xml += "<client-cpus>";
            xml += this.ClientCpus;
            xml += "</client-cpus>";

            xml += "<local-Ip>";
            xml += GetLocalIP();
            xml += "</local-Ip>";

            xml += "</deployemnt>";
            return xml;
        }

        private static void Load()
        {

        }
    }

    
}

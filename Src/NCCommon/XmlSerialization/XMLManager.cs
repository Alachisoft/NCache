using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Alachisoft.NCache.Common.XmlSerialization
{
    public class XMLManager
    {
        public static T ReadConfiguration<T>(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new Exception("Please provide path of configuration file!");

            XmlSerializer deserializer = new XmlSerializer(typeof(T));
            using (TextReader textReader = new StreamReader(path))
            {
                var data = (T)deserializer.Deserialize(textReader);
                return data;
            }
        }

        public static void WriteConfiguration<T>(string path, T data)
        {
            if (string.IsNullOrEmpty(path))
                throw new Exception("Please provide path of configuration file!");

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            //writer.WriteStartElement("wrapper");
            serializer.Serialize(writer, data);
            //writer.WriteEndElement();
            writer.Close();
        }
    }
}

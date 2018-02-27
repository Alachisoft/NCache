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

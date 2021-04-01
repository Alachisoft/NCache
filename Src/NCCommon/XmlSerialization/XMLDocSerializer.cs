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
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.XmlSerialization
{
    /// <summary>
    /// XML serialization and deserialization of strongly typed objects to/from an XML file.
    /// </summary>
    public static class XMLDocSerializer<T> where T : class
    {
        #region Load methods

        /// <summary>
        /// Loads an object from an XML file in Document format.
        /// </summary>
        /// <param name="path">Path of the file to load the object from.</param>
        /// <returns>Object loaded from an XML file in Document format.</returns>
        public static T Load(string path)
        {
            T serializableObject = LoadFromDocumentFormat(path);
            return serializableObject;
        }


        #endregion

        #region Save methods

        /// <summary>
        /// Saves an object to an XML file in Document format.
        /// </summary>
        /// <param name="serializableObject">Serializable object to be saved to file.</param>
        /// <param name="path">Path of the file to save the object to.</param>
        public static void Save(T serializableObject, string path)
        {
            SaveToDocumentFormat(serializableObject, path);
        }

        #endregion

        #region Private

        private static FileStream CreateFileStream(string path)
        {
            FileStream fileStream = null;
            fileStream = new FileStream(path, FileMode.OpenOrCreate);

            return fileStream;
        }

        private static T LoadFromDocumentFormat(string path)
        {
            T serializableObject = null;

            using (TextReader textReader = CreateTextReader(path))
            {
                XmlSerializer xmlSerializer = CreateXmlSerializer();
                serializableObject = xmlSerializer.Deserialize(textReader) as T;

            }

            return serializableObject;
        }

        private static TextReader CreateTextReader(string path)
        {
            TextReader textReader = null;
            textReader = new StreamReader(path);

            return textReader;
        }

        private static TextWriter CreateTextWriter(string path)
        {
            TextWriter textWriter = null;
            textWriter = new StreamWriter(path);

            return textWriter;
        }

        private static XmlSerializer CreateXmlSerializer()
        {
            Type ObjectType = typeof(T);

            XmlSerializer xmlSerializer = null;
            xmlSerializer = new XmlSerializer(ObjectType);

            return xmlSerializer;
        }

        private static void SaveToDocumentFormat(T serializableObject, string path)
        {
            using (TextWriter textWriter = CreateTextWriter(path))
            {
                XmlSerializer xmlSerializer = CreateXmlSerializer();
                xmlSerializer.Serialize(textWriter, serializableObject);
            }
        }

        private static void SaveToBinaryFormat(T serializableObject, string path)
        {
            using (FileStream fileStream = CreateFileStream(path))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(fileStream, serializableObject);
            }
        }

        #endregion
    }
} 
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

using System;
using System.Text;
using Newtonsoft.Json;
using Alachisoft.NCache.Serialization.JSON.CustomConverters;
using Alachisoft.NCache.Runtime.JSON;
using Alachisoft.NCache.Serialization.JSON;

namespace Alachisoft.NCache.Serialization.Formatters
{
    public class JsonBinaryFormatter
    {
        private static JsonSerializerSettings _settings;
        private static JsonSerializerSettings _searchableAttributesSettings;


        static JsonBinaryFormatter()
        {
            _settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                SerializationBinder = new CustomSerializationBinder()
            };
            _searchableAttributesSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ContractResolver = new SearchableAttributesContractResolver<PrimaryField>()
            };

            // 'RawJsonConverter' is faster as compared to 'SemiRawJsonConverter'
            _settings.Converters.Add(new RawJsonConverter());
        }

        public static byte[] ToByteArray(object value)
        {
            string json = SerializeObject(value);
            return Encoding.UTF8.GetBytes(json);
        }

        public static string SerializeObject(object value,bool isCustomAttributeBaseSerialzed=false)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if(isCustomAttributeBaseSerialzed)
                return JsonConvert.SerializeObject(value, _searchableAttributesSettings);

            return JsonConvert.SerializeObject(value, _settings);
        }             
        public static object FromByteBuffer(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            string json = Encoding.UTF8.GetString(buffer);
            return DeserializeObject(json);
        }

        public static object FromByteBuffer<T>(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            string json = Encoding.UTF8.GetString(buffer);
            return DeserializeObject<T>(json);
        }

        public static T DeserializeObject<T>(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
           

            return JsonConvert.DeserializeObject<T>(value, _settings);
        }

        public static object DeserializeObject(string value, bool isCustomAttributeBase = false)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (isCustomAttributeBase)
                return JsonConvert.DeserializeObject<object>(value, _searchableAttributesSettings);

            return JsonConvert.DeserializeObject(value, _settings);
        }

        public static string DecodeString(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            return Encoding.UTF8.GetString(buffer);
        }
    }
}

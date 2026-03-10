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

using Alachisoft.NCache.Common.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;

namespace Alachisoft.NCache.Serialization.JSON.CustomConverters
{
    /// <summary>
    ///  AddressConverter is to used to customized convert the Address class
    /// </summary>
    class AddressConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>true if this instance can convert the specified object type; otherwise, false.</returns>
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Address));
        }

        /// <summary>
        /// Write the Address Properties as you want in Json
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Address address = (Address)value;
            if (address != null)
            {
                // We have to Start the Object before wroting objects. And then Write the properties name
                // and their values.
                writer.WriteStartObject();
                writer.WritePropertyName("IpAddress");
                writer.WriteValue(address.IpAddress == null ? string.Empty : address.IpAddress.ToString() + ":" + address.Port.ToString());
                writer.WriteEndObject();
            }


        }
        /// <summary>
        /// Read the Json as you write the WriteJSon method
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            Address address = new Address();
            // Check if the reader token type is the StartObject
            if (reader.TokenType == JsonToken.StartObject)
            {
                JToken token = JToken.Load(reader);
                var addressPack = token.ToObject<AddressPack>();

                if (addressPack != null)
                {  string ip =   addressPack.IpAddress;

                    string[] ipAddress = ip.Split(':');
                    address.IpAddress = IPAddress.Parse(ipAddress[0]);
                    address.Port = Convert.ToInt32(ipAddress[1]);
                }
                else
                    return null;
            }
            return address;
        }
    }

    /// <summary>
    /// Utility class of AddressPack for json Address convertion
    /// </summary>
    public class AddressPack
    {   
        public string IpAddress { get; set; }
    }
}

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
using System.Numerics;
using Newtonsoft.Json;
using Alachisoft.NCache.Runtime.JSON;

namespace Alachisoft.NCache.Serialization.JSON.CustomConverters
{
    public class RawJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(JsonValueBase).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ReadJsonValueBase(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteRaw("");
                return;
            }

            var jsonValue = value as JsonValueBase;

            if (jsonValue == null)
                throw new JsonSerializationException($"Object of type other than '{typeof(JsonValueBase).FullName}' encountered while serializing.");

            WriteJsonValueBase(writer, jsonValue);
        }

        #region ---------------------------------- [ Read JSON ] ----------------------------------

        private JsonValueBase ReadJsonValueBase(JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return CreateJsonObject(reader);

                case JsonToken.StartArray:
                    return CreateJsonArray(reader);

                case JsonToken.Null:
                    return new JsonNull();

                default:
                    return ReadJsonValue(reader);
            }
        }

        private JsonValue ReadJsonValue(JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    return (bool)reader.Value;

                case JsonToken.Float:
                    return (double)reader.Value;

                case JsonToken.Integer:
                    if (reader.ValueType == typeof(BigInteger))
                        return (BigInteger)reader.Value;

                    return Convert.ToInt64(reader.Value);

                case JsonToken.String:
                    return (JsonValue)reader.Value.ToString();

                case JsonToken.Date:
                    return (DateTime)reader.Value;

                default:
                    throw new NotSupportedException($"Token '{reader.TokenType}' is not supported for read conversion.");
            }
        }

        private JsonObject CreateJsonObject(JsonReader reader)
        {
            var jsonObject = new JsonObject();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                var attributeName = reader.Value.ToString();

                reader.Read();
                if(attributeName.Equals("$type$"))
                {
                    jsonObject.Type = ReadJsonValue(reader).Value.ToString();
                    continue;
                }

                jsonObject.AddAttribute(
                    attributeName,
                    ReadJsonValueBase(reader)
                );
            }
            return jsonObject;
        }

        private JsonArray CreateJsonArray(JsonReader reader)
        {
            var jsonArray = new JsonArray();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                    break;

                jsonArray.Add(ReadJsonValueBase(reader));
            }
            return jsonArray;
        }

        #endregion

        #region --------------------------------- [ Write JSON ] ----------------------------------

        private void WriteJsonValueBase(JsonWriter writer, JsonValueBase jsonValue)
        {
            switch (jsonValue.DataType)
            {
                case Runtime.Enum.JsonDataType.Null:
                    writer.WriteNull();
                    break;

                case Runtime.Enum.JsonDataType.Object:
                    WriteJsonObject(writer, jsonValue as JsonObject);
                    break;

                case Runtime.Enum.JsonDataType.Array:
                    WriteJsonArray(writer, jsonValue as JsonArray);
                    break;

                default:
                    WriteJsonValue(writer, jsonValue as JsonValue);
                    break;
            }
        }

        private void WriteJsonValue(JsonWriter writer, JsonValue jsonValue)
        {
            if (jsonValue == default(JsonValue))
            {
                writer.WriteNull();
                return;
            }

            switch (jsonValue.DataType)
            {
                case Runtime.Enum.JsonDataType.Number:
                    var doubleValue = jsonValue.ToDouble();

                    if (Math.Floor(doubleValue) != doubleValue || doubleValue < long.MinValue || doubleValue > ulong.MaxValue)
                        writer.WriteValue(doubleValue);

                    else if (doubleValue < 0)
                        writer.WriteValue(jsonValue.ToInt64());

                    else
                        writer.WriteValue(jsonValue.ToUInt64());

                    break;

                case Runtime.Enum.JsonDataType.String:
                    writer.WriteValue(jsonValue.ToStringValue());
                    break;

                case Runtime.Enum.JsonDataType.Boolean:
                    writer.WriteValue(jsonValue.ToBoolean());
                    break;

                default:
                    throw new NotSupportedException($"Token '{jsonValue.DataType}' is not supported for write conversion.");
            }
        }

        private void WriteJsonObject(JsonWriter writer, JsonObject jsonObject)
        {
            if (jsonObject == default(JsonObject))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            if (jsonObject.Type != null)
            {
                //write type of json object at start 
                writer.WritePropertyName("$type$", true);
                WriteJsonValue(writer, (JsonValue)jsonObject.Type);
            }
            foreach (var attribute in jsonObject)
            {
                writer.WritePropertyName(attribute.Key, true);
                WriteJsonValueBase(writer, attribute.Value);
            }

            writer.WriteEndObject();
        }

        private void WriteJsonArray(JsonWriter writer, JsonArray jsonArray)
        {
            if (jsonArray == default(JsonArray))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartArray();

            foreach (var jsonValueBase in jsonArray)
            {
                WriteJsonValueBase(writer, jsonValueBase);
            }

            writer.WriteEndArray();
        }

        #endregion
    }
}
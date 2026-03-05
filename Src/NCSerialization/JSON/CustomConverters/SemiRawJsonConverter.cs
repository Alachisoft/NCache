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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Alachisoft.NCache.Runtime.JSON;

namespace Alachisoft.NCache.Serialization.JSON.CustomConverters
{
    public class SemiRawJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(JsonValueBase).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return GetJsonValueBase(JObject.Load(reader));

                case JsonToken.StartArray:
                    return GetJsonValueBase(JArray.Load(reader));

                case JsonToken.Null:
                    return new JsonNull();

                case JsonToken.Boolean:
                    return (bool)reader.Value;

                case JsonToken.Float:
                case JsonToken.Integer:
                    return Convert.ToDouble(reader.Value);

                case JsonToken.String:
                    return (JsonValue)reader.Value.ToString();

                default:
                    throw new NotSupportedException($"Token '{reader.TokenType}' is not supported for conversion.");
            }
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

            writer.WriteRaw(jsonValue.ToString());
        }

        private JsonValueBase GetJsonValueBase(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Array:
                    return CreateJsonArray(token);

                case JTokenType.Object:
                    return CreateJsonObject(token);

                case JTokenType.Null:
                    return new JsonNull();

                default:
                    return GetJsonValue(token);
            }
        }

        private JsonValue GetJsonValue(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return (bool)token;

                case JTokenType.Float:
                case JTokenType.Integer:
                    return Convert.ToDouble(token);

                case JTokenType.String:
                    return (JsonValue)token.ToString();

                default:
                    return default(JsonValue);
            }
        }

        private JsonObject CreateJsonObject(JToken token)
        {
            var jObject = token as JObject;
            var jsonObject = new JsonObject();

            if (jObject != default(JObject))
            {
                foreach (var property in jObject)
                {
                    jsonObject.AddAttribute(property.Key, GetJsonValueBase(property.Value));
                }
            }
            return jsonObject;
        }

        private JsonArray CreateJsonArray(JToken token)
        {
            var JsonArray = new JsonArray();

            foreach (var elem in token)
            {
                JsonArray.Add(GetJsonValueBase(elem));
            }
            return JsonArray;
        }
    }
}

using Alachisoft.NCache.Runtime.JSON;
using Newtonsoft.Json.Linq;
using System;


namespace Alachisoft.NCache.Serialization.JSON
{
    public class JsonHelper
    {
        public static object[] ToArray(JArray jArray)
        {
            object[] items = new object[jArray.Count];

            for (int i = 0; i < jArray.Count; i++)
            {
                switch (jArray[i].Type)
                {
                    case JTokenType.Object:
                        items[i] = ToJsonObject((JObject)jArray[i]);
                        break;
                    case JTokenType.Array:
                        items[i] = ToArray((JArray)jArray[i]);
                        break;
                    case JTokenType.Integer:
                        items[i] = (long)jArray[i];
                        break;
                    case JTokenType.Float:
                        items[i] = (double)jArray[i];
                        break;
                    case JTokenType.String:
                        items[i] = (string)jArray[i];
                        break;
                    case JTokenType.Boolean:
                        items[i] = (bool)jArray[i];
                        break;
                    case JTokenType.Date:
                        items[i] = (DateTime)jArray[i];
                        break;
                    case JTokenType.Null:
                        break;
                    default:
                        throw new Exception("Invalid type of json value");
                }

            }

            return items;
        }
        public static JsonObject ToJsonObject(JObject jObject)
        {
            var jsonObject = new JsonObject();

            foreach (var token in jObject)
            {
                switch (token.Value.Type)
                {
                    case JTokenType.Object:
                        jsonObject.AddAttribute(token.Key, ToJsonObject((JObject)token.Value));
                        break;
                    case JTokenType.Array:
                        var jsonArray = new JsonArray();

                        foreach (var jsonElemForArray in JsonDocumentUtil.ToJsonValueList(ToArray((JArray)token.Value)))
                            jsonArray.Add(jsonElemForArray);

                        jsonObject.AddAttribute(token.Key, jsonArray);
                        break;
                    case JTokenType.Integer:
                        jsonObject.AddAttribute(token.Key, (JsonValue)((long)token.Value));
                        break;
                    case JTokenType.Float:
                        jsonObject.AddAttribute(token.Key, (JsonValue)((double)token.Value));
                        break;
                    case JTokenType.String:
                        jsonObject.AddAttribute(token.Key, (JsonValue)token.Value.ToString());
                        break;
                    case JTokenType.Boolean:
                        jsonObject.AddAttribute(token.Key, (JsonValue)((bool)token.Value));
                        break;
                    case JTokenType.Date:
                        jsonObject.AddAttribute(token.Key, (JsonValue)((DateTime)token.Value));
                        break;
                    case JTokenType.Null:
                        break;
                    default:
                        throw new Exception("Invalid type of json value");
                }
            }
            return jsonObject;
        }
    }
}

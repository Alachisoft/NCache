using Alachisoft.NCache.Runtime.Enum;
using Alachisoft.NCache.Runtime.JSON;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Serialization.JSON
{
    public class JsonDocumentUtil
    {
        public static List<JsonValueBase> ToJsonValueList(IEnumerable arrayList)
        {
            List<JsonValueBase> jsonList = new List<JsonValueBase>();
            foreach (var value in arrayList)
            {
                JsonValueBase jsonValue = ToJsonValueBase(value/*, 0, 0*/);
                jsonList.Add(jsonValue);
            }
            return jsonList;
        }

        public static JsonValueBase ToJsonValueBase(object value/*, int binarySize, int binaryInMemorySize*/)
        {
            JsonValueBase jsonValue = null;

            if (IsNumber(value))
                jsonValue = (JsonValue)Convert.ToDouble(value);

            else if (value is bool)
                jsonValue = (JsonValue)((bool)value);

            else if (value is string || value is char)
                jsonValue = (JsonValue)value.ToString();

            else if (value is DateTime)
                jsonValue = (JsonValue)((DateTime)value);

            else if (value is JArray)
            {
                var jsonArray = new JsonArray();
                object[] values = JsonHelper.ToArray((JArray)value);

                foreach (var jsonValueForArray in ToJsonValueList(values))
                    jsonArray.Add(jsonValueForArray);

                jsonValue = jsonArray;
            }
            else if (value is JObject)
                jsonValue = JsonHelper.ToJsonObject((JObject)value);

            else
                throw new Exception("Json type not supported.");

            return jsonValue;
        }

        public static bool IsNumber(object value)
        {
            if (value == null)
                return false;

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }
    }
}

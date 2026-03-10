using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON.CustomConverters
{
    public class DecimalJsonConverter : JsonConverter<decimal>
    {
        public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public override decimal ReadJson(JsonReader reader, Type objectType, decimal existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return decimal.Parse((string)reader.Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            return Convert.ToDecimal(reader.Value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

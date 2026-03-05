using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON
{
    public class NCDataRowConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(DataRowCollection).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, new object());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (existingValue != null && existingValue is DataTable dataTable)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read();
                    while (true)
                    {
                        if (reader.TokenType == JsonToken.StartArray)
                        {
                            reader.Read();
                            object[] values = new object[dataTable.Columns.Count];
                            for (int i = 0; i < dataTable.Columns.Count; i++)
                            {
                                if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray) values[i] = serializer.Deserialize(reader, dataTable.Columns[i].DataType);
                                else
                                {
                                    values[i] = (reader.Value != null) ? (serializer.Deserialize(reader, dataTable.Columns[i].DataType) ?? DBNull.Value) : DBNull.Value;
                                }

                                reader.Read();
                            }

                            dataTable.Rows.Add(values);
                            reader.Read();
                        }

                        if (reader.TokenType == JsonToken.EndArray) break;
                    }
                }
            }
            else
            {
                return serializer.Deserialize(reader);
            }

            return existingValue;
        }
    }
}

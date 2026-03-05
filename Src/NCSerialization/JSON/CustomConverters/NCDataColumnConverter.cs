using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON
{
    public class NCDataColumnConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(DataColumnCollection).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, new object());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IList<DataColumn> primaryColumns = new List<DataColumn>();
            if (existingValue != null && existingValue is DataTable dataTable)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read();
                    while (true)
                    {
                        var column = NCDataTableUtil.ReadDataColumn(reader, serializer, out bool isPrimaryKey);
                        if (column != null)
                        {
                            dataTable.Columns.Add(column);
                            if (isPrimaryKey) primaryColumns.Add(column);
                        }

                        if (reader.TokenType == JsonToken.EndArray)
                        {
                            dataTable.PrimaryKey = primaryColumns.ToArray();
                            break;
                        }
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


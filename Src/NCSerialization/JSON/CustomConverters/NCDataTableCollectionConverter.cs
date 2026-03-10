using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON.CustomConverters
{
    public class NCDataTableCollectionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(DataTableCollection).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, new object());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IList<DataTable> primaryColumns = new List<DataTable>();
            if (existingValue != null && existingValue is DataSet dataSet)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read();
                    while (true)
                    {
                        var table = NCDataSetUtil.ReadDataTable(reader, serializer);
                        if (table != null)
                        {
                            dataSet.Tables.Add(table);
                        }

                        if (reader.TokenType == JsonToken.EndArray)
                            break;
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


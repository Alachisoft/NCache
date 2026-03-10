using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON.CustomConverters
{
    public class NCDataSetUtil
    {
        public readonly static string DATASET_NAME = "DataSetName";
        public readonly static string TABLES = "Tables";

        public static DataTable ReadDataTable(JsonReader reader, JsonSerializer serializer)
        {

            long rowCount = 0;
            DataTable dataTable = new DataTable();

            reader.Read();
            if (reader.TokenType == JsonToken.PropertyName)
            {
                if ((string)reader.Value == NCDataTableUtil.TYPE)
                {
                    reader.Read();
                }
            }
            else
            {
                throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
            }

            reader.Read();
            if (reader.TokenType == JsonToken.PropertyName)
            {
                if ((string)reader.Value == NCDataTableUtil.TABLE_NAME)
                {
                    reader.Read();
                    dataTable.TableName = (string)reader.Value;
                    reader.Read();
                }
            }
            else
            {
                throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
            }

            if (reader.TokenType == JsonToken.PropertyName)
            {
                if ((string)reader.Value == NCDataTableUtil.CASE_SENSITIVE)
                {
                    reader.Read();
                    dataTable.CaseSensitive = (bool)reader.Value;
                    reader.Read();
                }
            }
            else
            {
                throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
            }

            if (reader.TokenType == JsonToken.PropertyName)
            {
                if ((string)reader.Value == NCDataTableUtil.NAMESPACE)
                {
                    reader.Read();
                    dataTable.Namespace = (string)reader.Value;
                    reader.Read();
                }
            }
            else
            {
                throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
            }

            if (reader.TokenType == JsonToken.PropertyName)
            {
                if ((string)reader.Value == NCDataTableUtil.PREFIX)
                {
                    reader.Read();
                    dataTable.Prefix = (string)reader.Value;
                    reader.Read();
                }
            }
            else
            {
                throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
            }

            IList<DataColumn> primaryColumns = new List<DataColumn>();
            long columnCount = 0;
            if (reader.TokenType == JsonToken.PropertyName)
            {
                if ((string)reader.Value == NCDataTableUtil.COLUMNS)
                {
                    reader.Read();
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
                                columnCount++;
                            }

                            if (reader.TokenType == JsonToken.EndArray)
                            {
                                dataTable.PrimaryKey = primaryColumns.ToArray();
                                reader.Read();
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
            }

            if (reader.TokenType == JsonToken.PropertyName)
            {
                if ((string)reader.Value == NCDataTableUtil.ROWS)
                {
                    reader.Read();
                    if (reader.TokenType == JsonToken.StartArray)
                    {
                        reader.Read();
                        while (true)
                        {
                            if (reader.TokenType == JsonToken.StartArray)
                            {
                                reader.Read();
                                object[] values = new object[columnCount];
                                for (int i = 0; i < columnCount; i++)
                                {
                                    if (reader.TokenType == JsonToken.StartObject || reader.TokenType == JsonToken.StartArray) values[i] = serializer.Deserialize(reader, dataTable.Columns[i].DataType);
                                    else
                                    {
                                        values[i] = (reader.Value != null) ? (serializer.Deserialize(reader, dataTable.Columns[i].DataType) ?? DBNull.Value) : DBNull.Value;
                                    }

                                    reader.Read();
                                }
                                rowCount++;
                                dataTable.Rows.Add(values);
                                reader.Read();
                            }

                            if (reader.TokenType == JsonToken.EndArray)
                            {
                                reader.Read();
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
            }

            if (reader.TokenType == JsonToken.EndObject) reader.Read();

            return dataTable;
        }
    }
}

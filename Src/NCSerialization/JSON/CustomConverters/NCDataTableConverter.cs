using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
namespace Alachisoft.NCache.Serialization.JSON
{
    public class NCDataTableConverter : DataTableConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            DataTable tableToWrite = (DataTable)value;
            writer.WriteStartObject();

            writer.WritePropertyName(NCDataTableUtil.TYPE);
            serializer.Serialize(writer, tableToWrite.GetType().FullName + ", " + tableToWrite.GetType().Assembly);

            writer.WritePropertyName(NCDataTableUtil.TABLE_NAME);
            serializer.Serialize(writer, tableToWrite.TableName);

            writer.WritePropertyName(NCDataTableUtil.CASE_SENSITIVE);
            serializer.Serialize(writer, tableToWrite.CaseSensitive);

            writer.WritePropertyName(NCDataTableUtil.NAMESPACE);
            serializer.Serialize(writer, tableToWrite.Namespace);

            writer.WritePropertyName(NCDataTableUtil.PREFIX);
            serializer.Serialize(writer, tableToWrite.Prefix);

            writer.WritePropertyName(NCDataTableUtil.COLUMNS);
            writer.WriteStartArray();
            foreach (DataColumn column in tableToWrite.Columns)
            {
                if (serializer.NullValueHandling != NullValueHandling.Ignore)
                {
                    WriteDataColumn(tableToWrite, column, writer, serializer);
                }
            }
            writer.WriteEndArray();

            writer.WritePropertyName(NCDataTableUtil.ROWS);
            writer.WriteStartArray();
            foreach (DataRow row in tableToWrite.Rows)
            {
                writer.WriteStartArray();
                if (row != null && row.ItemArray != null)
                {
                    foreach (object item in row.ItemArray)
                    {
                        if (serializer.NullValueHandling != NullValueHandling.Ignore || item != null)
                        {
                            serializer.Serialize(writer, item);
                        }
                    }
                }
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private void WriteDataColumn(DataTable dataTable, DataColumn column, JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(NCDataTableUtil.COLUMN_NAME);
            serializer.Serialize(writer, column.ColumnName);

            var columnType = column.DataType.FullName + ", " + column.DataType.Assembly;
            writer.WritePropertyName(NCDataTableUtil.COLUMN_TYPE);
            serializer.Serialize(writer, columnType);

            writer.WritePropertyName(NCDataTableUtil.DEFAULT_VALUE);
            serializer.Serialize(writer, column.DefaultValue);

            writer.WritePropertyName(NCDataTableUtil.UNIQUE);
            serializer.Serialize(writer, column.Unique);

            writer.WritePropertyName(NCDataTableUtil.AUTO_INCREMENT);
            serializer.Serialize(writer, column.AutoIncrement);

            writer.WritePropertyName(NCDataTableUtil.ALLOW_DB_NULL);
            serializer.Serialize(writer, column.AllowDBNull);

            writer.WritePropertyName(NCDataTableUtil.READ_ONLY);
            serializer.Serialize(writer, column.ReadOnly);

            writer.WritePropertyName(NCDataTableUtil.COLUMN_MAPPING);
            serializer.Serialize(writer, column.ColumnMapping);

            writer.WritePropertyName(NCDataTableUtil.AUTO_INCREMENT_SEED);
            serializer.Serialize(writer, column.AutoIncrementSeed);

            writer.WritePropertyName(NCDataTableUtil.AUTO_INCREMENT_STEP);
            serializer.Serialize(writer, column.AutoIncrementStep);

            writer.WritePropertyName(NCDataTableUtil.CAPTION);
            serializer.Serialize(writer, column.Caption);

            writer.WritePropertyName(NCDataTableUtil.DATETIME_MODE);
            serializer.Serialize(writer, column.DateTimeMode);

            writer.WritePropertyName(NCDataTableUtil.EXPRESSION);
            serializer.Serialize(writer, column.Expression);

            writer.WritePropertyName(NCDataTableUtil.PREFIX);
            serializer.Serialize(writer, column.Prefix);

            writer.WritePropertyName(NCDataTableUtil.NAMESPACE);
            serializer.Serialize(writer, column.Namespace);

            writer.WritePropertyName(NCDataTableUtil.MAX_LENGTH);
            serializer.Serialize(writer, column.MaxLength);

            bool isPrimaryKey = false;
            writer.WritePropertyName(NCDataTableUtil.IS_PRIMARY_KEY);
            for (int i = 0; i < dataTable.PrimaryKey.Length; i++)
            {
                if (column.ColumnName.Equals(dataTable.PrimaryKey[i].ColumnName))
                {
                    isPrimaryKey = true;
                    break;
                }
            }
            serializer.Serialize(writer, isPrimaryKey);

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            long rowCount = 0;
            DataTable dataTable;
            if ((dataTable = existingValue as DataTable) == null)
            {
                dataTable = (objectType == typeof(DataTable)) ? new DataTable() : ((DataTable)Activator.CreateInstance(objectType));
            }

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

            return dataTable;
        }
    }
}


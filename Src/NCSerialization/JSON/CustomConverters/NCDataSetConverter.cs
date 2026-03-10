using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON.CustomConverters
{
    public class NCDataSetConverter : DataSetConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            DataSet dataSet = (DataSet)value;
            writer.WriteStartObject();
            // Write DataSet type
            writer.WritePropertyName(NCDataTableUtil.TYPE);
            serializer.Serialize(writer, dataSet.GetType().FullName + ", " + dataSet.GetType().Assembly);
            // Write DataSet name
            writer.WritePropertyName(NCDataSetUtil.DATASET_NAME);
            serializer.Serialize(writer, dataSet.DataSetName);
            // Write other properties
            writer.WritePropertyName(NCDataTableUtil.CASE_SENSITIVE);
            serializer.Serialize(writer, dataSet.CaseSensitive);
            writer.WritePropertyName(NCDataTableUtil.NAMESPACE);
            serializer.Serialize(writer, dataSet.Namespace);
            writer.WritePropertyName(NCDataTableUtil.PREFIX);
            serializer.Serialize(writer, dataSet.Prefix);
            // Write tables
            writer.WritePropertyName(NCDataSetUtil.TABLES);
            writer.WriteStartArray();
            foreach (DataTable table in dataSet.Tables)
            {
                WriteDataTable(dataSet, table, writer, serializer);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        public void WriteDataTable(DataSet dataSet, DataTable tableToWrite, JsonWriter writer, JsonSerializer serializer)
        {
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
            DataSet dataSet;
            if ((dataSet = existingValue as DataSet) == null)
            {
                dataSet = new DataSet();
            }
            if (reader.TokenType == JsonToken.StartObject)
            {
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
                    if ((string)reader.Value == NCDataSetUtil.DATASET_NAME)
                    {
                        reader.Read();
                        string dataSetName = (string)reader.Value;
                        dataSet = new DataSet(dataSetName);
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
                        dataSet.CaseSensitive = (bool)reader.Value;
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
                        dataSet.Namespace = (string)reader.Value;
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
                        dataSet.Prefix = (string)reader.Value;
                        reader.Read();
                    }
                }
                else
                {
                    throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
                }
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    if ((string)reader.Value == NCDataSetUtil.TABLES)
                    {
                        reader.Read();
                        if (reader.TokenType == JsonToken.StartArray)
                        {
                            reader.Read();
                            while (reader.TokenType != JsonToken.EndArray)
                            {
                                var table = NCDataSetUtil.ReadDataTable(reader, serializer);
                                dataSet.Tables.Add(table);
                            }
                            reader.Read();
                        }
                    }
                }
                else
                {
                    throw new JsonSerializationException($"Unexpected JSON token when reading DataTable. Expected PropertyName, got {reader.TokenType}.");
                }
            }
            return dataSet;
        }
    }
}

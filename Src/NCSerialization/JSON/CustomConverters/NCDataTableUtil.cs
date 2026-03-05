using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Alachisoft.NCache.Serialization.JSON
{
    public class NCDataTableUtil
    {
        public readonly static string TYPE = "$type";
        public readonly static string TABLE_NAME = "TableName";
        public readonly static string ROWS = "Rows";
        public readonly static string COLUMNS = "Columns";
        public readonly static string COLUMN_NAME = "$columnName";
        public readonly static string COLUMN_TYPE = "$columnType";
        public readonly static string CASE_SENSITIVE = "CaseSensitive";
        public readonly static string NAMESPACE = "Namespace";
        public readonly static string PREFIX = "Prefix";
        public readonly static string DEFAULT_VALUE = "DefaultValue";
        public readonly static string UNIQUE = "Unique";
        public readonly static string AUTO_INCREMENT = "AutoIncrement";
        public readonly static string ALLOW_DB_NULL = "AllowDBNull";
        public readonly static string READ_ONLY = "ReadOnly";
        public readonly static string COLUMN_MAPPING = "ColumnMapping";
        public readonly static string AUTO_INCREMENT_SEED = "AutoIncrementSeed";
        public readonly static string AUTO_INCREMENT_STEP = "AutoIncrementStep";
        public readonly static string CAPTION = "Caption";
        public readonly static string DATETIME_MODE = "DateTimeMode";
        public readonly static string EXPRESSION = "Expression";
        public readonly static string MAX_LENGTH = "MaxLength";
        public readonly static string IS_PRIMARY_KEY = "IsPrimaryKey";

        public static DataColumn ReadDataColumn(JsonReader reader, JsonSerializer serializer, out bool isPrimaryKey)
        {
            DataColumn column = null;
            isPrimaryKey = false;
            if (reader.TokenType == JsonToken.StartObject)
            {
                reader.Read();
                if ((string)reader.Value == NCDataTableUtil.COLUMN_NAME)
                {
                    reader.Read();
                    string columnName = (string)reader.Value;
                    Type type = null;
                    reader.Read();
                    if ((string)reader.Value == NCDataTableUtil.COLUMN_TYPE)
                    {
                        reader.Read();
                        type = Type.GetType((string)reader.Value, true, false);
                        column = new DataColumn(columnName, type);
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.DEFAULT_VALUE)
                    {
                        reader.Read();
                        column.DefaultValue = (reader.Value != null) ? (serializer.Deserialize(reader, type) ?? DBNull.Value) : DBNull.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.UNIQUE)
                    {
                        reader.Read();
                        column.Unique = (bool)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.AUTO_INCREMENT)
                    {
                        reader.Read();
                        column.AutoIncrement = (bool)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.ALLOW_DB_NULL)
                    {
                        reader.Read();
                        column.AllowDBNull = (bool)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.READ_ONLY)
                    {
                        reader.Read();
                        column.ReadOnly = (bool)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.COLUMN_MAPPING)
                    {
                        reader.Read();
                        column.ColumnMapping = (MappingType)serializer.Deserialize(reader, typeof(MappingType));
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.AUTO_INCREMENT_SEED)
                    {
                        reader.Read();
                        column.AutoIncrementSeed = (long)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.AUTO_INCREMENT_STEP)
                    {
                        reader.Read();
                        column.AutoIncrementStep = (long)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.CAPTION)
                    {
                        reader.Read();
                        column.Caption = (string)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.DATETIME_MODE)
                    {
                        reader.Read();
                        column.DateTimeMode = (DataSetDateTime)serializer.Deserialize(reader, typeof(DataSetDateTime));
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.EXPRESSION)
                    {
                        reader.Read();
                        column.Expression = (string)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.PREFIX)
                    {
                        reader.Read();
                        column.Prefix = (string)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.NAMESPACE)
                    {
                        reader.Read();
                        column.Namespace = (string)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.MAX_LENGTH)
                    {
                        reader.Read();
                        column.MaxLength = (int)(long)reader.Value;
                        reader.Read();
                    }

                    if ((string)reader.Value == NCDataTableUtil.IS_PRIMARY_KEY)
                    {
                        reader.Read();
                        isPrimaryKey = (bool)reader.Value;
                        reader.Read();
                    }

                    if (reader.TokenType == JsonToken.EndObject) reader.Read();
                }
            }

            return column;
        }
    }
}


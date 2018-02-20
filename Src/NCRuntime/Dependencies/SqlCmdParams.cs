// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License

using System.Data;
using System.Data.SqlTypes;

namespace Alachisoft.NCache.Runtime.Dependencies
{
    /// <summary>
    /// Holds the information about the type and value of the parameters passed to the command.
    /// </summary>

    public class SqlCmdParams

    {
        private CmdParamsType _type;
        private SqlParamDirection _direction;
        private SqlCmpOptions _compareInfo;
        private SqlDataRowVersion _sourceVersion;
        private bool _isNullable;
        private int _localeId;
        private int _offset;
        private byte _precision;
        private byte _scale;
        private object _value;
        private int _size;
        private string _sourceColumn;
        private bool _sourceColumnNullMapping;
        private object _sqlValue;
        private string _typeName;
        private string _udtName;

        /// <summary>
        /// Sets the SqlDbType of the passed parameter.
        /// </summary>
        public CmdParamsType Type
        {
            get { return _type; }
            set { _type = value; }
        }
        /// <summary>
        /// Sets a value that indicates whether the parameter is input-only, output-only, bidirectional, or a stored procedure return value parameter.
        /// </summary>
        public SqlParamDirection Direction
        {
            get { return _direction; }
            set { _direction = value; }
        }

        /// <summary>
        /// Sets the DataRowVersion to use when you load Value
        /// </summary>
        public SqlDataRowVersion SourceVersion
        {
            get { return _sourceVersion; }
            set { _sourceVersion = value; }
        }
        /// <summary>
        /// Sets the CompareInfo object that defines how string comparisons should be performed for this parameter.
        /// </summary>
        public SqlCmpOptions CompareInfo
        {
            get { return _compareInfo; }
            set { _compareInfo = value; }
        }

        /// <summary>
        /// The value of the passed parameter.
        /// </summary>
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }
        /// <summary>
        /// Sets the maximum size, in bytes, of the data within the column.
        /// </summary>
        public int Size
        {
            get { return _size; }
            set { _size = value; }
        }
        /// <summary>
        /// Sets a value that indicates whether the parameter accepts null values.
        /// </summary>
        public bool IsNullable
        {
            get { return _isNullable; }
            set { _isNullable = value; }
        }
        /// <summary>
        /// Sets the locale identifier that determines conventions and language for a particular region.
        /// </summary>
        public int LocaleID
        {
            get { return _localeId; }
            set { _localeId = value; }
        }
        /// <summary>
        /// Sets the offset to the Value property.
        /// </summary>
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }
        /// <summary>
        /// Sets the maximum number of digits used to represent the Value property.
        /// </summary>
        public byte Precision
        {
            get { return _precision; }
            set { _precision = value; }
        }
        /// <summary>
        /// Sets the number of decimal places to which Value is resolved.
        /// </summary>
        public byte Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }
        /// <summary>
        /// Sets the name of the source column mapped to the DataSet and used for loading or returning the Value.
        /// </summary>
        public string SourceColumn
        {
            get { return _sourceColumn; }
            set { _sourceColumn = value; }
        }
        /// <summary>
        /// Sets a value which indicates whether the source column is nullable. This allows SqlCommandBuilder to correctly generate Update statements for nullable columns.
        /// </summary>
        public bool SourceColumnNullMapping
        {
            get { return _sourceColumnNullMapping; }
            set { _sourceColumnNullMapping = value; }
        }
        /// <summary>
        /// Sets the value of the parameter as an SQL type.
        /// </summary>
        public object SqlValue
        {
            get { return _sqlValue; }
            set { _sqlValue = value; }
        }
        /// <summary>
        /// Sets the type name for a table-valued parameter.
        /// </summary>
        public string TypeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }
        /// <summary>
        /// Sets a string that represents a user-defined type as a parameter.
        /// </summary>
        public string UdtTypeName
        {
            get { return _udtName; }
            set { _udtName = value; }
        }

        public SqlDbType SqlParamType
        {
            get
            {
                switch (this._type)
                {
                    case CmdParamsType.BigInt:
                        return SqlDbType.BigInt;
                    case CmdParamsType.Binary:
                        return SqlDbType.Binary;
                    case CmdParamsType.Bit:
                        return SqlDbType.Bit;
                    case CmdParamsType.Char:
                        return SqlDbType.Char;
                    case CmdParamsType.Date:
                        return SqlDbType.Date;
                    case CmdParamsType.DateTime:
                        return SqlDbType.DateTime;
                    case CmdParamsType.DateTime2:
                        return SqlDbType.DateTime2;
                    case CmdParamsType.DateTimeOffset:
                        return SqlDbType.DateTimeOffset;
                    case CmdParamsType.Decimal:
                        return SqlDbType.Decimal;
                    case CmdParamsType.Float:
                        return SqlDbType.Float;
                    case CmdParamsType.Int:
                        return SqlDbType.Int;
                    case CmdParamsType.Money:
                        return SqlDbType.Money;
                    case CmdParamsType.NChar:
                        return SqlDbType.NChar;
                    case CmdParamsType.NVarChar:
                        return SqlDbType.NVarChar;
                    case CmdParamsType.Real:
                        return SqlDbType.Real;
                    case CmdParamsType.SmallDateTime:
                        return SqlDbType.SmallDateTime;
                    case CmdParamsType.SmallInt:
                        return SqlDbType.SmallInt;
                    case CmdParamsType.SmallMoney:
                        return SqlDbType.SmallMoney;
                    case CmdParamsType.Structured:
                        return SqlDbType.Structured;
                    case CmdParamsType.Time:
                        return SqlDbType.Time;
                    case CmdParamsType.Timestamp:
                        return SqlDbType.Timestamp;
                    case CmdParamsType.TinyInt:
                        return SqlDbType.TinyInt;
                    case CmdParamsType.Udt:
                        return SqlDbType.Udt;
                    case CmdParamsType.UniqueIdentifier:
                        return SqlDbType.UniqueIdentifier;
                    case CmdParamsType.VarBinary:
                        return SqlDbType.VarBinary;
                    case CmdParamsType.VarChar:
                        return SqlDbType.VarChar;
                    case CmdParamsType.Variant:
                        return SqlDbType.Variant;
                    case CmdParamsType.Xml:
                        return SqlDbType.Xml;
                    default:
                        return SqlDbType.NVarChar;
                }
            }
        }

        public ParameterDirection SqlParamDir
        {
            get
            {
                switch (this._direction)
                {
                    case SqlParamDirection.Input:
                        return ParameterDirection.Input;
                    case SqlParamDirection.Output:
                        return ParameterDirection.Output;
                    case SqlParamDirection.InputOutput:
                        return ParameterDirection.InputOutput;
                    case SqlParamDirection.ReturnValue:
                        return ParameterDirection.ReturnValue;
                    default:
                        return ParameterDirection.Input;
                }
            }
            set
            {
                switch (value)
                {
                    case ParameterDirection.Input:
                        this._direction = SqlParamDirection.Input;
                        break;
                    case ParameterDirection.InputOutput:
                        this._direction = SqlParamDirection.InputOutput;
                        break;
                    case ParameterDirection.Output:
                        this._direction = SqlParamDirection.Output;
                        break;
                    case ParameterDirection.ReturnValue:
                        this._direction = SqlParamDirection.ReturnValue;
                        break;
                }
            }
        }

     

        public SqlCompareOptions SqlCmpInfo
        {
            get
            {
                switch (this._compareInfo)
                {
                    case SqlCmpOptions.BinarySort:
                        return SqlCompareOptions.BinarySort;
                    case SqlCmpOptions.BinarySort2:
                        return SqlCompareOptions.BinarySort2;
                    case SqlCmpOptions.IgnoreCase:
                        return SqlCompareOptions.IgnoreCase;
                    case SqlCmpOptions.IgnoreKanaType:
                        return SqlCompareOptions.IgnoreKanaType;
                    case SqlCmpOptions.IgnoreNonSpace:
                        return SqlCompareOptions.IgnoreNonSpace;
                    case SqlCmpOptions.IgnoreWidth:
                        return SqlCompareOptions.IgnoreWidth;
                    case SqlCmpOptions.None:
                        return SqlCompareOptions.None;
                    default:
                        return SqlCompareOptions.None;
                }

            }
        }

        public DataRowVersion SrcVersion
        {
            get
            {
                switch (this._sourceVersion)
                {
                    case SqlDataRowVersion.Current:
                        return DataRowVersion.Current;
                    case SqlDataRowVersion.Default:
                        return DataRowVersion.Default;
                    case SqlDataRowVersion.Original:
                        return DataRowVersion.Original;
                    case SqlDataRowVersion.Proposed:
                        return DataRowVersion.Proposed;
                    default:
                        return DataRowVersion.Current;
                }
            }
        }
    }
}
//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Threading;
//using System.Runtime.Remoting.Messaging;
using System.Security.Permissions;

using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common;
using System.Data.SqlTypes;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    [Serializable]
    public class SqlCmdParams : ICompactSerializable,ISizable
    {
        private SqlDbType _type;
        private ParameterDirection _direction;
        private SqlCompareOptions _compareInfo;
        private DataRowVersion _sourceVersion;
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
        private string _udtTypeName;

        private bool _dbTypeSet = false;

        internal SqlCmdParams() { }

        public SqlCmdParams(SqlDbType type, object value)
        {
            _type = type;
            _value = value;
        }


        public int ParamSize
        {
            get { return _size; }
            set { _size = value; }
        }

        public bool IsNullable
        {
            get { return _isNullable; }
            set { _isNullable = value; }
        }

        public int LocaleID
        {
            get { return _localeId; }
            set { _localeId = value; }
        }

        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public byte Precision
        {
            get { return _precision; }
            set { _precision = value; }
        }

        public byte Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        public string SourceColumn
        {
            get { return _sourceColumn; }
            set { _sourceColumn = value; }
        }

        public bool SourceColumnNullMapping
        {
            get { return _sourceColumnNullMapping; }
            set { _sourceColumnNullMapping = value; }
        }

        public object SqlValue
        {
            get { return _sqlValue; }
            set { _sqlValue = value; }
        }

        public string TypeName
        {
            get { return _typeName; }
            set { _typeName = value; }
        }

        public string UdtName
        {
            get { return _udtTypeName; }
            set { _udtTypeName = value; }
        }

        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public SqlCompareOptions CmpInfo
        {
            get { return _compareInfo; }
            set { _compareInfo = value; }
        }

        public DataRowVersion SrcVersion
        {
            get { return _sourceVersion; }
            set { _sourceVersion = value; }
        }

        public SqlDbType DbType
        {
            get { return _type; }
            set
            {
                _type = value;
            }
        }
        public ParameterDirection Direction
        {
            get { return _direction; }
            set { _direction = value; }
        }

        private int DbTypeToInt
        {
            get { return (int)_type; }
        }

        private int DirectionToInt
        {
            get { return (int)_direction; }
        }

        private int CmpOptionsToInt
        {
            get { return (int)_compareInfo; }
        }

        private int SrcVersionToInt
        {
            get { return (int)_sourceVersion; }
        }

        public override string ToString()
        {

            return DbTypeToInt.ToString() + "\"" +
                DirectionToInt.ToString() + "\"" +
                CmpOptionsToInt.ToString() + "\"" +
                SrcVersionToInt.ToString() + "\"" +
                (_value == null ? "#" : _value.ToString()) + "\"" +
                _isNullable.ToString() + "\"" +
                _localeId.ToString() + "\"" +
                _offset.ToString() + "\"" +
                _precision.ToString() + "\"" +
                _scale.ToString() + "\"" +
                _size.ToString() + "\"" +
                (_sourceColumn == null || _sourceColumn == string.Empty ? "#" : _sourceColumn) + "\"" +
                _sourceColumnNullMapping.ToString() + "\"" +
                (_sqlValue != null ? _sqlValue.ToString() : "#") + "\"" +
                (_typeName == null || _typeName == string.Empty ? "#" : _typeName) + "\"" +
                (_udtTypeName == null || _udtTypeName == string.Empty ? "#" : _udtTypeName);


        }

        #region ISizable Implementation

        public int Size
        {
            get { return SqlCmParamsSize; }
        }

        public int InMemorySize
        {
            get
            {
                int inMemorySize = this.Size;

                inMemorySize += inMemorySize <= 24 ? 0 : Common.MemoryUtil.NetOverHead;

                return inMemorySize;
            }
        }

        private int SqlCmParamsSize
        {
            get
            {
                int temp = 0;
                temp += Common.MemoryUtil.NetEnumSize;  // for _type
                temp += Common.MemoryUtil.NetEnumSize;  // for _direction
                temp += Common.MemoryUtil.NetEnumSize;  // for _compareInfo
                temp += Common.MemoryUtil.NetEnumSize;  // for _sourceVersion
                temp += Common.MemoryUtil.NetByteSize;    // for _isNullable
                temp += Common.MemoryUtil.NetIntSize;    // for _localeId
                temp += Common.MemoryUtil.NetIntSize;    // for _offset
                temp += Common.MemoryUtil.NetIntSize;    // for _rbNodeKeySize
                temp += Common.MemoryUtil.NetByteSize;    // for _precision;
                temp += Common.MemoryUtil.NetByteSize;    // for _scale;
                temp += Common.MemoryUtil.NetByteSize; // for _sourceColumnNullMapping
                temp += Common.MemoryUtil.NetByteSize; // for _dbTypeSet

                temp += Common.MemoryUtil.GetStringSize(_value);
                temp += Common.MemoryUtil.GetStringSize(_sqlValue);
                temp += Common.MemoryUtil.GetStringSize(_sourceColumn);
                temp += Common.MemoryUtil.GetStringSize(_typeName);
                temp += Common.MemoryUtil.GetStringSize(_udtTypeName);

                return temp;
            }
        }
        #endregion

        #region ICompactSerializable Members

        public void  Deserialize(CompactReader reader)
        {
 	        _type = (SqlDbType)reader.ReadObject();
            _direction = (ParameterDirection)reader.ReadObject();
            _sourceVersion = (DataRowVersion)reader.ReadObject();
            _compareInfo = (SqlCompareOptions)reader.ReadObject();
            _value = reader.ReadObject();
            _isNullable = reader.ReadBoolean();
            _localeId = reader.ReadInt32();
            _offset = reader.ReadInt32();
            _precision = reader.ReadByte();
            _scale = reader.ReadByte();
            _size = reader.ReadInt32();
            _sourceColumn = reader.ReadString();
            _sourceColumnNullMapping = reader.ReadBoolean();
            _sqlValue = reader.ReadObject();
            _typeName = reader.ReadString();
            _udtTypeName = reader.ReadString();
            
        }

        public void  Serialize(CompactWriter writer)
        {
 	        writer.WriteObject(_type);
            writer.WriteObject(_direction);
            writer.WriteObject(_sourceVersion);
            writer.WriteObject(_compareInfo);
            writer.WriteObject(_value);
            writer.Write(_isNullable);
            writer.Write(_localeId);
            writer.Write(_offset);
            writer.Write(_precision);
            writer.Write(_scale);
            writer.Write(_size);
            writer.Write(_sourceColumn);
            writer.Write(_sourceColumnNullMapping);
            writer.WriteObject(_sqlValue);
            writer.Write(_typeName);
            writer.Write(_udtTypeName);
        }

        #endregion
    }
}


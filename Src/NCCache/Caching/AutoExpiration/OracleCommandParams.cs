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
// limitations under the License.

using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common;
using System.Data;
using Alachisoft.NCache.Runtime.Dependencies;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    public class OracleCommandParams : ICompactSerializable,ISizable
    {
        OracleCmdParamsType _type;
        OracleParameterDirection _direction;
        object _value;


        public OracleCommandParams(OracleCmdParamsType type, object value, OracleParameterDirection direction)
        {
            _type = type;
            _value = value;
            _direction = direction;
        }

        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        private int DbTypeToInt
        {
            get { return (int)_type; }
        }

        private int DirectionTypeToInt
        {
            get { return (int)_direction; }
        }

        public override string ToString()
        {
            return DbTypeToInt.ToString() + "\"" + (_value != null ? _value.ToString() : "") + "\"" + DirectionTypeToInt.ToString();
        }

        public OracleCmdParamsType Type
        {
            get
            {
                return _type;
            }
        }
        public ParameterDirection Direction
        {
            get
            {
                switch (_direction)
                {
                    case OracleParameterDirection.Input:
                        return ParameterDirection.Input;
                    case OracleParameterDirection.Output:
                        return ParameterDirection.Output;
                    default:
                        return ParameterDirection.InputOutput;
                }
            }
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            _type = (OracleCmdParamsType)reader.ReadObject();
            _value = reader.ReadObject();
            _direction = (OracleParameterDirection)reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_type);
            writer.WriteObject(_value);
            writer.WriteObject(_direction);
        }

        #endregion

        #region ISizable Implementation
        public int Size
        {
            get { return OracleCommandParamsSize; }
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

        private int OracleCommandParamsSize 
        {
            get 
            {
                int temp = 0;
                temp += Common.MemoryUtil.NetEnumSize; // for _type
                temp += Common.MemoryUtil.NetEnumSize;// for _direction
                
                if(_value!=null)
                    temp += Common.MemoryUtil.GetStringSize(_value);

                return temp;

            }
        }
        #endregion

    }
}


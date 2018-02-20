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

namespace Alachisoft.NCache.Runtime.Dependencies
{
    /// <summary>
    /// Holds the type and value of the parameters passed to the command instance.
    /// </summary>

    public class OracleCmdParams

    {
        private OracleCmdParamsType _type;
        private object _value;
        private OracleParameterDirection _direction;

        /// <summary>
        /// The direction of the passed parameters (in/out).
        /// </summary>
        public OracleParameterDirection Direction
        {
            get { return _direction; }
            set { _direction = value; }
        }

        /// <summary>
        /// The type of the command parameter.
        /// </summary>
        public OracleCmdParamsType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// The value of the command parameter.
        /// </summary>
        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        internal ParameterDirection OracleParamDirection
        {
            get 
            {
                switch (Direction)
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
    }
}
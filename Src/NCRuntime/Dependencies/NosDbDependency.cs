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

using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Runtime.Dependencies
{
    /// <summary>
    /// 
    /// </summary>
    
    [Serializable]
    public class NosDBDependency: CacheDependency
    {
        private Dictionary<string, object> _parameters;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandText"></param>
        /// <param name="connectionString"></param>
        public NosDBDependency( string connectionString, string commandText)
        {
            ConnectionString = connectionString;
            CommandText = commandText;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="commandText"></param>
        /// <param name="parameters"></param>
        /// <param name="timeout"></param>
        public NosDBDependency(string connectionString, string commandText, Dictionary<string, object> parameters, int timeout)
        {
            _parameters = parameters;
            Timeout = timeout;
            ConnectionString = connectionString;
            CommandText = commandText;
        }


        /// <summary>
        /// 
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string CommandText { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, object> Parameters
        {
            get
            {
                return _parameters ?? new Dictionary<string, object>();
            }
            set
            {
                _parameters = value;

            }
        }
      
        /// <summary>
        /// 
        /// </summary>
        public int Timeout { get; set; }

      

    }
}

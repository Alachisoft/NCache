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
using System.Collections;
using System.Collections.Generic;
using System.Data;

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.Dependencies
#else
namespace Alachisoft.NCache.Runtime.Dependencies
#endif
{
    /// <summary>
    /// Establishes a relationship between an item stored in an application's Cache object 
    /// and either a row in a specific Oracle database table or 
    /// the results of a Oracle query.
    /// This class can not be inherited.
    /// </summary>
    /// <remarks>
    /// You can add items with dependencies to your application's cache with the 
    /// <see cref="Cache.Add"/> and Add,Insert, AddDependency methods.
    /// <para>When you add an item to an application's <see cref="Cache"/> object with 
    /// <see cref="OracleCacheDependency"/>, on all ODP.NET supported versions of Oracle database, it monitors a 
    /// row in a specific Oracle database table so that when that row changes, items associated with 
    /// the row will be automatically removed from the Cache.  
    /// This helps you avoid losing changes made to the object between the time it is created and the 
    /// time it is inserted into the <see cref="Cache"/>. 
    /// <see cref="OracleCacheDependency"/> class supports the association of cached items with a 
    /// Oracle query. 
    /// </para>
    /// </remarks>
    /// <requirements>
    /// <constraint>This member is not available in SessionState edition.</constraint> 
    /// </requirements>
    [Serializable]

    public sealed class OracleCacheDependency : CacheDependency

    {
        private string _connectionString;
        private string _cmdText;
        private CommandType _cmdType;
        private Dictionary<string, OracleCmdParams> _cmdParams;


        public string ConnectionString
        {
            get { return _connectionString; }
        }

        public string CommandText
        {
            get { return _cmdText; }
        }

        public CommandType CommandType
        {
            get { return _cmdType; }
        }

        public Dictionary<string, OracleCmdParams> CommandParams
        {
            get { return _cmdParams; }
        }

        /// <summary>
        /// Initializes a new instance of the OracleCacheDependency class, 
        /// using the supplied connection string and query string.
        /// </summary>
        /// <remarks>
        /// This constructor is used to create OracleCacheDependency objects that use the 
        /// query-notification feature of Oracle products. 
        /// <para>
        /// Query must include the following:
        /// <list type="ul">
        /// <item>
        /// Fully qualified table names, including the name of the table owner. 
        /// For example, to refer to a table called "customers" that is owned by the database owner, 
        /// the statement must refer to "dbo.customers".
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="connectionString">connection string to be used by dependency.</param>
        /// <param name="cmdText">cmdText to be used by dependency.</param>
        /// <returns>A OracleCacheDependency object based on the supplied parameters.</returns>
        /// <requirements>        
        /// </requirements>
        public OracleCacheDependency(string connectionString, string cmdText)
            : this(connectionString, cmdText, OracleCommandType.Text, new Dictionary<string, OracleCmdParams>())
        { }

        /// <summary>
        /// Initializes a new instance of the OracleCacheDependency class. 
        /// It supports the use of stored procedures.
        /// </summary>
        /// <remarks>
        /// This constructor is used to create OracleCacheDependency objects that use the 
        /// query-notification feature of Oracle products. 
        /// <para>
        /// Query must include the following:
        /// <list type="ul">
        /// <item>
        /// Fully qualified table names, including the name of the table owner. 
        /// For example, to refer to a table called "customers" that is owned by the database owner, 
        /// the statement must refer to "dbo.customers".
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="connectionString">connection string to be used by dependency.</param>
        /// <param name="cmdText">cmdText to be used by dependency.</param>
        /// <param name="cmdType">The type of the command. (text/stored procedures)</param>
        /// <param name="cmdParams">Parameters to be passed to the command.</param>
        /// <returns>A OracleCacheDependency object based on the supplied parameters.</returns>
        /// <requirements>        
        /// </requirements>
        public OracleCacheDependency(string connectionString, string cmdText, OracleCommandType cmdType, Dictionary<string, OracleCmdParams> cmdParams)
        {
            switch (cmdType)
            {
                case OracleCommandType.Text:
                    _cmdType = CommandType.Text;
                    break;

                case OracleCommandType.StoredProcedure:
                    _cmdType = CommandType.StoredProcedure;
                    break;
            }

            _cmdText = cmdText;
            _connectionString = connectionString;
            _cmdParams = cmdParams;
        }

      
    }
}

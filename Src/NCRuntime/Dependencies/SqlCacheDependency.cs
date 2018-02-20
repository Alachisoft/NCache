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
using System.Text;
using System.Collections;
using System.Data;
using System.Collections.Generic;

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.Dependencies
#else
namespace Alachisoft.NCache.Runtime.Dependencies
#endif
{
    /// <summary>
    /// Establishes a relationship between an item stored in an application's Cache object 
    /// and either a row in a specific SQL Server database table or 
    /// the results of a SQL Server 2005 query.
    /// </summary>
    /// <remarks>
    /// You can add items with dependencies to your application's cache with the 
    /// <see cref="Cache.Add"/> and Insert methods.
    /// <para>When you add an item to an application's <see cref="Cache"/> object with 
    /// <see cref="SqlCacheDependency"/>, on all supported versions of SQL Server (7.0, 2000, 2005), it monitors a 
    /// row in a specific SQL Server database table so that when that row changes, items associated with 
    /// the row will be automatically removed from the Cache.  
    /// This helps you avoid losing changes made to the object between the time it is created and the 
    /// time it is inserted into the <see cref="Cache"/>. 
    /// <see cref="SqlCacheDependency"/> class supports the association of cached items with a 
    /// SQL Server query when SQL Server 2005 is used. 
    /// </para>
    /// </remarks>
    /// <requirements>
    /// <constraint>This member is not available in SessionState edition.</constraint> 
    /// </requirements>
    [Serializable]
    

    public sealed class SqlCacheDependency : CacheDependency

    {
        private string _connectionString;
        private string _cmdText;
        private CommandType _cmdType;
        private Dictionary<string, SqlCmdParams> _cmdParams;

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

        public Dictionary<string, SqlCmdParams> CommandParams
        {
            get { return _cmdParams; }
        }

        /// <summary>
        /// Initializes a new instance of the SqlCacheDependency class, 
        /// using the supplied connection string and query string.
        /// </summary>
        /// <remarks>
        /// This constructor is used to create SqlCacheDependency objects that use the 
        /// query-notification feature of SQL Server 2005 products. 
        /// <para>
        /// SQL query must include the following:
        /// <list type="ul">
        /// <item>
        /// Fully qualified table names, including the name of the table owner. 
        /// For example, to refer to a table called "customers" that is owned by the database owner, 
        /// the SQL statement must refer to "dbo.customers".
        /// </item>
        /// <item>
        /// Explicit column names in the Select statement. 
        /// You cannot use the asterisk (*) wildcard character to select all columns from a table. 
        /// For example, instead of "select * from customers", you must use 
        /// "select name, address, city, state from dbo.customers".
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="connectionString">connection string to be used by dependency.</param>
        /// <param name="cmdText">cmdText to be used by dependency.</param>
        /// <returns>A SqlCacheDependency object based on the supplied parameters.</returns>
        /// <requirements>
        /// <constraint>This member is only available with .NET 2.0 framework.</constraint> 
        /// </requirements>
        public SqlCacheDependency(string connectionString, string cmdText)
            : this(connectionString, cmdText, SqlCommandType.Text, new Dictionary<string, SqlCmdParams>())
        { }

        /// <summary>
        /// Initializes a new instance of the SqlCacheDependency class. 
        /// It supports the use of 'Stored Procedures'.
        /// </summary>
        /// <remarks>
        /// This constructor is used to create SqlCacheDependency objects that use the 
        /// query-notification feature of SQL Server 2005 products. 
        /// <para>
        /// SQL query must include the following:
        /// <list type="ul">
        /// <item>
        /// Fully qualified table names, including the name of the table owner. 
        /// For example, to refer to a table called "customers" that is owned by the database owner, 
        /// the SQL statement must refer to "dbo.customers".
        /// </item>
        /// <item>
        /// Explicit column names in the Select statement. 
        /// You cannot use the asterisk (*) wildcard character to select all columns from a table. 
        /// For example, instead of "select * from customers", you must use 
        /// "select name, address, city, state from dbo.customers".
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="connectionString">connection string to be used by dependency.</param>
        /// <param name="cmdText">cmdText to be used by dependency.</param>
        /// <param name="cmdType">The type of the command. (text/stored procedure)</param>
        /// <param name="cmdParams">Parameters to be passed to the command.</param>
        /// <returns>A SqlCacheDependency object based on the supplied parameters.</returns>
        /// <requirements>
        /// <constraint>This member is only available with .NET 2.0 framework.</constraint> 
        /// </requirements>
        public SqlCacheDependency(string connectionString, string cmdText, SqlCommandType cmdType, Dictionary<string, SqlCmdParams> cmdParams)
        {
            switch (cmdType)
            {
                case SqlCommandType.Text:
                    _cmdType = CommandType.Text;
                    break;

                case SqlCommandType.StoredProcedure:
                    _cmdType = CommandType.StoredProcedure;
                    break;
            }

            _connectionString = connectionString;
            _cmdText = cmdText;
            _cmdParams = cmdParams;
        }

       
    }

}

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

namespace Alachisoft.NCache.Runtime.Dependencies
{
    /// <summary>
	/// Establishes a relationship between an item stored in an application's Cache object 
	/// and either a specific database table. 
	/// This class cannot be inherited.
	/// </summary>
	/// <remarks>
  	/// You can add items with dependencies to your application's cache with the 
	/// <see cref="Cache.Add"/> and Insert methods.
	/// <para>When you add an item to an application's <see cref="Cache"/> object with 
	/// <see cref="DBCacheDependency"/>, it monitors a specific database table so that 
	/// when the table changes, items associated with 
	/// the table will be automatically removed from the Cache.  
	/// This helps you avoid losing changes made to the object between the time it is created and the 
	/// time it is inserted into the <see cref="Cache"/>. 
	/// </para>
	/// </remarks>
	/// <requirements>
	/// <constraint>This member is not available in SessionState edition.</constraint> 
	/// </requirements>
	[Serializable]

	public sealed class DBCacheDependency : CacheDependency
	{
        private string _connectionString;
        private string _primaryKey;
        private DBDependencyType _type;

		/// <summary>
		/// Initializes a new instance of the OleDbCacheDependency class, 
		/// using the supplied parameters to create a cache-key dependency. 
		/// </summary>
		/// <remarks>
		/// This constructor is used to create OleDbCacheDependency 
		/// objects for SQL Server 7 and SQL Server 2000 products.
		/// <para>
		/// You need to enable notification for both the table and the database containing the 
		/// table before you can use this <see cref="DBCacheDependency"/> in your application.
		/// </para>
		/// </remarks>
//		/// <param name="connectionString">The connection string for establishing a connection with
//		/// the database server where the database containing above mentioned table exists.</param>
        internal DBCacheDependency()
        { }

        public string ConnectionString
        {
            get { return _connectionString; }
        }

        public string PrimaryKey
        {
            get { return _primaryKey; }
        }

        public DBDependencyType Type
        {
            get { return _type; }
        }

        internal void CreateSqlCacheDependency(string connectionString, string primaryKey)
        {
            _connectionString = connectionString;
            _primaryKey = primaryKey;
            _type = DBDependencyType.SqlCacheDependency;
        }

        internal void CreateOleDbCacheDependency(string connectionString, string primaryKey)
        {
            _connectionString = connectionString;
            _primaryKey = primaryKey;
            _type = DBDependencyType.OleDbCacheDependency;
        }

	}

    //#endif
}

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

namespace Alachisoft.NCache.Runtime.Dependencies
{
    /// <summary>
    /// Returns an instance of <see cref="DBCacheDependency"/> class.  
    /// This is a static class.
    /// </summary>
    /// <requirements>
    /// <constraint>This member is not available in SessionState edition.</constraint> 
    /// </requirements>

    public class DBDependencyFactory


    {
        private DBDependencyFactory() { }
        /// <summary>
        /// Creates a new instance of Sql7CacheDependency class,
        /// using the supplied parameters.
        /// </summary>
        /// <remarks>
        /// This static method is used to create SqlCacheDependency 
        /// objects for SQL Server 7 and SQL Server 2000 products.
        /// <para>
        /// You need to enable notification for both the table and the database containing the 
        /// table before you can use this <see cref="DBCacheDependency"/> in your application.
        /// </para>
        /// </remarks>
        /// <param name="connectionString">The connection string for establishing a connection with
        /// the database server where the database containing above mentioned table exists.</param>
        /// <param name="primaryKey">The primary key of the record. Many dependency objects 
        /// can be created based on the same primary key. When the record with this primary key 
        /// is modified in the database, the associated item(s) in the cache expire(s).</param>
        /// <returns>A SqlCacheDependency object based on the supplied parameters.</returns>
        public static DBCacheDependency CreateSqlCacheDependency(string connectionString, string dbCacheKey)
        {
            DBCacheDependency dbDependency = new DBCacheDependency();
            dbDependency.CreateSqlCacheDependency(connectionString, dbCacheKey);
            return dbDependency;
        }

        /// <summary>
        /// Creates a new instance of <see cref="OleDbCacheDependency"/> class,
        /// using the supplied parameters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// You need to enable notification for both the table and the database containing the 
        /// table before you can use this <see cref="DBCacheDependency"/> in your application.
        /// </para>
        /// </remarks>
        /// <param name="connectionString">The connection string for establishing a connection with
        /// the database server where the database containing above mentioned table exists.</param>
        /// <param name="primaryKey">The primary key of the record. Many dependency objects 
        /// can be created based on the same primary key. When the record with this primary key 
        /// is modified in the database, the associated item(s) in the cache expire(s).</param>
        /// <returns>A SqlCacheDependency object based on the supplied parameters.</returns>

        public static DBCacheDependency CreateOleDbCacheDependency(string connectionString, string dbCacheKey)
        {
            DBCacheDependency dbDependency = new DBCacheDependency();
            dbDependency.CreateOleDbCacheDependency(connectionString, dbCacheKey);
            return dbDependency;
        }
    }
}
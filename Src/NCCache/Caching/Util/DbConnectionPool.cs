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
using System.Collections;
using System.Globalization;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.DataStructures.Clustered;

namespace Alachisoft.NCache.Caching.Util
{
    internal class DbConnectionPool : ResourcePool
	{
		internal class DbResourceInfo : IDisposable
		{
			private IDbConnection _conn;
			private IDictionary _syncData;

			public DbResourceInfo(IDbConnection conn)
			{
				_conn = conn;
			}

			#region	/                 --- IDisposable ---           /

			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or 
			/// resetting unmanaged resources.
			/// </summary>
			void IDisposable.Dispose()
			{
				if(_conn != null)
				{
					_conn.Close();
                    _conn.Dispose();
					_conn = null;
				}
			}

			#endregion

			public IDbConnection Connection 
			{ 
				get { return _conn; } 
				set { _conn = value; } 
			}

			public IDictionary DbSyncInfo
			{
				get {return _syncData;} 
				set {_syncData = value;}
			}
		}

        private ILogger _ncacheLog;
        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        public DbConnectionPool(ILogger NCacheLog)
        {
            this._ncacheLog = NCacheLog;
        }

        /// <summary>
		/// Adds a connection to the _connectionTable if already not present.
		/// Otherwise, increments the referrence count for it.
		/// </summary>
		/// <param name="connString"></param>
        /// <param name="connection">An initialized connection object</param>
		/// <returns></returns>
		public IDbConnection PoolConnection(string connString, IDbConnection connection)
		{
			lock (this)
			{
				string connKey = connString.ToLower();

				DbResourceInfo connInfo = (DbResourceInfo)GetResource(connKey);
				if (connInfo == null)
				{
					connection.ConnectionString = connection.ConnectionString;
                    connection.Open();
                    connInfo = new DbResourceInfo(connection);
					AddResource(connKey, connInfo);
				}
				else
				{
                    if (connInfo.Connection.State == ConnectionState.Broken || connInfo.Connection.State == ConnectionState.Closed)
                        connection.Open();
					AddResource(connKey, null);  //To increase the reference count
				}

				return connInfo.Connection;
			}
		}

		/// <summary>
		/// When connection is no more required, it is closed and removed from the 
		/// _connectionTable.
		/// </summary>
		/// <param name="connString"></param>
		public void RemoveConnection(string connString)
		{
			lock (this)
			{
				RemoveResource(connString.ToLower());
			}
		}

        /// <summary>
        /// Removes the Severed Connection from the Resource pool.
        /// </summary>
        /// <param name="connString">The connection string of the Severed Connection.</param>
        public void RemoveSeveredConnection(string connString)
        {
            lock (this)
            {
                IDbConnection conn = GetConnection(connString);
                if (conn != null)
                    conn.Close();
                RemoveSeveredResource(connString.ToLower());
            }
        }

		/// <summary>
		/// Wrapper for ResourcePool.GetResource(string key).
		/// </summary>
		/// <param name="connString"></param>
		/// <returns></returns>
		public IDbConnection GetConnection(string connString)
		{
			lock (this)
			{
				DbResourceInfo connInfo = (DbResourceInfo) GetResource(connString.ToLower());
				if(connInfo != null)
					return connInfo.Connection;
				return null;
			}
		}


		/// <summary>
		/// Wrapper for ResourcePool.GetResource(string key).
		/// </summary>
		/// <param name="connString"></param>
		/// <returns></returns>
		public IDictionary GetResourceSyncInfo(string connString)
		{
			lock (this)
			{
				DbResourceInfo connInfo = (DbResourceInfo) GetResource(connString.ToLower());
				if(connInfo != null)
					return connInfo.DbSyncInfo;
				return null;
			}
		}
		

		/// <summary>
		/// Acquire the modified records in ncache_db_sync table
		/// </summary>
		/// <param name="syncTable"></param>
		/// <param name="cacheName"></param>
		public void AcquireSyncData(string syncTable, string cacheName)
		{
			lock (this)
			{
				IEnumerator em = Keys.GetEnumerator();
				while (em.MoveNext())
				{
					DbResourceInfo connInfo = (DbResourceInfo) GetResource((string)em.Current); 
					
					IDictionary dbSyncInfo = LoadTableData(syncTable, cacheName, connInfo.Connection);
					connInfo.DbSyncInfo = dbSyncInfo;
				}
			}
		}

        /// <summary>
        /// Remove all the records where work_in_progress flag is set.
        /// </summary>
        /// <returns></returns>
        public void RemoveSyncData(string syncTable, string cacheName)
        {
            lock (this)
            {
                IEnumerator em = Keys.GetEnumerator();
                while (em.MoveNext())
                {
                    DbResourceInfo connInfo = (DbResourceInfo)GetResource((string)em.Current);

                    RemoveTableData(syncTable, cacheName, connInfo.Connection);
                    connInfo.DbSyncInfo = null;
                }
            }

        }

        private bool RemoveTableData(string syncTable, string cacheName, IDbConnection connection)
        {
            object[] tableInfo = new object[] { syncTable, cacheName };

            SqlCommand command = null;

            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                if (connection is SqlConnection)
                {
                    command = ((SqlConnection)connection).CreateCommand();
                    command.CommandText = string.Format(CultureInfo.InvariantCulture, "DELETE FROM {0} WHERE CACHE_ID = '{1}' AND WORK_IN_PROGRESS = 1", tableInfo);
                    command.CommandType = CommandType.Text;

                    command.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception ex)
            {
                NCacheLog.Error(cacheName, ex.ToString());                
            }
            return false;
        }

		/// <summary>
		/// Remove all the stored sync information
		/// </summary>
		public void FlushSyncData()
		{
			lock (this)
			{
				IEnumerator em = Keys.GetEnumerator();
				while (em.MoveNext())
				{
					DbResourceInfo connInfo = (DbResourceInfo) GetResource((string)em.Current);
					connInfo.DbSyncInfo = null;
				}
			}		
		}


        /// <summary>
        /// Load the modified records for the given cache and set these flags to false
        /// </summary>
        /// <returns></returns>
        private Hashtable LoadTableData(string syncTable, string cacheName, IDbConnection connection)
        {   
            object[] tableInfo = new object[] { syncTable, cacheName };
            Hashtable tableData = new Hashtable();

            IDataReader reader = null;
            IDbCommand command = null;

            string cacheKey = "";
            bool modified = false;
            IDbTransaction transaction = connection.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);

            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                command = connection.CreateCommand();
                command.CommandText = string.Format(CultureInfo.InvariantCulture, "UPDATE {0} SET WORK_IN_PROGRESS = 1 WHERE CACHE_ID = '{1}' AND MODIFIED = 1", tableInfo);
                command.CommandType = CommandType.Text;
                command.Transaction = transaction;

                reader = command.ExecuteReader();
            }
            catch (Exception ex)
            {
                NCacheLog.Error(cacheName, ex.ToString());
                transaction.Rollback();
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }
            
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                command = connection.CreateCommand();
                command.CommandText = string.Format(CultureInfo.InvariantCulture, "SELECT CACHE_KEY, MODIFIED FROM {0} WHERE CACHE_ID = '{1}' AND WORK_IN_PROGRESS = 1", tableInfo);
                command.CommandType = CommandType.Text;
                command.Transaction = transaction;

                reader = command.ExecuteReader();
                //Infact we are doing nothing with this flag. reader.Read() will take care of it

                while (reader.Read())
                {
                    cacheKey = Convert.ToString(reader.GetValue(0));
                    modified = Convert.ToBoolean(reader.GetValue(1));
                    tableData.Add(cacheKey, modified);
                }
            }
            catch (Exception ex)
            {
                NCacheLog.Error(cacheName, ex.ToString());
                transaction.Rollback();
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                    reader = null;
                }
            }

            transaction.Commit();

            return tableData;
        }


        /// <summary>
        /// Gets the keys which have been modified in the database.
        /// call this method after acquiring the latest database state.
        /// </summary>
        /// <returns> array list of all the modified keys. </returns>
        internal ClusteredArrayList GetExpiredKeys()
        {
            ClusteredArrayList keys = new ClusteredArrayList();
            lock (this)
            {
                IEnumerator em = Keys.GetEnumerator();
                while (em.MoveNext())
                {
                    DbResourceInfo connInfo = (DbResourceInfo)GetResource((string)em.Current);
                    if (connInfo != null && connInfo.DbSyncInfo != null)
                    {
                        keys.AddRange(connInfo.DbSyncInfo.Keys);
                        connInfo.DbSyncInfo = null;
                    }
                }
            }
            return keys;
        }
    }
}

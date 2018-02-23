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

using System;
using Alachisoft.NCache.Caching.Util;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Collections;
using System.Globalization;

using Alachisoft.NCache.Common.Logger;

using Alachisoft.NCache.Common.DataStructures.Clustered;


namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// Summary description for CacheDbSyncManager.
	/// </summary>
	internal class CacheDbSyncManager
	{
        /// <summary>
        /// Keeps the information about the polling based database dependencies.
        /// two type of information are kept. 
        /// 1. what is the cache key to map with the provide database key.
        /// 2. what is the actual hint. this helps us to know important properties of hint like
        ///    needs_resync etc.
        /// </summary>
        internal class DependencyInformation
        {
            private string _cacheKey;
            private ExpirationHint _hint;
           

            internal DependencyInformation(string key, ExpirationHint hint)
            {
                this._cacheKey = key;
                this._hint = hint;
            }

            internal string CacheKey
            {
                get { return _cacheKey; }
            }

            internal ExpirationHint Hint
            {
                get { return _hint; }
            }
        }

        Hashtable               _connStringTable;
		OleDbConnectionPool     _oledbConPool;
		DbConnectionPool        _sqlConPool;
        Hashtable               _tblDepCount;
		string                  _cacheID = null;
        private ILogger          _ncacheLog;
        private Hashtable       _dbKeysMappingTbl;
        public const string SYNC_TABLE = "ncache_db_sync";

        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        public Hashtable TblDepCount
        {
            get { return _tblDepCount; }
        }

        public Hashtable ConnectoinStringTable { get { return _connStringTable; } }

        public void AddDBMapping(string dbKey, string cacheKey, ExpirationHint hint)
        {
            lock (_dbKeysMappingTbl.SyncRoot)
            {
                _dbKeysMappingTbl[dbKey] = new DependencyInformation(cacheKey, hint);
            }
        }
        
        public void RemoveDBMapping(string dbKey)
        {
            lock (_dbKeysMappingTbl.SyncRoot)
            {
                _dbKeysMappingTbl.Remove(dbKey);
            }
        }

		public CacheDbSyncManager(ILogger NCacheLog)
		{
            this._ncacheLog = NCacheLog;
            _oledbConPool = new OleDbConnectionPool(NCacheLog);
            _sqlConPool = new DbConnectionPool(NCacheLog);
            _connStringTable = new Hashtable();
            _connStringTable.Add("oledb", new Hashtable());
            _connStringTable.Add("sql", new Hashtable());
            _dbKeysMappingTbl = Hashtable.Synchronized(new Hashtable());            

		}

		public string GetCacheID(string instanceName)
		{
			if (_cacheID == null)
			{
                if (instanceName.IndexOf("_BK_NODE_") != -1)
                    _cacheID = instanceName.Substring(0, instanceName.IndexOf("_BK_NODE_"));
                else
                    _cacheID = instanceName;
			}

			return _cacheID;
		}

        /// <summary>
        /// does a lowercase comparison and returns true if the connection string already exists.
        /// </summary>
        /// <param name="connString"></param>
        /// <returns>True if connection string already exists otherwise returns false.</returns>
        public bool ConnectionStringExists(string connString)
        {
			return (((Hashtable)ConnectoinStringTable["sql"]).ContainsKey(connString.ToLower()) ||
				((Hashtable)ConnectoinStringTable["oledb"]).ContainsKey(connString.ToLower()));

		}


        /// <summary>
        /// Adds the connection string to the table.
        /// </summary>
        /// <param name="connString">Connection String</param>
        /// <param name="isSql">True if connection string is for SQL Cache Dependency.</param>
        public void AddConnectionString(string connString, bool isSql)
        {
            if (_connStringTable != null)
            {
                lock (_connStringTable.SyncRoot)
                {
                    if (isSql)
                    {
                        if (!((Hashtable)_connStringTable["sql"]).ContainsKey(connString.ToLower()))
                        {
							((Hashtable)_connStringTable["sql"]).Add(connString.ToLower(), connString);
                        }
                    }
                    else
                    {
                        if (!((Hashtable)_connStringTable["oledb"]).ContainsKey(connString.ToLower()))
                        {
							((Hashtable)_connStringTable["oledb"]).Add(connString.ToLower(), connString);
                        }
                    }
                }
            }
        }

		public OleDbConnection AddToOleDbConnectionPool(string connectionString)
		{
           return _oledbConPool.PoolConnection(connectionString, new OleDbConnection(connectionString));
		}

		public void RemoveFromOleDbConnectionPool(string connectionString)
		{
			_oledbConPool.RemoveConnection(connectionString.ToLower());
		}

		public IDbConnection AddToSqlConnectionPool(string connectionString)
		{
           return _sqlConPool.PoolConnection(connectionString, new SqlConnection(connectionString));
            
		}

		public void RemoveFromSqlConnectionPool(string connectionString)
		{
			_sqlConPool.RemoveConnection(connectionString.ToLower());
		}

		public void AcquireSyncData()
		{
            string connString = string.Empty;           

			if (_cacheID == null) return;

			try
			{
                IDictionaryEnumerator oleDbEn = ((Hashtable)_connStringTable["oledb"]).GetEnumerator();
                while(oleDbEn.MoveNext())
                {
                    connString = oleDbEn.Value.ToString();
					AddToOleDbConnectionPool(oleDbEn.Value.ToString());
                }

                _oledbConPool.AcquireSyncData(SYNC_TABLE, _cacheID);
			}
            catch (OleDbException s)
            {
                if (s.Message.Contains("SQL Server does not exist or access denied"))
                {
                    if (((Hashtable)_connStringTable["oledb"]).ContainsKey(connString.ToLower()))
                    {
                        ((Hashtable)_connStringTable["oledb"]).Remove(connString.ToLower());
                    }
                    RemoveFromOleDbConnectionPool(connString);
                    NCacheLog.Error("CacheDbSyncManager", s.ToString());
                }
                else
                    throw;
            }
			catch(Exception e)
			{
				NCacheLog.Error("CacheDbSyncManager", e.ToString());
			}

			try
			{
                connString = string.Empty;
                IDictionaryEnumerator sqlEn = ((Hashtable)_connStringTable["sql"]).GetEnumerator();
                while(sqlEn.MoveNext())
                {
                    connString = sqlEn.Value.ToString();
					AddToSqlConnectionPool(sqlEn.Value.ToString());
                }

                _sqlConPool.AcquireSyncData(SYNC_TABLE, _cacheID);
			}
            catch (OleDbException s)
            {
                if (s.Message.Contains("SQL Server does not exist or access denied"))
                {
                    if (((Hashtable)_connStringTable["sql"]).ContainsKey(connString.ToLower()))
                    {
                        ((Hashtable)_connStringTable["sql"]).Remove(connString.ToLower());
                    }
                    RemoveFromOleDbConnectionPool(connString);
                    NCacheLog.Error("CacheDbSyncManager", s.ToString());
                }
                else
                    throw;
            }

			catch(Exception e)
			{
				NCacheLog.Error("CacheDbSyncManager", e.ToString());
			}
		}

        public void RemoveSyncData()
        {
            if (_cacheID == null) return;

            try
            {
                _oledbConPool.RemoveSyncData(SYNC_TABLE, _cacheID);
            }
            catch (Exception e)
            {
                NCacheLog.Error("CacheDbSyncManager", e.ToString());
            }

            try
            {
                _sqlConPool.RemoveSyncData(SYNC_TABLE, _cacheID);
            }
            catch (Exception e)
            {
                NCacheLog.Error("CacheDbSyncManager", e.ToString());
            }
        }

        /// <summary>
        /// Removes all the connections from the oleDb and sql connection pools.
        /// </summary>
        public void ClearResourcePools()
        {
            _oledbConPool.RemoveAllResources();
            _sqlConPool.RemoveAllResources();
        }

		/// <summary>
		/// Clear the connection pools
		/// </summary>
		public void Clear()
		{
			RemoveAllResources();
		}


		/// <summary>
		/// Remove the related records from ncache_db_sync table
		/// </summary>
        private void RemoveAllResources()
        {
            object[] tableInfo = { SYNC_TABLE, _cacheID };

            lock (_oledbConPool)
            {
                OleDbConnection connection;

                IEnumerator em = _oledbConPool.Keys.GetEnumerator();
                while (em.MoveNext())
                {
                    OleDbDataReader reader = null;
                    OleDbCommand command ;

                    try
                    {
                        OleDbConnectionPool.SqlDbResourceInfo connInfo = (OleDbConnectionPool.SqlDbResourceInfo)_oledbConPool.GetResource((string)em.Current);
                        connection = connInfo.Connection;

                        command = connection.CreateCommand();
                        command.CommandText = string.Format(CultureInfo.InvariantCulture, "DELETE FROM {0} WHERE CACHE_ID = '{1}'", tableInfo);
                        command.CommandType = CommandType.Text;

                        reader = command.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        NCacheLog.Error(ex.ToString());
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
                }
                _oledbConPool.RemoveAllResources();
                //clear the table for dependency count.
                _tblDepCount = null;
            }


            lock (_sqlConPool)
            {
                IDbConnection connection = null;

                IEnumerator em = _sqlConPool.Keys.GetEnumerator();
                while (em.MoveNext())
                {
                    IDataReader reader = null;
                    IDbCommand command = null;

                    try
                    {
                        DbConnectionPool.DbResourceInfo connInfo = (DbConnectionPool.DbResourceInfo)_sqlConPool.GetResource((string)em.Current);
                        connection = connInfo.Connection;

                        command = connection.CreateCommand();
                        command.CommandText = string.Format(CultureInfo.InvariantCulture, "DELETE FROM {0} WHERE CACHE_ID = '{1}'", tableInfo);
                        command.CommandType = CommandType.Text;

                        reader = command.ExecuteReader();
                    }
                    catch (Exception ex)
                    {
                        NCacheLog.Error(ex.ToString());
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
                }

                _sqlConPool.RemoveAllResources();
            }
        }


		/// <summary>
		/// Remove the data kept for synchronization
		/// </summary>
		public void FlushSyncData()
		{
			try
			{
				_oledbConPool.FlushSyncData();
			}
			catch(Exception e)
			{
				NCacheLog.Error("CacheDbSyncManager", e.ToString());
			}

			try
			{
				_sqlConPool.FlushSyncData();
			}
			catch(Exception e)
			{
				NCacheLog.Error("CacheDbSyncManager", e.ToString());
			}		
		}

		/// <summary>
		/// Check if the current object has expired
		/// </summary>
		/// <returns></returns>
		public bool IsOleDbItemExpired(string cacheKey, string connectionString)
		{
            IDictionary dbSyncData = _oledbConPool.GetResourceSyncInfo(connectionString);
            if (dbSyncData != null && dbSyncData.Contains(cacheKey))
            {
                return true;
            }
            return false;
        }

		/// <summary>
		/// Check if the current object has expired
		/// </summary>
		/// <returns></returns>
		public bool IsSqlItemExpired(string cacheKey, string connectionString)
		{
            int count = 0;
            IDictionary dbSyncData = _sqlConPool.GetResourceSyncInfo(connectionString);
            if (dbSyncData != null && dbSyncData.Contains(cacheKey))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// maintains a table to indicate number of dependency objects 
        /// based on the same key.
        /// </summary>
        /// <param name="cacheKey"></param>
        public void UpdateDepCount(string cacheKey)
        {
            if (_tblDepCount == null)
                _tblDepCount = new Hashtable();

            if (_tblDepCount.Contains(cacheKey))
            {
                int depCount = Convert.ToInt32(_tblDepCount[cacheKey]);
                depCount++;
                _tblDepCount[cacheKey] = depCount;
            }
            else
            {
                _tblDepCount[cacheKey] = 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>
        /// returns a hashtable containing 2 keys. 
        /// 1. expire-items
        /// 2. resync-items
        /// against these 2, associated cache keys are returned.
        /// </returns>
        internal IDictionary GetExpiredKeys()
        {
            ClusteredArrayList sqlKeyList = _sqlConPool.GetExpiredKeys();
            sqlKeyList.AddRange(_oledbConPool.GetExpiredKeys());

            if (sqlKeyList != null && sqlKeyList.Count > 0)
            {
                HashVector cacheKeyList = new HashVector();
                ClusteredArrayList expire = new ClusteredArrayList();
                ClusteredArrayList resync = new ClusteredArrayList();

                IEnumerator ie = sqlKeyList.GetEnumerator();
                lock (_dbKeysMappingTbl.SyncRoot)
                {
                    while (ie.MoveNext())
                    {
                        if (_dbKeysMappingTbl.Contains(ie.Current))
                        {
                            DependencyInformation depInfo = _dbKeysMappingTbl[ie.Current] as DependencyInformation;
                            if (depInfo != null)
                            {
                                if (depInfo.Hint.NeedsReSync) resync.Add(depInfo.CacheKey);
                                else expire.Add(depInfo.CacheKey);
                            }
                        }
                    }

                    cacheKeyList["expire-items"] = expire;
                    cacheKeyList["resync-items"] = resync;
                    return cacheKeyList;
                }
            }

            return new HashVector();
        }
    }
}


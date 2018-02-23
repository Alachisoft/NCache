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
using System.Data;
using System.Globalization;     

using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
	/// <summary>
	/// A polling based dependency. When notification is enabled for a table 
	/// by the user (using AspNet_regSql utility), a table named 'AspNet_SqlCacheTablesForChangeNotification'
	/// is created. This table contains the name of every table for which notification
	/// is enabled and an associated changeId. Each time the table is changed, its changedId 
	/// is incremented. This table is polled to check for any changes.
	/// </summary>
	[Serializable]

    public class DBCacheDependency : DependencyHint, ICompactSerializable
	{
		[CLSCompliant(false)]
		protected string              _dbCacheKey = "";
		[CLSCompliant(false)]
		protected string              _cacheName  = "";
		[CLSCompliant(false)]
		protected string              _connectionString = "";
		
		[NonSerialized] internal CacheDbSyncManager  _cdbSyncManager = null;
		[NonSerialized] internal IDbConnection       _connection = null;

        private const int DBCacheDependencySize = 16;

		/// <summary>
		/// Add or Update the record related to the given cacheKey
		/// </summary>
		/// <returns></returns>
		internal bool SetSyncData()
		{
            string modifiedStr = "0";
            object[] tableInfo = new object[] { CacheDbSyncManager.SYNC_TABLE, _dbCacheKey, _cacheName, modifiedStr };
			IDataReader reader = null;
			IDbCommand command = null;

            bool exists = false;
            object modified = null;

            lock (_connection)
            {
                IDbTransaction trans = _connection.BeginTransaction(IsolationLevel.RepeatableRead);

                try
                {
                    command = _connection.CreateCommand();
                    command.CommandText = string.Format(CultureInfo.InvariantCulture, "SELECT MODIFIED FROM {0} WHERE CACHE_KEY = '{1}' AND CACHE_ID = '{2}'", tableInfo);
                    command.CommandType = CommandType.Text;
                    command.Transaction = trans;

                    if (_connection.State != ConnectionState.Open)
                        _connection.Open();

                    reader = command.ExecuteReader();
                  
                    if (reader.Read())
                    {
                        modified = reader.GetValue(0);
                        exists = true;
                    }
                }
                catch (Exception ex)
                {
                    NCacheLog.Error("DBCacheDependency", ex.ToString());
                    exists = false;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                        reader = null;
                    }

                    if (command != null)
                    {
                        command.Dispose();
                        command = null;
                    }
                }

                if (!exists)
                {
                    try
                    {
                        command = _connection.CreateCommand();
                        command.CommandText = string.Format(CultureInfo.InvariantCulture, "INSERT INTO {0} VALUES ('{1}', '{2}', {3}, 0)", tableInfo);
                        command.CommandType = CommandType.Text;
                        command.Transaction = trans;

                        if (_connection.State != ConnectionState.Open)
                            _connection.Open();

                        reader = command.ExecuteReader();
                        exists = true;
                    }
                    catch (Exception ex)
                    {
                        NCacheLog.Error("DBCacheDependency", ex.ToString());
                        trans.Rollback();
                        return false;
                    }
                    finally
                    {
                        if (reader != null)
                        {
                            reader.Close();
                            reader.Dispose();
                            reader = null;
                        }

                        if (command != null)
                        {
                            command.Dispose();
                            command = null;
                        }
                    }
                }
                else
                {
                    if (Convert.ToBoolean(modified) == true)
                    {
                        exists = false;
                        try
                        {
                            command = _connection.CreateCommand();
                            command.CommandText = string.Format(CultureInfo.InvariantCulture, "UPDATE {0} SET MODIFIED = {3}, WORK_IN_PROGRESS = {3} WHERE CACHE_KEY = '{1}' AND CACHE_ID = '{2}'", tableInfo);
                            command.CommandType = CommandType.Text;
                            command.Transaction = trans;

                            if (_connection.State != ConnectionState.Open)
                                _connection.Open();

                            reader = command.ExecuteReader();
                            exists = true;
                        }
                        catch (Exception ex)
                        {
                            NCacheLog.Error("DBCacheDependency", ex.ToString());
                            trans.Rollback();
                            return false;
                        }
                        finally
                        {
                            if (reader != null)
                            {
                                reader.Close();
                                reader.Dispose();
                                reader = null;
                            }

                            if (command != null)
                            {
                                command.Dispose();
                                command = null;
                            }
                        }
                    }
                }
                trans.Commit();
            }

			return exists;
		}

		/// <summary>
		/// Remove the related record from ncache_db_sync table
		/// </summary>
		/// <returns></returns>
		private bool RemoveSyncData()
		{
            object[] tableInfo = new object[] { CacheDbSyncManager.SYNC_TABLE, _cacheName, _dbCacheKey };

			IDbCommand command = null;

            try
            {
                command = _connection.CreateCommand();
                command.CommandText = string.Format(CultureInfo.InvariantCulture, "DELETE FROM {0} WHERE CACHE_ID = '{1}' AND CACHE_KEY = '{2}'", tableInfo);
                command.CommandType = CommandType.Text;

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                NCacheLog.Error("DBCacheDependency", ex.ToString());
                return false;
            }
            finally
            {
                if (command != null)
                {
                    command.Dispose();
                    command = null;
                }
            }
		}
        
        /// <summary>
		/// Base class uses this property to check for changes.
		/// Returns true if the table (on which dependency was created) has changed.
		/// </summary>
		public override Boolean HasChanged
		{
            get
            {
                return false;
            }
		}

        /// <summary>
		/// Dispose
		/// </summary>
		protected override void DisposeInternal()
		{
			if (_cdbSyncManager != null)
			{
				RemoveSyncData();
                _cdbSyncManager.RemoveDBMapping(_dbCacheKey);

                if (_hintType == ExpirationHintType.Sql7CacheDependency)
                    _cdbSyncManager.RemoveFromSqlConnectionPool(_connectionString);
                else
                    _cdbSyncManager.RemoveFromOleDbConnectionPool(_connectionString);
			}
		}

        #region ICompactSerializable Members

        void ICompactSerializable.Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);
            CacheKey = reader.ReadString();
            _dbCacheKey = reader.ReadString();
            _cacheName = reader.ReadString();
            _connectionString = reader.ReadString();
        }

        void ICompactSerializable.Serialize(CompactWriter writer)
        {
            base.Serialize(writer);
            writer.Write(CacheKey);
            writer.Write(_dbCacheKey);
            writer.Write(_cacheName);
            writer.Write(_connectionString);
        }

        #endregion

        #region ISizable Members

        public override int Size
        {
            get { return base.Size + DBDependencySize; }
        }

        public override int InMemorySize
        {
            get 
            { 
                int inMemorySize=this.Size;
                
                inMemorySize += inMemorySize <= 24 ? 0 : Common.MemoryUtil.NetOverHead;

                return inMemorySize;
            }
        }

        private int DBDependencySize
        {
            get 
            {
                int temp = 0;
                if (this._dbCacheKey != null)
                    temp += Common.MemoryUtil.GetStringSize(this._dbCacheKey);
                
                if (this._cacheName != null)
                    temp += Common.MemoryUtil.GetStringSize(this._cacheName);

                if (this._connectionString != null)
                    temp += Common.MemoryUtil.GetStringSize(this._connectionString);

                return temp+DBCacheDependencySize;               		
            }
        }

        #endregion

    }
}


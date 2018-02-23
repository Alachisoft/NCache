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
using System.Data.OleDb;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    [Serializable]

    public class OleDbCacheDependency : DBCacheDependency
    {
        public OleDbCacheDependency()
        {         
            base._hintType = ExpirationHintType.OleDbCacheDependency;
        }
        
        /// <summary>
        /// Creates a polling based OleDbCacheDependency object.
        /// </summary>
        /// <param name="connectionString"></param>
        public OleDbCacheDependency(string connectionString, string pk)
        {
            _connectionString = connectionString;
            _dbCacheKey = pk;
            base._hintType = ExpirationHintType.OleDbCacheDependency;
        }

        public string ConnectionString
        {
            get { return this._connectionString; }
        }

        public string DbCacheKey
        {
            get { return this._dbCacheKey; }
        }



        /// <summary>
        /// Reset dependency settings
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override bool Reset(CacheRuntimeContext context)
        {
            base.Reset(context);

            if (_connectionString != null)
            {
                if (!context.ExpiryMgr.CacheDbSyncManager.ConnectionStringExists(_connectionString))
                {
                    context.ExpiryMgr.CacheDbSyncManager.AddConnectionString(_connectionString, false);
                    context.CacheImpl.ReplicateConnectionString(_connectionString, false);
                }

                if(context.IsDbSyncCoordinator) return Initialize(context);
            }

            return false;
        }

        /// <summary>
        /// Initializes the dependency object.
        /// </summary>		
        /// <param name="context"></param>
        /// <returns></returns>
        private bool Initialize(CacheRuntimeContext context)
        {
            try
            {
                _cdbSyncManager = context.ExpiryMgr.CacheDbSyncManager;
                _cacheName = _cdbSyncManager.GetCacheID(context.CacheImpl.Name);
                _connection = _cdbSyncManager.AddToOleDbConnectionPool(_connectionString);
                if (SetSyncData())
                {
                    _cdbSyncManager.AddDBMapping(_dbCacheKey, CacheKey, this);
                    return true;
                }
            }
            catch (OleDbException s)
            {
                if (s.Message.Contains("SQL Server does not exist or access denied"))
                    _cdbSyncManager.RemoveFromOleDbConnectionPool(_connectionString);
            }
            return false;
        }

        public override string ToString()
        {
            return "OLEDBDEPENDENCY \"" + _connectionString + "\"" + _dbCacheKey + "\"\r\n";
        }
    }
}
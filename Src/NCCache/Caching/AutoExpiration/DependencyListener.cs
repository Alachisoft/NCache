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
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Caching.AutoExpiration
{
    /// <summary>
    /// Base class for dependency listeners.        
    /// </summary>
    public class DependencyListener : IDisposable
    {
        protected IDBConnectionPool _connectionPool;
        protected IDependencyChangeListener _dependencyListener;
        protected string _cacheKey;
        protected string _connString;
        protected string _queryString;
        protected ExpirationHintType _hintType;
        protected ILogger _ncacheLog;

        private object _syncLock = new object();

        protected ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

        internal ExpirationHintType ExpHintType
        {
            get { return _hintType; }
        }

        internal string ConnString
        {
            get { return _connString; }
        }

        internal string QueryString
        {
            get { return _queryString; }
        }

        /// <summary>
        /// Initialize instance of dependency listener
        /// </summary>
        /// <param name="key">key used to reference object</param>
        /// <param name="connString">connection string used to connect database</param>
        /// <param name="queryString">query string for which dataset is created to be monitored</param>
        /// <param name="context">current cache runtime context</param>
        /// <param name="hintType">expiration hint type</param>
        protected DependencyListener(string key, string connString, string queryString, IDBConnectionPool connectionPool, IDependencyChangeListener dependencyListener, ILogger logger, ExpirationHintType hintType)
        {
            _cacheKey = key;
            _connString = connString;
            _queryString = queryString;
            _hintType = hintType;
            _ncacheLog = logger;
            _connectionPool = connectionPool;
            _dependencyListener = dependencyListener;
        }

        /// <summary>
        /// CacheKey that is the key of the listener table as well.
        /// </summary>
        internal string CacheKey
        {
            get { return _cacheKey; }
        }

        /// <summary>
        /// Initializes the dependency instance. registers the change event handler for it.
        /// </summary>
        /// <returns>true if the dependency was successfully initialized.</returns>
        public virtual bool Initialize()
        {
            return false;
        }

        /// <summary>
        /// Stop notification listening
        /// </summary>
        public virtual void Stop()
        {
            this._connectionPool.RemoveFromDbConnectionPool(_connString);
        }

        /// <summary>
        /// Called when dataset is changed.
        /// </summary>
        /// <param name="changed">fired because data is changed</param>
        /// <param name="restart">fired because server is restarted</param>
        /// <param name="error">fired because of any error</param>
        /// <param name="error">fired because of invalid query</param>
        protected void OnDependencyChanged(bool changed, bool restart, bool error, bool invalid)
        {
            if (_dependencyListener != null)
                _dependencyListener.OnDependencyChanged(this, _cacheKey, changed, restart, error, invalid);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Stop();
        }

        #endregion
    }

}

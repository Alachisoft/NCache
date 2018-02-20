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

namespace Alachisoft.NCache.Web.NOutputCache
{
    /// <summary>
    /// Holds output cache settings, that are read from config file
    /// </summary>
    internal class OutputCacheSettings
    {
        private string _cacheName = string.Empty;
        private string _clientCacheName = string.Empty;
        private bool _exceptionsEnabled = false;
        private bool _enableLogs = false;
        private bool _enableDetailedLogs = false;

        /// <summary>
        /// Get or set the cache name
        /// </summary>
        public string CacheName
        {
            get { return this._cacheName; }
            set { this._cacheName = value; }
        }

    
        /// <summary>
        /// Get or set exceptions enabled flag
        /// </summary>
        public bool ExceptionsEnabled
        {
            get { return this._exceptionsEnabled; }
            set { this._exceptionsEnabled = value; }
        }

        /// <summary>
        /// Get or set enable loging flag
        /// </summary>
        public bool EnableLogs
        {
            get { return this._enableLogs; }
            set { this._enableLogs = value; }
        }

        /// <summary>
        /// Get or set enable detailed loging flag
        /// </summary>
        public bool EnableDetailedLogs
        {
            get { return this._enableDetailedLogs; }
            set { this._enableDetailedLogs = value; }
        }
    }
}
// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.Runtime;


namespace Alachisoft.NCache.Web.Caching.APILogging
{

    internal class APILogItem
    {
        private string _signature = null;

        private string _key = null;

        private int? _noOfObjectsReturned = null;

        /// <summary> Absolute expiration for the object. </summary>
        private DateTime? _abs = null;
        /// <summary> Sliding expiration for the object. </summary>
        private TimeSpan? _sld = null;

        private CacheItemPriority? _p = null;

        private int _noOfKeys = -1;

        private string _query = null;
        private System.Collections.IDictionary _queryValues = null;

        private TimeSpan? _lockTimeout = null;
        private bool? _acquireLock = null;

        private string _exceptionMessage = null;
        private RuntimeAPILogItem _rtAPILogItem = null;
        private DateTime _loggingTime;

        public DateTime LoggingTime
        {
            get { return _loggingTime; }
            set { _loggingTime = value; }
        }
        
        
        public RuntimeAPILogItem RuntimeAPILogItem
        {
            get { return _rtAPILogItem; }
            set { _rtAPILogItem = value; }
        }

        public APILogItem()
        {
        }

        public APILogItem(string key, string exceptionMessage)
        {
            _key = key;
            this.ExceptionMessage = exceptionMessage;
        }

        public APILogItem(string key, CacheItem item , string exceptionMessage)
        {
            _key = key;

            _abs = item.AbsoluteExpiration;
            _sld = item.SlidingExpiration;
            _p = item.Priority;
            this.ExceptionMessage = exceptionMessage;
        }

        /// <summary>
        /// Get or set the sinature of API call
        /// </summary>
        public string Signature
        {
            get { return _signature; }
            set { _signature = value; }
        }

        /// <summary>
        /// Get or set the key
        /// </summary>
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        /// <summary>
        /// Get or set the number of keys
        /// </summary>
        public int NoOfKeys
        {
            get { return _noOfKeys; }
            set { _noOfKeys = value; }
        }

        public int? NoOfObjectsReturned
        {
            get { return _noOfObjectsReturned; }
            set { _noOfObjectsReturned = value; }
        }

        /// <summary>
        /// Get or set the absolute expiraion date and time
        /// </summary>
        public DateTime? AbsolueExpiration
        {
            get { return _abs; }
            set { _abs = value; }
        }

        /// <summary>
        /// Get or set the sliding expiration timespan
        /// </summary>
        public TimeSpan? SlidingExpiration
        {
            get { return _sld; }
            set { _sld = value; }
        }

        /// <summary>
        /// Get or set the priority
        /// </summary>
        public CacheItemPriority? Priority
        {
            get { return _p; }
            set { _p = value; }
        }



        public string Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public System.Collections.IDictionary QueryValues
        {
            get { return _queryValues; }
            set { _queryValues = value; }
        }

        public TimeSpan? LockTimeout
        {
            get { return _lockTimeout; }
            set { _lockTimeout = value; }
        }

        public bool? AcquireLock
        {
            get { return _acquireLock; }
            set { _acquireLock = value; }
        }


        public string ExceptionMessage
        {
            get { return _exceptionMessage; }
            set
            {
                _exceptionMessage = value;
                if (_exceptionMessage != null)
                {
                    _exceptionMessage = _exceptionMessage.Replace('\r', ' ');
                    _exceptionMessage = _exceptionMessage.Replace('\n', ' ');
                }
            }
        }
    }
}

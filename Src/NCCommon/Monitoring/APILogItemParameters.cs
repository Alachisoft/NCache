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
using Alachisoft.NCache.Runtime;
using System.Collections;

namespace Alachisoft.NCache.Common.Monitoring
{
    [Serializable]
    public class APILogItemParameters
    {

        private string _signature = null;

        private string _key = null;

        private int? _noOfObjectsReturned = null;

        /// <summary> Absolute expiration for the object. </summary>
        private DateTime? _abs = null;
        /// <summary> Sliding expiration for the object. </summary>
        private TimeSpan? _sld = null;

        private CacheItemPriority? _p = null;
        private string _group = null;
        private string _subGroup = null;

        private int _noOfKeys = -1;
        
        private long _version;
        private string _providerName = null;
        private string _resyncProviderName = null;

        private string _query = null;
        private System.Collections.IDictionary _queryValues = null;

        private TimeSpan? _lockTimeout = null;
        private bool? _acquireLock = null;

        private string _exceptionMessage = null;
        private DateTime _loggingTime;

        #region ServerAPILogging

        private string[]  _keys; 
        private object _value;
        private Hashtable _serverTags;
        private Hashtable _serverNamedTags;
        private Hashtable _serverQueryInfo;
        private long _absExpiration;
        private long _sldExpiration;
        private string _dsWriteOption;
        private string _dsReadOption;
        private string _streamMode;
        private string _tag;
        private string _lockHandle;
        private bool _isResyncRequired;
        private string _accessType;
        private bool _getData;
        private int _chunkSize;
        private string _taskId;
        private string _readProviderName;
        private string _extractor;
        private string _aggregator;
        private string _dataSourceClearedCallback;
        private string _onAsyncCacheClearCallback;
        private string _task;
        private string _keyfilter;
        private int _timeOut;
        byte[] _parameters;
        object[] _arguments;
        string _writeProviderName;
        object _notifId;
        object _data;


        public string DataSourceClearedCallback
        {
            get { return _dataSourceClearedCallback; }
            set { _dataSourceClearedCallback = value; }
        }

        public string OnAsyncCacheClearCallback
        {
            get { return _onAsyncCacheClearCallback; }
            set { _onAsyncCacheClearCallback = value; }
        }



        public object NotifID
        {
            get { return _notifId; }
            set { _notifId = value; }
        }
        public object Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public string WriteProviderName
        {
            get { return _writeProviderName; }
            set { _writeProviderName = value; }
        }

        public string Task
        {
            get { return _task; }
            set { _task = value; }
        }

        public byte[] Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        public object [] Arguments
        {
            get { return _arguments; }
            set { _arguments = value; }
        }
        
        public string ReadProviderName
        {
            get { return _readProviderName; }
            set { _readProviderName = value; }
        }


        public string Extractor
        {
            get { return _extractor; }
            set { _extractor = value; }
        }

        public string Aggregator
        {
            get { return _aggregator; }
            set { _aggregator = value; }
        }
        public int Timeout
        {
            get { return _timeOut; }
            set { _timeOut = value; }
        }

        public string Keyfilter
        {
            get { return _keyfilter; }
            set { _keyfilter = value; }
        }


        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }
        public string TaskID
        {
            get { return _taskId; }
            set { _taskId = value; }
        }
        public int ChunkSize
        {
            get { return _chunkSize; }
            set { _chunkSize = value; }
        }

        public bool GetData
        {
            get { return _getData; }
            set { _getData = value; }
        }

        public string AccessType
        {
            get { return _accessType; }
            set { _accessType = value; }
        }
        public bool IsResyncRequired
        {
            get { return _isResyncRequired; }
            set { _isResyncRequired = value; }
        }
        public string LockHandle
        {
            get { return _lockHandle; }
            set { _lockHandle = value; }
        }

        public Hashtable Tags
        {
            get { return _serverTags; }
            set { _serverTags = value; }
        }

        public string Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        public Hashtable NamedTags
        {
            get { return _serverNamedTags; }
            set { _serverNamedTags = value; }
        }
        public Hashtable QueryInfo
        {
            get { return _serverQueryInfo; }
            set { _serverQueryInfo = value; }
        }

        public long AbsoluteExpiration
        {
            get { return _absExpiration; }
            set { _absExpiration = value; }
        }

        public long SlidingExpiration
        {
            get { return _sldExpiration; }
            set { _sldExpiration = value; }
        }
        
        public string [] Keys
        {
            get { return _keys; }
            set { _keys = value; }
        }

        public APILogItemParameters(string exceptionMessage)
        {
            this.ExceptionMessage = exceptionMessage;
        }
        #endregion

        public APILogItemParameters()
        {
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
        /// Get or set the name of group
        /// </summary>
        public string Group
        {
            get { return _group; }
            set { _group = value; }
        }

        /// <summary>
        /// Get or set the name of subgroup
        /// </summary>
        public string SubGroup
        {
            get { return _subGroup; }
            set { _subGroup = value; }
        }
        /// <summary>
        /// Get or set the priority
        /// </summary>
        public CacheItemPriority? Priority
        {
            get { return _p; }
            set { _p = value; }
        }

        public string ProviderName
        {
            get { return _providerName; }
            set { _providerName = value; }
        }

        public string ResyncProviderName
        {
            get { return _resyncProviderName; }
            set { _resyncProviderName = value; }
        }

        public string DSWriteOption
        {
            get { return _dsWriteOption; }
            set { _dsWriteOption = value; }
        }

        public string DSReadOption
        {
            get { return _dsReadOption; }
            set { _dsReadOption = value; }
        }

        public string StreamMode
        {
            get { return _streamMode; }
            set { _streamMode = value; }
        }


        public string Query
        {
            get { return _query; }
            set { _query = value; }
        }

        public System.Collections.IDictionary Values
        {
            get { return _queryValues; }
            set { _queryValues = value; }
        }

        public long Version
        {
            get { return _version; }
            set { _version = value; }
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

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
using System.Collections;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
namespace Alachisoft.NCache.Caching.Util
{
    public class HotConfig : ICompactSerializable
    {
        private bool    _isErrorLogsEnabled;
        private bool    _isDetailedLogsEnabled;
        private long    _cacheMaxSize;
        private long    _cleanInterval;
        private float   _evictRatio;
        private bool _securityEnabled;
        private string _securityDomainController;
        private String _securityPort;
        
        private Hashtable _securityUsers;
        private bool _compressionEnabled;
        private bool    _isTargetCache;
        private long _compressionThreshold;
        private Hashtable _backingSource;
        
        public bool CompressionEnabled
        {
            get { return _compressionEnabled; }
            set { _compressionEnabled = value; }
        }

        public long CompressionThreshold
        {
            get { return _compressionThreshold; }
            set { _compressionThreshold = value; }
        }
        /// <summary>
        /// Registered Backing Source.
        /// </summary>
        public Hashtable BackingSource
        {
            get { return _backingSource; }
            set { _backingSource = value; }
        }

        public bool SecurityEnabled
        {
            get { return _securityEnabled; }
            set { _securityEnabled = value; }
        }

        public string SecurityDomainController
        {
            get { return _securityDomainController; }
            set { _securityDomainController = value; }
        }

        public String SecurityPort
        {
            get { return _securityPort; }
            set { _securityPort = value; }
        }

        public Hashtable SecurityUsers
        {
            get { return _securityUsers; }
            set { _securityUsers = value; }
        }

        public float EvictRatio
        {
            get { return _evictRatio; }
            set { _evictRatio = value; }
        }

        public long CacheMaxSize
        {
            get { return _cacheMaxSize; }
            set { _cacheMaxSize = value; }
        }

        /// <summary>Fatal anmd error logs.</summary>
        public bool IsErrorLogsEnabled
        {
            get { return _isErrorLogsEnabled; }
            set { _isErrorLogsEnabled = value; }
        }

        /// <summary>Info, warning, debug logs.</summary>
        public bool IsDetailedLogsEnabled
        {
            get { return _isDetailedLogsEnabled; }
            set { _isDetailedLogsEnabled = value; }
        }

        public long CleanInterval
        {
            get { return _cleanInterval; }
            set { _cleanInterval = value; }
        }

       

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_isErrorLogsEnabled + "\"");
            sb.Append(_isDetailedLogsEnabled + "\"");

            sb.Append(_compressionEnabled + "\"");
            sb.Append(_compressionThreshold + "\"");

            sb.Append(_cacheMaxSize + "\"");
            sb.Append(_cleanInterval + "\"");
            sb.Append(_evictRatio + "\"");

            sb.Append(_isTargetCache + "\"");
            sb.Append(_isTargetCache + "\"");
   

            return sb.ToString();
        }

        public static HotConfig FromString(string attributes)
        {
            if (attributes == null || attributes == string.Empty) return null;

            HotConfig config = new HotConfig();

            int beginQuoteIndex = 0;
            int endQuoteIndex = 0;
            
            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string errorLogs = attributes.Substring(beginQuoteIndex, endQuoteIndex - beginQuoteIndex);
            if (errorLogs != null && errorLogs != string.Empty)
                config._isErrorLogsEnabled = Convert.ToBoolean(errorLogs);

            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string detailedLogs = attributes.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
            if (detailedLogs != null && detailedLogs != string.Empty)
                config._isDetailedLogsEnabled = Convert.ToBoolean(detailedLogs);


            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string compression = attributes.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
            if (compression != null && compression != string.Empty)
                config._compressionEnabled = Convert.ToBoolean(compression);

            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string threshold = attributes.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
            if (threshold != null && threshold != string.Empty)
            {
                long kbs = Convert.ToInt64(threshold);
                // convert to bytes
                config._compressionThreshold = kbs * 1024;
            }

            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string size = attributes.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
            if (size != null && size != string.Empty)
                config._cacheMaxSize = Convert.ToInt64(size);

            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string interval = attributes.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
            if (interval != null && interval != string.Empty)
                config._cleanInterval = Convert.ToInt64(interval);

            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string evict = attributes.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
            if (evict != null && evict != string.Empty)
                config._evictRatio = Convert.ToSingle(evict);

            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string isTargetCache = attributes.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
            if (isTargetCache != null && isTargetCache != string.Empty)
                config._isTargetCache = Convert.ToBoolean(isTargetCache);

            return config;
        }

        private static void UpdateDelimIndexes(ref string attributes, char delim, ref int beginQuoteIndex, ref int endQuoteIndex)
        {
            beginQuoteIndex = endQuoteIndex;
            endQuoteIndex = attributes.IndexOf(delim, beginQuoteIndex + 1);
        }

        public void Deserialize(CompactReader reader)
        {
            IsErrorLogsEnabled = (reader.ReadBoolean());
            IsDetailedLogsEnabled = (reader.ReadBoolean());
            CacheMaxSize = (reader.ReadInt64());
            CleanInterval = (reader.ReadInt64());
            EvictRatio = (reader.ReadSingle());
            SecurityEnabled = (reader.ReadBoolean());
            SecurityDomainController = reader.ReadObject() as string;
            SecurityPort = reader.ReadObject() as string;
            SecurityUsers = reader.ReadObject() as Hashtable;
            CompressionEnabled = (reader.ReadBoolean());

            BackingSource = reader.ReadObject() as Hashtable;

        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(IsErrorLogsEnabled);
            writer.Write(IsDetailedLogsEnabled);
            writer.Write(CacheMaxSize);
            writer.Write(CleanInterval);
            writer.Write(EvictRatio);
            writer.Write(SecurityEnabled);
            writer.WriteObject(SecurityDomainController);
            writer.WriteObject(SecurityPort);
            writer.WriteObject(SecurityUsers);
            writer.Write(CompressionEnabled);
            writer.WriteObject(BackingSource);
        }
    }
}
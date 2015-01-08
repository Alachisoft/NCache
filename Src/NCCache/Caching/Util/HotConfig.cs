// Copyright (c) 2015 Alachisoft
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
using System.Text;

namespace Alachisoft.NCache.Caching.Util
{
    public class HotConfig
    {
        private bool    _isErrorLogsEnabled;
        private bool    _isDetailedLogsEnabled;
        private long    _cacheMaxSize;
        private long    _cleanInterval;
        private float   _evictRatio;

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
            sb.Append(_cacheMaxSize + "\"");
            sb.Append(_cleanInterval + "\"");
            sb.Append(_evictRatio + "\"");

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
            return config;
        }

        private static void UpdateDelimIndexes(ref string attributes, char delim, ref int beginQuoteIndex, ref int endQuoteIndex)
        {
            beginQuoteIndex = endQuoteIndex;
            endQuoteIndex = attributes.IndexOf(delim, beginQuoteIndex + 1);
        }
    }
}

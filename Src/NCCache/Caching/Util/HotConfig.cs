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
        private bool    _isTargetCache;
      
        ///<summary>Registered Backing Source.</summary>
        private Hashtable _backingSource;

        private bool _expirationEnabled;
        private bool _absoluteLongerEnabled;
   //     private bool _absoluteLongestEnabled;
        private bool _absoluteDefaultEnabled;
        private bool _defaultSlidingEnabled;
        private bool _slidingDefaultEnabled;
        private bool _slidingLongerEnabled;
    //    private bool _slidingLongestEnabled;

        private long _absoluteDefault;
        private long _absoluteLonger;
   //     private long _absoluteLongest;

        private long _slidingDefault;
        private long _slidingLonger;
      //  private long _slidingLongest;
      
        /// <summary>
        /// Registered Backing Source.
        /// </summary>
        public Hashtable BackingSource
        {
            get { return _backingSource; }
            set { _backingSource = value; }
        }
     

  

        public float EvictRatio
        {
            get { return _evictRatio; }
            set { _evictRatio = value; }
        }

        public bool ExpirationEnabled
        {
            get { return _expirationEnabled; }
            set { _expirationEnabled = value; }
        }

        public bool AbsoluteLongerEnabled
        {
            get { return _absoluteLongerEnabled; }
            set { _absoluteLongerEnabled = value; }
        }
        public bool AbsoluteDefaultEnabled
        {
            get { return _absoluteDefaultEnabled; }
            set { _absoluteDefaultEnabled = value; }
        }
        public bool DefaultSlidingEnabled
        {
            get { return _defaultSlidingEnabled; }
            set { _defaultSlidingEnabled = value; }
        }

  
        public bool SlidingLongerEnabled
        {
            get { return _slidingLongerEnabled; }
            set { _slidingLongerEnabled = value; }
        }
        
        public long AbsoluteDefault
        {
            get { return _absoluteDefault; }
            set { _absoluteDefault = value; }
        }

        public long AbsoluteLonger
        {
            get { return _absoluteLonger; }
            set{    _absoluteLonger = value;}
        }
        

        public long SlidingDefault
        {
            get { return _slidingDefault; }
            set { _slidingDefault = value; }
        }

        public long SlidingLonger
        {
            get { return _slidingLonger; }
            set { _slidingLonger = value; }
        }
        

        public long CacheMaxSize
        {
            get { return _cacheMaxSize; }
            set { _cacheMaxSize = value; }
        }

        /// <summary>Fatal and error logs.</summary>
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
          

            UpdateDelimIndexes(ref attributes, '"', ref beginQuoteIndex, ref endQuoteIndex);
            string threshold = attributes.Substring(beginQuoteIndex + 1, endQuoteIndex - beginQuoteIndex - 1);
            if (threshold != null && threshold != string.Empty)
            {
                long kbs = Convert.ToInt64(threshold);
                // convert to bytes
               
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
        
     
            ExpirationEnabled = (reader.ReadBoolean());
            AbsoluteDefault = reader.ReadInt64();
            AbsoluteLonger = reader.ReadInt64();
            SlidingDefault = reader.ReadInt64();
            SlidingLonger = reader.ReadInt64();
            AbsoluteLongerEnabled = reader.ReadBoolean();
            AbsoluteDefaultEnabled = reader.ReadBoolean();
            DefaultSlidingEnabled= reader.ReadBoolean();
            SlidingLongerEnabled = reader.ReadBoolean();
            BackingSource = reader.ReadObject() as Hashtable;
  

        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(IsErrorLogsEnabled);
            writer.Write(IsDetailedLogsEnabled);
            writer.Write(CacheMaxSize);
            writer.Write(CleanInterval);
            writer.Write(EvictRatio);

            writer.Write(ExpirationEnabled);
            writer.Write(AbsoluteDefault);
            writer.Write(AbsoluteLonger);
            writer.Write(SlidingDefault);
            writer.Write(SlidingLonger);
            writer.Write(AbsoluteLongerEnabled);
            writer.Write(AbsoluteDefaultEnabled);
            writer.Write(DefaultSlidingEnabled );
            writer.Write(SlidingLongerEnabled);
            writer.WriteObject(BackingSource);
          
        }
    }
}
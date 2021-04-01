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
using Alachisoft.NCache.Common.DataStructures;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Management.APILogging
{
    public class APILogManager 
    {
        static  bool _enableLogging = false;
        static bool _versionMatch = false;
        object _lock = new object();

        static APILogManager s_logmanager;
        SlidingIndex<Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem> _index = null;

        public static bool EnableLogging
        {
            set { _enableLogging = value; }
            get { return _enableLogging; }
        }

        public static bool IsVersionMatched
        {
            set { _versionMatch = value; }
            get { return _versionMatch; }
        }

        public static APILogManager APILogManger
        {
            set { s_logmanager = value; }
            get { return s_logmanager; }
        }

        public void StartLogging(string cacheID,bool version)
        {
           
        }

        public void StopLogging(string cacheID)
        {
            
        }

        public bool LogEntry(Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem entry)
        {
                return true;
        }

        public List<Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem> GetEntry(ref long startTime)
        {
            List<Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem> sbEntries = new List<Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem>(); 
            try
            {
                IEnumerator en = _index.GetCurrentData(ref startTime);
                while (en.MoveNext())
                {

                    sbEntries.Add((Alachisoft.NCache.Common.Monitoring.APILogging.APIRuntimeLogItem)en.Current);
                }
                return sbEntries;
            }
            catch
            {
                return null;
            }
        }
        
        public void Dispose()
        {
            _index = null;
        }
    }
}

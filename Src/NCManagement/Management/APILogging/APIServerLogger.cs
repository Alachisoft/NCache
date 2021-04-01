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
using System.Collections.Generic;

namespace Alachisoft.NCache.Management.APILogging
{
    public class APIServerLogger
    {
        IDictionary<string, long> _instanceDic = new Dictionary<string, long>();
        private object _lock = new object();
        private bool _loggingEnabled = false;

        public APIServerLogger()
        {
            
        }
        public void AddInstanceInformation (string InstanceId)
        {
            lock (_lock)
            {
                if (InstanceId != null)
                {
                    if (!_instanceDic.ContainsKey(InstanceId))
                    {

                        _instanceDic.Add(InstanceId.ToLower(), 0);
                    }
                }
            }
        }
        public void UpdateInstanceStartTime ( string instanceID, long instanceTime)
        {
            try
            {
                lock (_lock)
                {
                    if (_instanceDic.ContainsKey(instanceID.ToLower()))
                    {
                        _instanceDic[instanceID.ToLower()] = instanceTime;
                    }
                }
            }
            catch
            {
            }
        }
        
        public long GetInstanceStartTime ( string instanceID)
        {
            try
            {
                long startTime;
                if (_instanceDic.ContainsKey(instanceID.ToLower()))
                {
                    _instanceDic.TryGetValue(instanceID.ToLower(), out startTime);
                    return startTime;
                }
            }
            catch
            {
            }
            return -1;
        }


        public bool IsLoggingEnabled (string instanceID)
        {
            try
            {
                if (_instanceDic.Count<=1)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public bool StopLogging(string InstanceID)
        {
            bool stopLogging = false;
            try {
                if (_instanceDic.ContainsKey(InstanceID.ToLower())) { 
                    lock (_lock){ 
                        _instanceDic.Remove(InstanceID.ToLower());
                        if (_instanceDic.Count==0)
                                stopLogging = true;
                    }
                }
                return stopLogging;
            }
            catch
            {
                return false;
            }
        }
      
        public bool ContainsInstance (string instanceID)
        {
            if (_instanceDic.ContainsKey(instanceID.ToLower()))
                return true;
            else
                return false;
        }
    }
}

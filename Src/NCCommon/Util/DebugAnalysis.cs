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
using System.Configuration;

namespace Alachisoft.NCache.Common.Util
{
    public class DebugAnalysis
    {
        private static int _debugWanDelay = 1; // millseconds;
        private static bool detailLogging = ServiceConfiguration.EnableDebugLog;

        public static int WANDELAY(string cacheName)
        {
            string wanAppValue = ConfigurationSettings.AppSettings["WANDELAY." + cacheName];
            if (wanAppValue != null)
            {
                _debugWanDelay = Convert.ToInt32(wanAppValue);
                if (_debugWanDelay > 1000)
                {
                    _debugWanDelay = 1000;
                    return _debugWanDelay;
                }
                else
                {
                    return _debugWanDelay;
                }
            }
            return _debugWanDelay;
        }


        public static bool EnableDetailLogging
        {
            get
            {
                return detailLogging;
            }
        }
    }
}

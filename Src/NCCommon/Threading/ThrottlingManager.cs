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
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Common.Threading
{
    public class ThrottlingManager
    {
        long _limit;
        long _throtllingUnitMs = 1000;
        long _currentInternval;
        long _currentSize;
        long _currentMilliSeconds;

 
        DateTime _startTime;

       
        public ThrottlingManager(long limit)
        {
            _limit = limit;
        }

        public ThrottlingManager(long limit, long unit)
        {
            _limit = limit;
            _throtllingUnitMs = unit;
        }


        public void Start()
        {
            _startTime = DateTime.Now;
            _currentMilliSeconds = GetMilliSecondDiff();
            _currentInternval = _currentMilliSeconds / _throtllingUnitMs;
        }


        private long GetMilliSecondDiff()
        {
            return (long)(DateTime.Now - _startTime).TotalMilliseconds;
        }

        /// <summary>
        /// Waits if throttling limit reaches 
        /// </summary>
        /// <param name="size"></param>
        public void Throttle(long size)
        {
            lock (this)
            {
                long msNow = GetMilliSecondDiff();
                long currentInterval = msNow / _throtllingUnitMs;
                if (currentInterval == _currentInternval)
                {
                    _currentSize += size;
                    if (_currentSize >= _limit)
                    {
                        Thread.Sleep((int)(_throtllingUnitMs - (msNow - _currentMilliSeconds)));
                    }
                  
                }
                else
                {
                    _currentInternval = currentInterval;
                    _currentMilliSeconds = msNow;
                    _currentSize= size;
                }
            }
        }
    }
}

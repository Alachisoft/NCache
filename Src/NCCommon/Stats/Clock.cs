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
using System.Timers;

namespace Alachisoft.NCache.Common.Stats
{
    public class Clock
    {
        private static int _updateInterval = 1; //in seconds;
        private static long _currentTime;
        private static bool _started;
        private static Timer _timer;
        private static int _refCount;

        public static void StartClock()
        {
            lock (typeof(Clock))
            {
                if (!_started)
                {
                    _timer = new Timer(_updateInterval * 1000);
                    _timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
                    _timer.Start();
                    _started = true;
                }
                _refCount++;
            }
        }

        public static void StopClock()
        {
            lock (typeof(Clock))
            {
                _refCount--;
                if (_started && _refCount == 0)
                {
                    _timer.Stop();
                    _started = false;
                    _currentTime = 0;
                }
            }
        }

        private static void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            System.Threading.Interlocked.Increment(ref _currentTime);
        }

        public static long CurrentTimeInSeconds
        {
            get { return _currentTime; }
        }
    }
}

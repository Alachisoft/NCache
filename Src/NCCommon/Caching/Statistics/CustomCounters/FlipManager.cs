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
using System.Threading;

namespace Alachisoft.NCache.Common.Caching.Statistics.CustomCounters
{
    public class FlipManager
    {
        static int _timeLapse;
        static InstantaneousFlip _flip;
        static Thread _flipManagerThread;
        private static bool _isStarted;

        static FlipManager()
        {
            _flip = new InstantaneousFlip(0);
            _timeLapse = 1000; //1sec
            _isStarted = false;

            _flipManagerThread = new Thread(Run)
            {
                Name = "FlipManagerThread",
                IsBackground = true
            };

            _flipManagerThread.Start();
        }

        static void TimerElapsed()
        {
            _flip.Increment();
        }

        public static InstantaneousFlip Flip { get { return _flip; } }


        public static void Run()
        {
            if (_isStarted)
            {
                return;
            }
            while (true)
            {
                FlipManager.TimerElapsed();
                try
                {
                    Thread.Sleep(_timeLapse);
                }
                catch (ThreadAbortException)
                {
                    _isStarted = false;
                }
                catch (ThreadInterruptedException)
                {
                    _isStarted = false;
                }
            }
        }
    }
}

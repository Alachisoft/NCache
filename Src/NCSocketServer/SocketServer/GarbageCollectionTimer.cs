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
using System.Threading;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// Follows Singleton patren. This class is responsible to do preodic collections of GEN#2.
    /// By default the due time and period is set to 5 mins. The minimum interval of 1 mins can be specified.
    /// </summary>
    public sealed class GarbageCollectionTimer : IDisposable
    {
        TimerCallback timerCallback = null;
        private Timer timer = null;
        private static GarbageCollectionTimer _instance = null;

        private bool stopped = true;
        /// <summary>
        /// To prevent Object creation by user.
        /// </summary>
        private GarbageCollectionTimer()
        {
            timerCallback = new TimerCallback(StartColletion);
            //timer = new Timer(timerCallback, 2, System.Threading.Timeout.Infinite, 0);
        }

        /// <summary>
        /// Returns the GarbageCollectionThread instance. It ensures that only one instance 
        /// of this class exist.
        /// </summary>
        /// <returns>GarbageCollectionThread instance</returns>
        public static GarbageCollectionTimer GetInstance()
        {
            if (_instance == null)
                _instance = new GarbageCollectionTimer();
            return _instance;
        }

        /// <summary>
        /// priate reentrant mehtod used for the timercallback.
        /// </summary>
        private void StartColletion(object generation)
        {
            if (!stopped)
            {
                //GC.Collect(2);

                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "GarbageCollectionTimer.StartCollection", "Generation #2 collected.");
            }
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before first collection, in minutes. minimum value is 0 mins which does the collection immediately.</param>
        /// <param name="period">The time interval between tw oconsective collections, in minutes. minimus value is 1 mins.</param>
        public void Start(int dueTime, int period)
        {
            if (dueTime < 0)
                throw new ArgumentException("The Value must be greater than equal to zero(0).", "dueTime");
            if (period < 1)
                throw new ArgumentException("The value must be greater than 0.", "period");

            if (timer == null)
                timer = new Timer(timerCallback, 2, new TimeSpan(0, dueTime, 0), new TimeSpan(0, period, 0));
            stopped = false;

        }

        public void Stop()
        {
            stopped = true;
            if (timer != null)
                timer.Dispose();
            timer = null;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Stop();
            _instance = null;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}

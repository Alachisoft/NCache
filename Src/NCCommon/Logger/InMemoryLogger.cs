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
using System.Threading;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures;

namespace Alachisoft.NCache.Common.Logger
{
    /// <summary>
    /// This Class used for In Memory-Logging for Bridge Dev Tracing
    /// </summary>
    public class InMemoryLogger
    {
        private Hashtable _hashtable;
        private int _addCount=0;
        private int _updateCount=0;
        private string _cacheName;
        private Thread _writerThread;
        private bool _isThreadStopped = true;
        private string _loggerType;
        private int _threadWaitInterval = 120000; //2 min  || 240000; 4 min; 
        private ILogger _nCachelogger;
        private static Dictionary<string, InMemoryLogger> _inMemoryLoggers = new Dictionary<string, InMemoryLogger>();

        public static InMemoryLogger GetLogger(string srcCache, string LoggerType, ILogger NCacheLog)
        {
            InMemoryLogger logger = null;
            lock (_inMemoryLoggers)
            {
                if (_inMemoryLoggers.ContainsKey(srcCache))
                    logger = _inMemoryLoggers[srcCache];
                else
                {
                    logger = new InMemoryLogger();
                    logger.Initialize(srcCache, LoggerType, NCacheLog);
                    _inMemoryLoggers.Add(srcCache, logger);
                }
            }
            return logger;
        }


        public void Initialize(string targetId, string LoggerType, ILogger ncachelogger)
        {
            _hashtable = new Hashtable();
            _cacheName = targetId;
            _loggerType = LoggerType;
            _nCachelogger = ncachelogger;
            DebugStart();
        }

        private void DebugStart()
        {
            _writerThread = new Thread(new ThreadStart(Run));
            _writerThread.IsBackground = true;
            _writerThread.Name = "InMemoryLogger." + _cacheName + "." + _loggerType;
            _writerThread.Start();
        }

        private void Run()
        {
            while (_isThreadStopped)
            {
                Thread.Sleep(_threadWaitInterval);
                string msg =  " ::: Add = " + _addCount + "  ---- Update = " + _updateCount + " ::: ";
                _nCachelogger.DevTrace(msg);
            }
        }

        public void Stop()
        {
            _isThreadStopped = false;

            if (_writerThread.IsAlive)
            {
#if !NETCORE
                _writerThread.Abort();
#elif NETCORE
                _writerThread.Interrupt();
#endif
                _writerThread = null;
            }

            _addCount = 0;
            _updateCount = 0;
              
        }
    }


}





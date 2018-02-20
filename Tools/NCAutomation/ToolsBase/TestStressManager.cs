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

using Alachisoft.NCache.Web.Caching;
using System;
using System.Collections.Generic;
using Factory = Alachisoft.NCache.Web.Caching.NCache;
using System.Threading;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Automation.ToolsOutput;
using System.Management.Automation;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    internal sealed class TestStressManager : PSCmdlet
    {
        string _cacheId = "";
        int _totalLoopCount = 0;
        int _testCaseIterations = 10;
        int _testCaseIterationDelay = 0;
        int _getsPerIteration = 1;
        int _updatesPerIteration = 1;
        int _dataSize = 1024;
        int _expiration = 300;
        int _threadCount = 1;
        int _reportingInterval = 5000;
        string _userId;
        string _password;
        IOutputConsole _outputProvider;
        IList<StressThreadTask> _tasks = new List<StressThreadTask>();
        Cache _cache;
        PowerShellAdapter _adapter;

        public TestStressManager(string cacheId, int totalLoopCount, int testCaseIterations, int testCaseIterationDelay, int getsPerIteration, int updatesPerIteration, int dataSize, int expiration, int threadCount, int reportingInterval, bool noLogo, string userID, string passwrod, IOutputConsole outputProvider, PowerShellAdapter adapter)
        {
            _cacheId = cacheId;
            _totalLoopCount = totalLoopCount;
            _testCaseIterations = testCaseIterations;
            _testCaseIterationDelay = testCaseIterationDelay;
            _getsPerIteration = getsPerIteration;
            _updatesPerIteration = updatesPerIteration;
            _dataSize = dataSize;
            _expiration = expiration;
            _threadCount = threadCount;
            _reportingInterval = reportingInterval;
            _userId = userID;
            _password = passwrod;
            _outputProvider = outputProvider;
            _adapter = adapter;
        }

        public void StartTasks()
        {
            try
            {
                Thread[] threads = new Thread[_threadCount];
                CacheInitParams parameters = new CacheInitParams();
               
                _cache = Factory.InitializeCache(_cacheId, parameters);
                _cache.ExceptionsEnabled = true;

                string pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();

                for (int threadIndex = 0; threadIndex < _threadCount; threadIndex++)
                {
                    StressThreadTask threadTask = new StressThreadTask(_cache, _totalLoopCount, _testCaseIterations, _testCaseIterationDelay, _getsPerIteration, _updatesPerIteration, _dataSize, _expiration, _threadCount, _reportingInterval, threadIndex, _outputProvider, _adapter);
                    _tasks.Add(threadTask);
                    threadTask.Start();
                }
                _adapter.Listen();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void StopTasks(bool forcefully)
        {
            try
            {
                foreach (var task in _tasks)
                {
                    task.Stop(forcefully);
                }               
            }
            catch (Exception ex)
            {
                _outputProvider.WriteLine(ex.ToString());
            }
            finally
            {
                if (_cache != null)
                    _cache.Dispose();
            }
        }


        public class StressThreadTask : PSCmdlet
        {
            Cache _cache = null;
            int _totalLoopCount = 0;
            int _testCaseIterations = 10;
            int _testCaseIterationDelay = 0;
            int _getsPerIteration = 1;
            int _updatesPerIteration = 1;
            int _dataSize = 1024;
            int _expiration = 300;
            int _threadCount = 1;
            int _reportingInterval = 5000;
            int _threadIndex = 0;
            int _pid = 0;
            int numErrors = 0;
            int maxErrors = 1000;
            IOutputConsole _outputProvider;
            Thread thread;
            PowerShellAdapter _adapter;

            /// <summary>
            /// Constructor
            /// </summary>
            public StressThreadTask(Cache cache, int totalLoopCount, int testCaseIterations, int testCaseIterationDelay, int getsPerIteration, int updatesPerIteration, int dataSize, int expiration, int threadCount, int reportingInterval, int threadIndex, IOutputConsole outputProvider, PowerShellAdapter adapter)
            {
                _cache = cache;
                _totalLoopCount = totalLoopCount;
                _testCaseIterations = testCaseIterations;
                _testCaseIterationDelay = testCaseIterationDelay;
                _getsPerIteration = getsPerIteration;
                _updatesPerIteration = updatesPerIteration;
                _dataSize = dataSize;
                _expiration = expiration;
                _threadCount = threadCount;
                _reportingInterval = reportingInterval;
                _threadIndex = threadIndex;
                _pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                CreateThread();
                _outputProvider = outputProvider;
                _adapter = adapter;
            }

            private void CreateThread()
            {
                ThreadStart threadDelegate = new ThreadStart(DoTest);
                thread = new Thread(threadDelegate);

                thread.Name = "ThreadIndex: " + _threadIndex;
            }
            /// <summary>
            /// Test starting call
            /// </summary>
            public void DoTest()
            {
                try
                {
                    DoGetInsert();
                }
                catch (Exception e)
                {
                    if (e.Message != null && e.Message.Contains("The WriteObject and WriteError methods cannot be called from outside the overrides of the BeginProcessing, ProcessRecord, and EndProcessing methods, and they can only be called from within the same thread. Validate that the cmdlet makes these calls correctly, or contact Microsoft Customer Support Services."))
                    {
                      _adapter.TerminateThreads = true;
                    }

                    else
                    {
                        _adapter.WriteObject("DoTest() Exception: " + e.ToString() + "\n");
                        _adapter.TerminateThreads = true;
                    }                              
                }
            }

            public void Start()
            {
                if (thread == null)
                    CreateThread();
                thread.Start();
            }

            public void Stop(bool forcefully)
            {
                try
                {
                    if (thread.IsAlive)
                    {
                        if (forcefully)
                        {
                            thread.Abort();
                            _adapter.Finished = true;
                            return;
                        }
                        else
                        {
                            thread.Join();

                        }
                    }
                }
                catch (ThreadAbortException e)
                {
                    Thread.ResetAbort();
                }
                _adapter.Finished = true;
            }

            /// <summary>
            /// Perform Get/Insert operations on cache, bsed on user given input.
            /// </summary>
            private void DoGetInsert()
            {

                byte[] data = new byte[_dataSize];

                if (_totalLoopCount <= 0)
                {
                    // this means an infinite loop. user will have to do Ctrl-C to stop the program
                    for (long totalIndex = 0; ; totalIndex++)
                    {
                        ProcessGetInsertIteration(data);
                        if (totalIndex >= _reportingInterval)
                        {
                            try
                            {
                                long count = _cache.Count;
                                _adapter.WriteObject(DateTime.Now.ToString() + ": Cache count: " + count);
                                totalIndex = 1;
                            }
                            catch (Exception e)
                            {
                                _adapter.WriteObject("DoGetInsert() Exception: " + e.ToString() + "\n");
                                numErrors++;
                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;                                   
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (long totalIndex = 0; totalIndex < _totalLoopCount; totalIndex++)
                    {
                        ProcessGetInsertIteration(data);
                        if (totalIndex >= _reportingInterval)
                        {
                            try
                            {
                                long count = _cache.Count;
                                _adapter.WriteObject(DateTime.Now.ToString() + ": Cache count: " + count);
                            }
                            catch (Exception e)
                            {
                                _adapter.WriteObject("DoGetInsert() Exception: " + e.ToString() + "\n");
                                numErrors++;
                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;                                    
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Perform Get/Insert task on cache.
            /// Called by DoGetInsert method
            /// </summary>
            private void ProcessGetInsertIteration(byte[] data)
            {
                string guid = System.Guid.NewGuid().ToString(); //create a unique key to be inserted in store.

                for (long testCaseIndex = 0; testCaseIndex < _testCaseIterations; testCaseIndex++)
                {
                    string key = guid;

                    for (int getsIndex = 0; getsIndex < _getsPerIteration; getsIndex++)
                    {
                        try
                        {
                            object obj = _cache.Get(key);
                        }
                        catch (Exception e)
                        {
                            if (e.Message != null && e.Message.Contains("You've hit the maximum requests that can be made to a cache server under pre-evaluation. Please request a trial license key at sales@alachisoft.com to remove this limitation."))
                            {
                                _adapter.WriteObject("GET Error: Exception: " + e.ToString() + "\n");                                
                                _adapter.TerminateThreads = true;                               
                            }
                            else
                            {

                                _adapter.WriteObject("GET Error: Key: " + key + ", Exception: " + e.ToString() + "\n");
                                numErrors++;


                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;
                                }
                            }
                        }

                    }

                    for (int updatesIndex = 0; updatesIndex < _updatesPerIteration; updatesIndex++)
                    {
                        try
                        {
                            _cache.Insert(key, data,
                                                    Cache.NoAbsoluteExpiration,
                                                    new TimeSpan(0, 0, 0, _expiration),
                                                    CacheItemPriority.Default);
                        }
                        catch (Exception e)
                        {

                            if (e.Message != null && e.Message.Contains("You've hit the maximum requests that can be made to a cache server under pre-evaluation. Please request a trial license key at sales@alachisoft.com to remove this limitation."))
                            {
                                _adapter.WriteObject("INSERT Error: Exception: " + e.ToString() + "\n");
                                _adapter.TerminateThreads = true;
                            }
                            else
                            {
                                _adapter.WriteObject("INSERT Error: Key: " + key + ", Exception: " + e.ToString() + "\n");
                                numErrors++;

                                if (numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;
                                }
                            }
                        }
                    }

                    if (_testCaseIterationDelay > 0)
                    {
                        // Sleep for this many seconds
                        Thread.Sleep(_testCaseIterationDelay * 1000);
                    }

                }
            }

        }
    }
}

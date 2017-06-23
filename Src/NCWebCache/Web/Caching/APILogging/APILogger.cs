// Copyright (c) 2017 Alachisoft
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
using System.IO;
using Alachisoft.NCache.Common;

using System.Threading;

namespace Alachisoft.NCache.Web.Caching.APILogging
{
    internal class APILogger
    {
        private string _fileName;
        private string _cacheName;
        private int _iterationNumber = 0;

        private Queue _logQueue = Queue.Synchronized(new Queue());
        private Thread _thread;
        private int _logToFileInterval;
        private DebugAPIConfiguraions _debugAPIConfigurations;

        public APILogger(string cacheName, DebugAPIConfiguraions debugAPIConfigurations)
        {
            _debugAPIConfigurations = debugAPIConfigurations;
            _logToFileInterval = 1000 * DebugAPIConfiguraions.LoggerThreadLoggingInterval;

            _cacheName = cacheName;
            _thread = new Thread(new ThreadStart(LogInBackground));
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Log(APILogItem logItem)
        {
            logItem.LoggingTime = DateTime.Now;
            _logQueue.Enqueue(logItem);
        }

        private void LogInBackground()
        {
            while (!_debugAPIConfigurations.LoggingExpired)
            {
                try
                {
                    while (_logQueue.Count > 0)
                    {
                        APILogItem apiLogItem = _logQueue.Dequeue() as APILogItem;
                        LogInternal(apiLogItem);
                    }
                    Thread.Sleep(_logToFileInterval);
                }
                catch (ThreadAbortException) { break; }
                catch (ThreadInterruptedException) { break; }
                catch (Exception e)
                {
                    try
                    {
                        APILogItem logItem = new APILogItem(null, "An error occurred while logging" + e.ToString());
                        LogInternal(logItem);
                    }
                    catch (Exception) { }
                }
            }
        }

        public void LogInternal(APILogItem logItem)
        {
            using (StreamWriter w = File.AppendText(getFileName(logItem.LoggingTime)))
            {
                w.WriteLine(logItem.LoggingTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt") + "\t" + logItem.Signature);
                if (logItem.NoOfKeys > 1)
                {
                    w.WriteLine(String.Format("{0,-30}\t Number of Keys = {1}", "", logItem.NoOfKeys.ToString()));
                }

                if (!String.IsNullOrEmpty(logItem.Key))
                    w.WriteLine(String.Format("{0,-30}\t Key = {1}", " ", logItem.Key));
                if (logItem.AbsolueExpiration != null)
                    w.WriteLine(String.Format("{0,-30}\t Absolute Expiration = {1}", " ", logItem.AbsolueExpiration.ToString()));
                if (logItem.SlidingExpiration != null)
                    w.WriteLine(String.Format("{0,-30}\t Sliding Expiration = {1} milliseconds", " ", logItem.SlidingExpiration.Value.TotalMilliseconds));
                if (logItem.Priority != null)
                    w.WriteLine(String.Format("{0,-30}\t Priority = {1}", " ", logItem.Priority.ToString()));

                if (!string.IsNullOrEmpty(logItem.Query))
                    w.WriteLine(String.Format("{0,-30}\t Query = {1}", " ", logItem.Query.ToString()));
               
                if (logItem.QueryValues != null)
                {
                    w.WriteLine(String.Format("{0,-30}\t Values:", " "));
                    IDictionaryEnumerator ide = logItem.QueryValues.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        w.WriteLine(String.Format("{0,-30}\t\t Key = " + ide.Key.ToString() + "\tValue = " + ide.Value.ToString(), " "));
                    }

                }

                if (logItem.LockTimeout != null)
                    w.WriteLine(String.Format("{0,-30}\t LockTimeout = {1} milliseconds", " ", logItem.LockTimeout.Value.TotalMilliseconds.ToString()));
                if (logItem.AcquireLock != null)
                    w.WriteLine(String.Format("{0,-30}\t AcquireLock = {1}", " ", logItem.AcquireLock.ToString()));

                if (logItem.RuntimeAPILogItem != null)
                {
                    RuntimeAPILogItem rtLogItem = logItem.RuntimeAPILogItem;
                    string avg = rtLogItem.IsBulk ? "Average " : "";
                    if(rtLogItem.IsBulk)
                        w.WriteLine(String.Format("{0,-30}\t Number of Objects = {1}", " ", rtLogItem.NoOfObjects.ToString()));
                    w.WriteLine(String.Format("{0,-30}\t {1}Object Size (bytes) = {2}", " ", avg, rtLogItem.SizeOfObject));
                }

                if (logItem.NoOfObjectsReturned != null)
                    w.WriteLine(String.Format("{0,-30}\t Number of Objects Returned = {1}", " ", logItem.NoOfObjectsReturned.ToString()));
                if (logItem.ExceptionMessage != null)
                {
                    w.WriteLine(String.Format("{0,-30}\t Exception = {1}", " ", logItem.ExceptionMessage));
                }
                w.WriteLine();

            }
        }

        private string getFileName(DateTime loggingTime)
        {
            int iterationNumber = _debugAPIConfigurations.GetIterationNumber(loggingTime);
            if (_fileName != null && _iterationNumber == iterationNumber)
            {
                return _fileName;
            }
            _iterationNumber = iterationNumber;

            _fileName = getPath() + "\\" + "APIUsageLogs." + _cacheName + "_" + loggingTime.Day.ToString() + "-" + loggingTime.Month + "-" + loggingTime.Year + "-" + loggingTime.Hour + "-" + loggingTime.Minute + "-" + loggingTime.Second + ".logs.txt"; ;

            using (StreamWriter w = File.AppendText(_fileName))
            {
                w.WriteLine("TIMESTAMP                              \t API CALL");
            }
            return _fileName;
        }

        private string getPath()
        {
            string _path = AppUtil.InstallDir;
            string dirName = @"log-files\\APIUsageLogs";
            try
            {
                _path = System.IO.Path.Combine(_path, dirName);
            }
            catch { }

            if (_path == null && _path.Length > 0)
            {
                _path = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).DirectoryName;
                _path = System.IO.Path.Combine(_path, dirName);
            }
            if (!System.IO.Directory.Exists(_path))
                System.IO.Directory.CreateDirectory(_path);
            return _path;
        }

        public void Dispose()
        {
            _thread.Abort();
        }
    }
}

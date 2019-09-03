//  Copyright (c) 2018 Alachisoft
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
using System.Collections;
using System.IO;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Caching;
using System.Threading;
using Runtime = Alachisoft.NCache.Runtime;

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

                if (!String.IsNullOrEmpty(logItem.Group))
                    w.WriteLine(String.Format("{0,-30}\t Group = {1}", " ", logItem.Group));
                if (!String.IsNullOrEmpty(logItem.SubGroup))
                    w.WriteLine(String.Format("{0,-30}\t SubGroup = {1}", " ", logItem.SubGroup));

                if (logItem.Tags != null && logItem.Tags.Length != 0)
                {
                    w.WriteLine(String.Format("{0,-30}\t Tags:", " "));
                    foreach (Tag t in logItem.Tags)
                        w.WriteLine(String.Format("{0,-30}\t\tValue = {1}", " ", t != null ? t.TagName : ""));
                }

                if (logItem.NamedTags != null && logItem.NamedTags.Count != 0)
                {
                    w.WriteLine(String.Format("{0,-30}\t NamedTags:", " "));
                    IEnumerator ie = logItem.NamedTags.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        DictionaryEntry de = (DictionaryEntry)ie.Current;
                        w.WriteLine(String.Format("{0,-30}\t\t Key = " + de.Key.ToString() + "\tValue = " + de.Value.ToString(), " "));
                    }
                }

                if (logItem.Priority != null)
                    w.WriteLine(String.Format("{0,-30}\t Priority = {1}", " ", logItem.Priority.ToString()));

                if (logItem.Dependency != null)
                {
                    w.WriteLine(String.Format("{0,-30}\t Dependency = {1}", " ", logItem.Dependency.GetType().Name));
                    if ((logItem.Dependency as Runtime.Dependencies.KeyDependency) != null)
                    {
                        w.WriteLine(string.Format("{0,-30}\t\t KeyDependencyType = {1}", " ", (logItem.Dependency as Runtime.Dependencies.KeyDependency).KeyDependencyType));
                        w.WriteLine(String.Format("{0,-30}\t\t Keys:", " "));
                        Runtime.Dependencies.KeyDependency kd = (Runtime.Dependencies.KeyDependency)logItem.Dependency;
                        foreach (string key in kd.CacheKeys)
                            w.WriteLine(String.Format("{0,-30}\t\t\tValue = {1}", " ", key));
                    }
                    else if ((logItem.Dependency as Runtime.Dependencies.FileDependency) != null)
                    {
                        w.WriteLine(String.Format("{0,-30}\t\t Files:", " "));
                        Runtime.Dependencies.FileDependency fd = (Runtime.Dependencies.FileDependency)logItem.Dependency;
                        foreach (string fileName in fd.fileNames)
                            w.WriteLine(String.Format("{0,-30}\t\t\tValue = {1}", " ", fileName));
                    }
                }
                if (logItem.SyncDependency != null)
                    w.WriteLine(String.Format("{0,-30}\t SyncDependency = {1}", " ", logItem.SyncDependency.ToString()));
                if (logItem.IsResyncRequired != null)
                    w.WriteLine(String.Format("{0,-30}\t IsResyncRequired = {1}", " ", logItem.IsResyncRequired.ToString()));


                if (!String.IsNullOrEmpty(logItem.ProviderName))
                    w.WriteLine(String.Format("{0,-30}\t ProviderName = {1}", " ", logItem.ProviderName));

                if (!String.IsNullOrEmpty(logItem.ResyncProviderName))
                    w.WriteLine(String.Format("{0,-30}\t ResyncProviderName = {1}", " ", logItem.ResyncProviderName));

                if (logItem.DSWriteOption != null)
                    w.WriteLine(String.Format("{0,-30}\t DSWriteOption = {1}", " ", logItem.DSWriteOption.ToString()));

                if (logItem.DSReadOption != null)
                    w.WriteLine(String.Format("{0,-30}\t DSReadOption = {1}", " ", logItem.DSReadOption.ToString()));


                if (logItem.ContinuousQuery != null)
                {
                    w.WriteLine(String.Format("{0,-30}\t ContinuousQuery:", " "));
                    w.WriteLine(String.Format("{0,-30}\t\t Query = {1}", " ", logItem.ContinuousQuery.Query));
                    w.WriteLine(String.Format("{0,-30}\t\t Values:", " "));
                    IDictionaryEnumerator ide = logItem.ContinuousQuery.Values.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        w.WriteLine(String.Format("{0,-30}\t\t\t Key = " + ide.Key.ToString() + "\tValue = " + ide.Value.ToString(), " "));
                    }
                }


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


                if (logItem.StreamMode != null)
                    w.WriteLine(String.Format("{0,-30}\t SreamMode = {1}", " ", logItem.StreamMode.ToString()));

                if (logItem.CacheItemVersion != null)
                    w.WriteLine(String.Format("{0,-30}\t CacheItemVersion = {1}", " ", logItem.CacheItemVersion.ToString()));

                if (logItem.RuntimeAPILogItem != null)
                {
                    RuntimeAPILogItem rtLogItem = logItem.RuntimeAPILogItem;
                    string avg = rtLogItem.IsBulk ? "Average " : "";
                    if (rtLogItem.IsBulk)
                        w.WriteLine(String.Format("{0,-30}\t Number of Objects = {1}", " ", rtLogItem.NoOfObjects.ToString()));
                    w.WriteLine(String.Format("{0,-30}\t {1}Object Size (bytes) = {2}", " ", avg, rtLogItem.SizeOfObject));
                    w.WriteLine(String.Format("{0,-30}\t Encryption Enabled = {1}", " ", rtLogItem.EncryptionEnabled.ToString()));
                    if (rtLogItem.EncryptionEnabled)
                        w.WriteLine(String.Format("{0,-30}\t {1}Encrypted Object Size (bytes) = {2}", " ", avg, logItem.RuntimeAPILogItem.SizeOfEncryptedObject));
                    w.WriteLine(String.Format("{0,-30}\t Compression Enabled = {1}", " ", rtLogItem.CompressionEnabled.ToString()));
                    if (rtLogItem.CompressionEnabled)
                        w.WriteLine(String.Format("{0,-30}\t {1}Compressed Object Size (bytes) = {2}", " ", avg, logItem.RuntimeAPILogItem.SizeOfCompressedObject));
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

            _fileName = getPath() + Path.DirectorySeparatorChar + "APIUsageLogs." + _cacheName + "_" + loggingTime.Day.ToString() + "-" + loggingTime.Month + "-" + loggingTime.Year + "-" + loggingTime.Hour + "-" + loggingTime.Minute + "-" + loggingTime.Second + ".logs.txt"; ;

            //Header of File
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
#if !NETCORE
            _thread.Abort();
#elif NETCORE
            _thread.Interrupt();
#endif
        }
    }
}

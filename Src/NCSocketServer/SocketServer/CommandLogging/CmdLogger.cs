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
using System.Linq;
using System.IO;
using System.Threading;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.SocketServer.CommandLogging
{
    public class CmdLogger : IDisposable
    {
        public enum LogType
        {
            Info,
            Warning,
            Error
        }

        private Queue _logQueue;
        private Thread _logThread;
        private string _currentPath;
        private LogType defaultLogType = LogType.Info;
        private bool enabled = true;

        public CmdLogger()
        { }

        public CmdLogger(string destinationPath, string name, LogType defaulType)
        {
            string logFilePath;
            _logQueue = Queue.Synchronized(new Queue());
            if (Directory.Exists(destinationPath))
                logFilePath = destinationPath;
            else
            {
                logFilePath = AppUtil.InstallDir;
            }

            defaultLogType = defaulType;
            logFilePath = Path.Combine(logFilePath, name);
            _currentPath = logFilePath + ".log";
            
#if NET40
            File.WriteAllLines(_currentPath, new[]
            {
                string.Format("{0,20}     {1,20}      {2, 20}      {3, 15}     {4, 20}", "TIMESTAMP", "Client(IP:ID)",
                    "ExecutionTime(seconds)", "Command", "Parameters")
                ,
                string.Format("{0,20}     {1,20}      {2, 20}      {3, 20}     {4, 20}", "", "","", "", "") // Add an empty line.
            }); 
#else
            File.WriteAllText(_currentPath,
                string.Format("{0,20}     {1,20}      {2, 20}      {3, 15}     {4, 20}", "TIMESTAMP", "Client(IP:ID)",
                    "ExecutionTime(seconds)", "Command", "Parameters")
                + Environment.NewLine +
                string.Format("{0,20}     {1,20}      {2, 20}      {3, 20}     {4, 20}", "", "", "", "", "")); 
#endif

            // Start log persistence thread...
            _logThread = new Thread(Run) { IsBackground = true };
            _logThread.Start();
        }

        public void Dispose()
        {
            if (_logQueue != null)
            {
                _logQueue.Clear();
                _logQueue = null;
            }
            if (_logThread != null)
            {
                try
                {
#if !NETCORE
                    _logThread.Abort();
#elif NETCORE
                    _logThread.Interrupt();
#endif

                    _logThread = null;
                }
                catch (Exception)
                {}
            }
        }
        
        private void Run()
        {
            if (enabled)
            {
                while (true)
                {
                    if (_logQueue.Count < 1)
                    {
                        lock (_logQueue)
                        {
                            Monitor.Wait(_logQueue);
                        }
                    }
                    if (_logQueue.Count > 0)
                    {
                        IEnumerable enumerable;
                        lock (_logQueue)
                        {
                            enumerable = _logQueue.ToArray();
                            _logQueue.Clear();
                        }
#if NET40
                        File.AppendAllLines(_currentPath, enumerable.OfType<string>()); 
#else
                        foreach (string str in enumerable)
                        {
                            File.WriteAllText(_currentPath, Environment.NewLine + str); 
                        }
#endif
                    }
                }
            }
        }

        public virtual void Log(string clientID, string executionTime, string commandName, string commandParams)
        {
            lock (_logQueue)
            {
                _logQueue.Enqueue(string.Format("{0,20}     {1,20}      {2, 20}      {3, 20}     {4, 20}",
                    DateTime.Now, clientID,
                    executionTime, commandName, commandParams));
                Monitor.PulseAll(_logQueue);
            }
        }
    }
}

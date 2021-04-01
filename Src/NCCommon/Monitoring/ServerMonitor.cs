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

namespace Alachisoft.NCache.Common.Monitoring
{
    public class ServerMonitor
    {
        private static bool _monitor;
        private static Hashtable _clientActivity = Hashtable.Synchronized(new Hashtable());
        private static Hashtable _threadClientMap = Hashtable.Synchronized(new Hashtable());
        private static Hashtable _dataStructures = Hashtable.Synchronized(new Hashtable());
        private static Hashtable _dedicatedThreadsActivity = Hashtable.Synchronized(new Hashtable());
        private static DateTime _startTime;
        private static DateTime _endTime;
        private static object _sync = new object();

        public static DateTime StartTime
        {
            get { return _startTime; }
        }

        public static DateTime EndTime
        {
            get { return _endTime; }
        }

        public static bool MonitorActivity
        {
            get { return _monitor; }
        }
        
        public static void StartMonitoring()
        {
            lock (_sync)
            {
                if (_monitor) return; //already started.
                _monitor = true;
                _startTime = DateTime.Now;
            }
        }

        public static void StopMonitoring()
        {
            lock (_sync)
            {
                if (_monitor)
                {
                    _monitor = false;
                    _endTime = DateTime.Now;
                }
            }
        }

        public static void RegisterClient(string clientID, string address)
        {
            if (clientID == null) return;
            lock (_clientActivity.SyncRoot)
            {
                if (!_clientActivity.Contains(clientID))
                {
                    ClientMonitor clientActivity = new ClientMonitor(clientID, address);
                    _clientActivity.Add(clientID, clientActivity);
                }
            }
        }

        public static void UnregisterClient(string clientId)
        {
            if (clientId == null) return;
            lock (_clientActivity.SyncRoot)
            {
                if (_clientActivity.Contains(clientId))
                {
                    _clientActivity.Remove(clientId);
                }
            }
        }

        public static void StartClientActivity(string clientId)
        {
            if (clientId == null) return;
            if (_monitor)
            {
                ClientMonitor cMonitor = _clientActivity[clientId] as ClientMonitor;
                if (cMonitor != null)
                {
                    cMonitor.StartActivity();
                }
                int tId = Thread.CurrentThread.ManagedThreadId;

                lock (_threadClientMap.SyncRoot)
                {
                    _threadClientMap[tId] = cMonitor;
                }
            }
        }

        public static void StopClientActivity(string clientId)
        {

            if (_monitor)
            {
                ClientMonitor cMonitor = _clientActivity[clientId] as ClientMonitor;
                if (cMonitor != null)
                {
                    cMonitor.StopActivity();
                }
                int tId = Thread.CurrentThread.ManagedThreadId;

                lock (_threadClientMap.SyncRoot)
                {
                    _threadClientMap.Remove(tId);
                }
            }
        }

        public static void LogClientActivity(string method, string activity)
        {

            if (_monitor)
            {
                ClientMonitor cMonitor = _threadClientMap[Thread.CurrentThread.ManagedThreadId] as ClientMonitor;
                if (cMonitor != null)
                {
                    cMonitor.LogActivity(method,activity);
                }
            }
        }

        public static Hashtable GetCompletedClientActivity()
        {
            Hashtable activityTable = new Hashtable();
            lock (_clientActivity.SyncRoot)
            {
                IDictionaryEnumerator ide = _clientActivity.GetEnumerator();

                while (ide.MoveNext())
                {
                    ClientMonitor cMonitor = ide.Value as ClientMonitor;
                    activityTable.Add(cMonitor.Info, cMonitor.GetCompletedClientActivities());
                }
            }
            return activityTable;
        }

        public static Hashtable GetCurrentClientActivity()
        {
            Hashtable activityTable = new Hashtable();
            lock (_clientActivity.SyncRoot)
            {
                IDictionaryEnumerator ide = _clientActivity.GetEnumerator();

                while (ide.MoveNext())
                {
                    ClientMonitor cMonitor = ide.Value as ClientMonitor;
                    activityTable.Add(cMonitor.Info, cMonitor.GetCurrentClientActivities());
                }
            }
            return activityTable;
        }

        public static void Reset()
        {
            if (_clientActivity != null)
            {
                lock (_clientActivity.SyncRoot)
                {
                    _clientActivity.Clear();
                }
            }
            if (_dedicatedThreadsActivity != null)
            {
                lock (_dedicatedThreadsActivity.SyncRoot)
                {
                    _dedicatedThreadsActivity.Clear();
                }
            }
        }
    }
}

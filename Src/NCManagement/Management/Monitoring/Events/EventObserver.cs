//  Copyright (c) 2026 Alachisoft
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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Monitoring.CustomEventEntryLogging;
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Management.Monitoring.Events
{
    public class EventObserver
    {
        #region Fields
        private static FileSystemWatcher _watcher;
        private Dictionary<string, EventLog> eventLogDictionary = new Dictionary<string, EventLog>();
        #endregion

        #region Events
        public event EventAddedEventHandler EventAdded;
        #endregion

        #region Methods

        #region EventViewerEvents

        #region Protected-Methods
        protected virtual void OnEventAdded(EventAddedArguments e)
        {
            EventAdded?.Invoke(this, e);
        }

        protected void EventLogEntryWritten(object sender, EntryWrittenEventArgs e)
        {
            if (eventLogDictionary.ContainsKey(e.Entry.Source))
            {
                EventViewerEvent eventViewerEntry = new EventViewerEvent(e.Entry);
                if (ServiceConfiguration.BindToIP != null) eventViewerEntry.Machine = ServiceConfiguration.BindToIP.ToString();

                OnEventAdded(new EventAddedArguments
                {
                    EventEntry = eventViewerEntry
                });


            }
        }
        #endregion

        #region Public-Methods
        public void RegisterEventViewerEvents(List<string> sources)
        {
#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                RegisterCustomEventsOnLinux();
                return;
            }
#endif
            UnRegisterEventViewerEvents();
            if (eventLogDictionary.Count == 0)
            {
                EventLog eventLog = new EventLog("Application", Dns.GetHostName());
                eventLog.EntryWritten += new EntryWrittenEventHandler(EventLogEntryWritten);
                eventLog.EnableRaisingEvents = true;
                foreach (string src in sources)
                {
                    if (eventLogDictionary.ContainsKey(src))
                        continue;
                    eventLogDictionary.Add(src, eventLog);
                }
            }
        }

        public void UnRegisterEventViewerEvents()
        {
            foreach (EventLog eventLog in eventLogDictionary.Values)
            {
                eventLog.EntryWritten -= new EntryWrittenEventHandler(EventLogEntryWritten);
            }
            eventLogDictionary.Clear();
        }
        #endregion

        #endregion

        #region CustomEventLogs Linux
        #region Protected-Methods
        protected void OnEventLogsFileUpdate(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Renamed)
                CustomEventEntryParser.filePointerPosition = 0;

            CustomEventEntryParser.ParseEventLog(e.FullPath);

            while (CustomEventEntryParser.customEventEntries.Count > 0)
            {
                var eventViewerEntry = new EventViewerEvent(CustomEventEntryParser.customEventEntries.Dequeue());
                if (ServiceConfiguration.BindToIP != null)
                    eventViewerEntry.Machine = ServiceConfiguration.BindToIP.ToString();
                else
                    eventViewerEntry.Machine = Environment.MachineName;

                OnEventAdded(new EventAddedArguments
                {
                    EventEntry = eventViewerEntry
                });

            }
        }
        #endregion

        #region Private-Methods
        private void RegisterCustomEventsOnLinux()
        {
            UnRegisterCustomEventsOnLinux();

            string installDirectory = AppUtil.InstallDir;
            _watcher = new FileSystemWatcher();
            _watcher.Path = installDirectory + Path.DirectorySeparatorChar + "log-files" + Path.DirectorySeparatorChar + "eventlogs";
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.Filter = "*.txt";
            _watcher.Changed += new FileSystemEventHandler(OnEventLogsFileUpdate);
            _watcher.EnableRaisingEvents = true;

            CustomEventEntryParser.timeStampAtEventRegistration = DateTime.Now;
        }

        private void UnRegisterCustomEventsOnLinux()
        {
            if (_watcher == null)
                return;
            _watcher.Changed -= new FileSystemEventHandler(OnEventLogsFileUpdate);
            _watcher.EnableRaisingEvents = false;
            _watcher = null;
        }
        #endregion
        #endregion

        #endregion

    }
}



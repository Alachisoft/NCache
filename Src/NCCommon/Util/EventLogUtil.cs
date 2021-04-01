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
using System.Diagnostics;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Common
{
    public class EventLogUtil
    {
        static int s_logLevel = 7;

        static EventLogUtil()
        {
            string logLevel = ServiceConfiguration.EventLogLevel;

            logLevel = logLevel.ToLower();
            switch (logLevel)
            {
                case "error":
                    s_logLevel = 1;
                    break;

                case "warning":
                    s_logLevel = 3;
                    break;

                case "all":
                    s_logLevel = 7;
                    break;
            }
           
        }

        /// <summary>
        /// Writes an error, warning, information, success audit, or failure audit 
        /// entry with the given message text to the event log.
        /// </summary>
        /// <param name="msg">The string to write to the event log.</param>
        /// <param name="type">One of the <c>EventLogEntryType</c> values.</param>
        public static void LogEvent(string source, string msg, EventLogEntryType type,short category,int eventId)
        {
            try
            {
                
                int level = (int)type;
                if ((level & s_logLevel) == level)
                {
                    using (EventLog ncLog = new EventLog("Application"))
                    {
                        ncLog.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 7);

                        ncLog.Source = source;
                        ncLog.WriteEntry(msg, type,eventId,category);
                    }
                }
            }
            catch (Exception) { }
        }

        public static void SetEventLogLevent()
        {
            string logLevel = ServiceConfiguration.EventLogLevel;

            logLevel = logLevel.ToLower();
            switch (logLevel)
            {
                case "error":
                    s_logLevel = 1;
                    break;

                case "warning":
                    s_logLevel = 3;
                    break;

                case "all":
                    s_logLevel = 7;
                    break;
            }
        }
    }
}
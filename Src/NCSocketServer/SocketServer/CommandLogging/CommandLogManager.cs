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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Util;
using System.IO;

namespace Alachisoft.NCache.SocketServer.CommandLogging
{
    public static class CommandLogManager
    {
        private static CmdLogger _cmdLogger;

        private static string _cacheName;

        public static void LogInfo(string clientID, string executionTime, string commandName, string commandParams)
        {
            if (ServiceConfiguration.EnableCommandThresholdLogging)
            {
                if (_cmdLogger != null)
                    _cmdLogger.Log(clientID, executionTime, commandName, commandParams);
                else
                {
                    string loggingPath = Path.Combine(AppUtil.LogDir, "log-files");
                    _cmdLogger = new CmdLogger(loggingPath,
                        "CommandLogging_" + _cacheName + "_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss"),
                        CmdLogger.LogType.Info);

                    _cmdLogger.Log(clientID, executionTime, commandName, commandParams);
                }
            }
        }

        internal static void InitializeLogger(string cacheName)
        {
            if (string.IsNullOrEmpty(cacheName))
                return;

            _cacheName = cacheName;
            if (ServiceConfiguration.EnableCommandThresholdLogging)
            {
                string loggingPath = Path.Combine(AppUtil.LogDir, "log-files");
                _cmdLogger = new CmdLogger(loggingPath, "CommandLogging_" + cacheName + "_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss"), CmdLogger.LogType.Info);
            }
        }
    }
}

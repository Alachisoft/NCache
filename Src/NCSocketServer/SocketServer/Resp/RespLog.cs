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

namespace Alachisoft.NCache.SocketServer.Resp
{
    /// <summary>
    /// Thin wrapper over the socket server logger.
    /// <para>
    /// The RESP listener runs inside the cache host and shares that process's log file and its
    /// runtime-togglable levels, so the existing null and level checks apply here too. Centralising
    /// them keeps every call site a one-liner and guarantees logging can never throw into a command
    /// path - a failure to log must never fail a cache operation.
    /// </para>
    /// </summary>
    internal static class RespLog
    {
        public static bool IsDetailedEnabled
        {
            get
            {
                try
                {
                    var logs = Alachisoft.NCache.SocketServer.SocketServer.Logger;
                    return logs != null && logs.IsDetailedLogsEnabled && logs.NCacheLog != null;
                }
                catch { return false; }
            }
        }

        /// <summary>Always written, regardless of the configured level. For lifecycle events.</summary>
        public static void Critical(string module, string message)
        {
            try
            {
                var logs = Alachisoft.NCache.SocketServer.SocketServer.Logger;
                if (logs != null && logs.NCacheLog != null) logs.NCacheLog.CriticalInfo(module, message);
            }
            catch { }
        }

        public static void Warn(string module, string message)
        {
            try
            {
                var logs = Alachisoft.NCache.SocketServer.SocketServer.Logger;
                if (logs != null && logs.NCacheLog != null) logs.NCacheLog.Warn(module, message);
            }
            catch { }
        }

        public static void Error(string module, string message)
        {
            try
            {
                var logs = Alachisoft.NCache.SocketServer.SocketServer.Logger;
                if (logs != null && logs.IsErrorLogsEnabled && logs.NCacheLog != null)
                    logs.NCacheLog.Error(module, message);
            }
            catch { }
        }

        /// <summary>Only written when detailed logs are on; safe to call per command.</summary>
        public static void Detail(string module, string message)
        {
            try
            {
                var logs = Alachisoft.NCache.SocketServer.SocketServer.Logger;
                if (logs != null && logs.IsDetailedLogsEnabled && logs.NCacheLog != null)
                    logs.NCacheLog.Info(module, message);
            }
            catch { }
        }
    }
}

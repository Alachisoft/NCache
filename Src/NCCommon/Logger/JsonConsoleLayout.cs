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
using log4net.Extended.Appender;
using log4net.Extended.Core;
using log4net.Extended.Layout;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Alachisoft.NCache.Common.Logger
{
    // Per-logger identity (one NCacheLogger instance = one cache/client/etc.).
    // Kept per-instance (NOT log4net GlobalContext) because one process can host
    // several loggers, each with its own source/instance/role.
    public sealed class LogIdentity
    {
        public string loggerName;
        public string source;     // cache-host|client|service|socket-server|eventlog
        public string instance;   // cache name (null where N/A)
        public string role;       // active|replica (cache only)
        public string node;       // IP / hostname
        public int    pid       = System.Diagnostics.Process.GetCurrentProcess().Id;
        public string procStart = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    // ------------------------------------------------------------------
    // JSON layout: renders each LoggingEvent as ONE compact JSON line + '\n'.
    // This is what `docker logs` shows when NCache runs in a container: the
    // file appenders keep writing log-files/ as before, this layout mirrors
    // the same events to the process console.
    // ------------------------------------------------------------------
    public sealed class JsonConsoleLayout : LayoutSkeleton
    {
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions { WriteIndented = false };
        private static long _seq = 0;   // per-process monotonic counter
        private readonly LogIdentity _id;

        public JsonConsoleLayout(LogIdentity id) { _id = id; IgnoresException = true; }

        public override void ActivateOptions() { }

        public override void Format(TextWriter writer, LoggingEvent e)
        {
            string message = e.RenderedMessage ?? string.Empty;
            if (e.ExceptionObject != null)
                message += "\n" + e.ExceptionObject.ToString();   // real newlines -> serializer escapes to \n

            MapLevel(e.Level, out string level, out bool isErr);

            var rec = new
            {
                app         = "ncache",
                source      = _id.source,
                ts          = e.TimeStamp.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                seq         = Interlocked.Increment(ref _seq),
                level       = level,
                instance    = _id.instance,
                role        = _id.role,
                node        = _id.node,
                pid         = _id.pid,
                procStart   = _id.procStart,
                appDomain   = e.Domain,
                logger      = e.LoggerName,
                thread      = e.ThreadName,
                message     = message,
                stream      = isErr ? "stderr" : "stdout",
                v           = 1,
            };

            writer.Write(JsonSerializer.Serialize(rec, JsonOpts));
            writer.Write('\n');   // single terminator; log4net serializes DoAppend so this write is atomic
        }

        // Handles NCache custom levels (CRIT=criticalInfo, DEV=devInfo) by NAME,
        // because their numeric values are huge and would misclassify.
        private static void MapLevel(Level lvl, out string name, out bool isErr)
        {
            switch (lvl.Name)
            {
                case "FATAL": name = "FATAL"; isErr = true;  return;
                case "ERROR": name = "ERROR"; isErr = true;  return;
                case "WARN":  name = "WARN";  isErr = true;  return;
                case "CRIT":  name = "INFO";  isErr = false; return;  // criticalInfo -> always-on INFO
                case "DEV":   name = "DEBUG"; isErr = false; return;  // devInfo
                case "INFO":  name = "INFO";  isErr = false; return;
                case "DEBUG": name = "DEBUG"; isErr = false; return;
                default:      name = "TRACE"; isErr = false; return;
            }
        }
    }

    // ------------------------------------------------------------------
    // Console appender factory. Two appenders: one on Console.Out, one on
    // Console.Error, both fed every level (the "stream" field in the JSON
    // tells the consumer which one the event belongs to). Returns a single
    // forwarding appender that fans out to both.
    // ------------------------------------------------------------------
    public static class ConsoleAppenderFactory
    {
        public static IAppender Create(LogIdentity id)
        {
            var fwd = new ForwardingAppender { Name = "ConsoleForward_" + id.instance };
            fwd.AddAppender(Out(id));
            fwd.AddAppender(Err(id));
            fwd.ActivateOptions();
            return fwd;
        }

        private static IAppender Out(LogIdentity id)
        {
            var a = new ConsoleAppender { Name = "ConsoleOut_" + id.instance, Target = "Console.Out", Layout = new JsonConsoleLayout(id) };
            a.Threshold = log4net.Extended.Core.Level.All;
            a.ActivateOptions();
            return a;
        }

        private static IAppender Err(LogIdentity id)
        {
            var a = new ConsoleAppender { Name = "ConsoleErr_" + id.instance, Target = "Console.Error", Layout = new JsonConsoleLayout(id) };
            a.Threshold = log4net.Extended.Core.Level.All;
            a.ActivateOptions();
            return a;
        }
    }
}

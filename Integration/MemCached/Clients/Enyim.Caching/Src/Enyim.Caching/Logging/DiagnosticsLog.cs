// Copyright (c) 2015 Alachisoft
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Configuration;

namespace Enyim.Caching
{
	public class DiagnosticsLogFactory : ILogFactory
	{
		private TextWriter writer;

		public DiagnosticsLogFactory() : this(ConfigurationManager.AppSettings["Enyim.Caching.Diagnostics.LogPath"]) { }

		public DiagnosticsLogFactory(string logPath)
		{
			if (String.IsNullOrEmpty(logPath))
				throw new ArgumentNullException(
					"Log path must be defined.  Add the following to configuration/appSettings: <add key=\"Enyim.Caching.Diagnostics.LogPath\" "
					+ "value=\"path to the log file\" /> or specify a valid path in in the constructor.");

			this.writer = new StreamWriter(logPath, true);
		}

		ILog ILogFactory.GetLogger(string name)
		{
			return new TextWriterLog(name, this.writer);
		}

		ILog ILogFactory.GetLogger(Type type)
		{
			return new TextWriterLog(type.FullName, this.writer);
		}
	}

	public class ConsoleLogFactory : ILogFactory
	{
		ILog ILogFactory.GetLogger(string name)
		{
			return new TextWriterLog(name, Console.Out);
		}

		ILog ILogFactory.GetLogger(Type type)
		{
			return new TextWriterLog(type.FullName, Console.Out);
		}
	}

	#region [ ILog implementation          ]

	internal class TextWriterLog : ILog
	{
		private const string PrefixDebug = "DEBUG";
		private const string PrefixInfo = "INFO";
		private const string PrefixWarn = "WARN";
		private const string PrefixError = "ERROR";
		private const string PrefixFatal = "FATAL";

		private TextWriter writer;
		private string name;

		public TextWriterLog(string name, TextWriter writer)
		{
			this.name = name;
			this.writer = writer;
		}

		private void Dump(string prefix, string message, params object[] args)
		{
			var line = String.Format("{0:yyyy-MM-dd' 'HH:mm:ss} [{1}] {2} {3} - ", DateTime.Now, prefix, Thread.CurrentThread.ManagedThreadId, this.name) + String.Format(message, args);

			lock (this.writer)
			{
				this.writer.WriteLine(line);
				this.writer.Flush();
			}
		}

		private void Dump(string prefix, object message)
		{
			var line = String.Format("{0:yyyy-MM-dd' 'HH:mm:ss} [{1}] {2} {3} - {4}", DateTime.Now, prefix, Thread.CurrentThread.ManagedThreadId, this.name, message);

			lock (this.writer)
			{
				this.writer.WriteLine(line);
				this.writer.Flush();
			}
		}

		bool ILog.IsDebugEnabled
		{
			get { return true; }
		}

		bool ILog.IsInfoEnabled
		{
			get { return true; }
		}

		bool ILog.IsWarnEnabled
		{
			get { return true; }
		}

		bool ILog.IsErrorEnabled
		{
			get { return true; }
		}

		bool ILog.IsFatalEnabled
		{
			get { return true; }
		}

		void ILog.Debug(object message)
		{
			this.Dump(PrefixDebug, message);
		}

		void ILog.Debug(object message, Exception exception)
		{
			this.Dump(PrefixDebug, message + " - " + exception);
		}

		void ILog.DebugFormat(string format, object arg0)
		{
			this.Dump(PrefixDebug, format, arg0);
		}

		void ILog.DebugFormat(string format, object arg0, object arg1)
		{
			this.Dump(PrefixDebug, format, arg0, arg1);
		}

		void ILog.DebugFormat(string format, object arg0, object arg1, object arg2)
		{
			this.Dump(PrefixDebug, format, arg0, arg1, arg2);
		}

		void ILog.DebugFormat(string format, params object[] args)
		{
			this.Dump(PrefixDebug, format, args);
		}

		void ILog.DebugFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.Dump(PrefixDebug, String.Format(provider, format, args));
		}

		void ILog.Info(object message)
		{
			this.Dump(PrefixInfo, message);
		}

		void ILog.Info(object message, Exception exception)
		{
			this.Dump(PrefixInfo, message + " - " + exception);
		}

		void ILog.InfoFormat(string format, object arg0)
		{
			this.Dump(PrefixInfo, format, arg0);
		}

		void ILog.InfoFormat(string format, object arg0, object arg1)
		{
			this.Dump(PrefixInfo, format, arg0, arg1);
		}

		void ILog.InfoFormat(string format, object arg0, object arg1, object arg2)
		{
			this.Dump(PrefixInfo, format, arg0, arg1, arg2);
		}

		void ILog.InfoFormat(string format, params object[] args)
		{
			this.Dump(PrefixInfo, format, args);
		}

		void ILog.InfoFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.Dump(PrefixInfo, String.Format(provider, format, args));
		}

		void ILog.Warn(object message)
		{
			this.Dump(PrefixWarn, message);
		}

		void ILog.Warn(object message, Exception exception)
		{
			this.Dump(PrefixWarn, message + " - " + exception);
		}

		void ILog.WarnFormat(string format, object arg0)
		{
			this.Dump(PrefixWarn, format, arg0);
		}

		void ILog.WarnFormat(string format, object arg0, object arg1)
		{
			this.Dump(PrefixWarn, format, arg0, arg1);
		}

		void ILog.WarnFormat(string format, object arg0, object arg1, object arg2)
		{
			this.Dump(PrefixWarn, format, arg0, arg1, arg2);
		}

		void ILog.WarnFormat(string format, params object[] args)
		{
			this.Dump(PrefixWarn, format, args);
		}

		void ILog.WarnFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.Dump(PrefixWarn, String.Format(provider, format, args));
		}

		void ILog.Error(object message)
		{
			this.Dump(PrefixError, message);
		}

		void ILog.Error(object message, Exception exception)
		{
			this.Dump(PrefixError, message + " - " + exception);
		}

		void ILog.ErrorFormat(string format, object arg0)
		{
			this.Dump(PrefixError, format, arg0);
		}

		void ILog.ErrorFormat(string format, object arg0, object arg1)
		{
			this.Dump(PrefixError, format, arg0, arg1);
		}

		void ILog.ErrorFormat(string format, object arg0, object arg1, object arg2)
		{
			this.Dump(PrefixError, format, arg0, arg1, arg2);
		}

		void ILog.ErrorFormat(string format, params object[] args)
		{
			this.Dump(PrefixError, format, args);
		}

		void ILog.ErrorFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.Dump(PrefixError, String.Format(provider, format, args));
		}

		void ILog.Fatal(object message)
		{
			this.Dump(PrefixFatal, message);
		}

		void ILog.Fatal(object message, Exception exception)
		{
			this.Dump(PrefixFatal, message + " - " + exception);
		}

		void ILog.FatalFormat(string format, object arg0)
		{
			this.Dump(PrefixFatal, format, arg0);
		}

		void ILog.FatalFormat(string format, object arg0, object arg1)
		{
			this.Dump(PrefixFatal, format, arg0, arg1);
		}

		void ILog.FatalFormat(string format, object arg0, object arg1, object arg2)
		{
			this.Dump(PrefixFatal, format, arg0, arg1, arg2);
		}

		void ILog.FatalFormat(string format, params object[] args)
		{
			this.Dump(PrefixFatal, format, args);
		}

		void ILog.FatalFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.Dump(PrefixFatal, String.Format(provider, format, args));
		}
	}

	#endregion
}

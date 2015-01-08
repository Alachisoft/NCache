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

namespace Enyim.Caching
{
	/// <summary>
	/// Creates an empty logger. Used when no other factories are installed.
	/// </summary>
	public class NullLoggerFactory : ILogFactory
	{
		ILog ILogFactory.GetLogger(string name)
		{
			return NullLogger.Instance;
		}

		ILog ILogFactory.GetLogger(Type type)
		{
			return NullLogger.Instance;
		}

		#region [ NullLogger                   ]

		private class NullLogger : ILog
		{
			internal static readonly ILog Instance = new NullLogger();

			private NullLogger() { }

			#region [ ILog                         ]

			bool ILog.IsDebugEnabled
			{
				get { return false; }
			}

			bool ILog.IsInfoEnabled
			{
				get { return false; }
			}

			bool ILog.IsWarnEnabled
			{
				get { return false; }
			}

			bool ILog.IsErrorEnabled
			{
				get { return false; }
			}

			bool ILog.IsFatalEnabled
			{
				get { return false; }
			}

			void ILog.Debug(object message) { }
			void ILog.Debug(object message, Exception exception) { }
			void ILog.DebugFormat(string format, object arg0) { }
			void ILog.DebugFormat(string format, object arg0, object arg1) { }
			void ILog.DebugFormat(string format, object arg0, object arg1, object arg2) { }
			void ILog.DebugFormat(string format, params object[] args) { }
			void ILog.DebugFormat(IFormatProvider provider, string format, params object[] args) { }
			void ILog.Info(object message) { }
			void ILog.Info(object message, Exception exception) { }
			void ILog.InfoFormat(string format, object arg0) { }
			void ILog.InfoFormat(string format, object arg0, object arg1) { }
			void ILog.InfoFormat(string format, object arg0, object arg1, object arg2) { }
			void ILog.InfoFormat(string format, params object[] args) { }
			void ILog.InfoFormat(IFormatProvider provider, string format, params object[] args) { }
			void ILog.Warn(object message) { }
			void ILog.Warn(object message, Exception exception) { }
			void ILog.WarnFormat(string format, object arg0) { }
			void ILog.WarnFormat(string format, object arg0, object arg1) { }
			void ILog.WarnFormat(string format, object arg0, object arg1, object arg2) { }
			void ILog.WarnFormat(string format, params object[] args) { }
			void ILog.WarnFormat(IFormatProvider provider, string format, params object[] args) { }
			void ILog.Error(object message) { }
			void ILog.Error(object message, Exception exception) { }
			void ILog.ErrorFormat(string format, object arg0) { }
			void ILog.ErrorFormat(string format, object arg0, object arg1) { }
			void ILog.ErrorFormat(string format, object arg0, object arg1, object arg2) { }
			void ILog.ErrorFormat(string format, params object[] args) { }
			void ILog.ErrorFormat(IFormatProvider provider, string format, params object[] args) { }
			void ILog.Fatal(object message) { }
			void ILog.Fatal(object message, Exception exception) { }
			void ILog.FatalFormat(string format, object arg0) { }
			void ILog.FatalFormat(string format, object arg0, object arg1) { }
			void ILog.FatalFormat(string format, object arg0, object arg1, object arg2) { }
			void ILog.FatalFormat(string format, params object[] args) { }
			void ILog.FatalFormat(IFormatProvider provider, string format, params object[] args) { }

			#endregion
		}

		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion

// Copyright (c) 2017 Alachisoft
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
	/// The ILog interface is used by the client to log messages.
	/// </summary>
	/// <remarks>Use the <see cref="T:Enyim.Caching.LogManager" /> class to programmatically assign logger implementations.</remarks>
	public interface ILog
	{
		bool IsDebugEnabled { get; }
		bool IsInfoEnabled { get; }
		bool IsWarnEnabled { get; }
		bool IsErrorEnabled { get; }
		bool IsFatalEnabled { get; }

		void Debug(object message);
		void Debug(object message, Exception exception);
		void DebugFormat(string format, object arg0);
		void DebugFormat(string format, object arg0, object arg1);
		void DebugFormat(string format, object arg0, object arg1, object arg2);
		void DebugFormat(string format, params object[] args);
		void DebugFormat(IFormatProvider provider, string format, params object[] args);

		void Info(object message);
		void Info(object message, Exception exception);
		void InfoFormat(string format, object arg0);
		void InfoFormat(string format, object arg0, object arg1);
		void InfoFormat(string format, object arg0, object arg1, object arg2);
		void InfoFormat(string format, params object[] args);
		void InfoFormat(IFormatProvider provider, string format, params object[] args);

		void Warn(object message);
		void Warn(object message, Exception exception);
		void WarnFormat(string format, object arg0);
		void WarnFormat(string format, object arg0, object arg1);
		void WarnFormat(string format, object arg0, object arg1, object arg2);
		void WarnFormat(string format, params object[] args);
		void WarnFormat(IFormatProvider provider, string format, params object[] args);

		void Error(object message);
		void Error(object message, Exception exception);
		void ErrorFormat(string format, object arg0);
		void ErrorFormat(string format, object arg0, object arg1);
		void ErrorFormat(string format, object arg0, object arg1, object arg2);
		void ErrorFormat(string format, params object[] args);
		void ErrorFormat(IFormatProvider provider, string format, params object[] args);

		void Fatal(object message);
		void Fatal(object message, Exception exception);
		void FatalFormat(string format, object arg0);
		void FatalFormat(string format, object arg0, object arg1);
		void FatalFormat(string format, object arg0, object arg1, object arg2);
		void FatalFormat(string format, params object[] args);
		void FatalFormat(IFormatProvider provider, string format, params object[] args);
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

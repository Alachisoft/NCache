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
//Copyright (c) 2007-2008 Henrik Schr�der, Oliver Kofoed Pedersen

//Permission is hereby granted, free of charge, to any person
//obtaining a copy of this software and associated documentation
//files (the "Software"), to deal in the Software without
//restriction, including without limitation the rights to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the
//Software is furnished to do so, subject to the following
//conditions:

//The above copyright notice and this permission notice shall be
//included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//OTHER DEALINGS IN THE SOFTWARE.

using System;

namespace BeIT.MemCached {
	internal class LogAdapter {
		public static LogAdapter GetLogger(Type type) {
			return new LogAdapter(type);
		}

		public static LogAdapter GetLogger(string name) {
			return new LogAdapter(name);
		}

		/*
		 * The problem with logging on the .Net platform is that there is no common logging framework, and 
		 * everyone seems to have their own favorite. We wanted this project to compile straight away
		 * without external dependencies, and we want you to be able to embed it directly into your code,
		 * without having to add references to some other logging framework.
		 * 
		 * Therefore, the MemcachedClient code uses this LogAdapter to add flexible logging.
		 * By default, it is implemented as simple console logging.
		 * 
		 * If you are using log4net, simply comment out the console logging code, uncomment the log4net code,
		 * add the using statement, and make sure your project references log4net.
		 * 
		 * If you are using some other logging framework, feel free to implement your own version of this LogAdapter.
		 */

		//Console Implementation
		private string loggerName;
		private LogAdapter(string name) { loggerName = name; }
		private LogAdapter(Type type) { loggerName = type.FullName; }
		public void Debug(string message) { Console.Out.WriteLine(DateTime.Now + " DEBUG " + loggerName + " - " + message); }
		public void Info(string message) { Console.Out.WriteLine(DateTime.Now + " INFO " + loggerName + " - " + message); }
		public void Warn(string message) { Console.Out.WriteLine(DateTime.Now + " WARN " + loggerName + " - " + message); }
		public void Error(string message) { Console.Out.WriteLine(DateTime.Now + " ERROR " + loggerName + " - " + message); }
		public void Fatal(string message) { Console.Out.WriteLine(DateTime.Now + " FATAL " + loggerName + " - " + message); }
		public void Debug(string message, Exception e) { Console.Out.WriteLine(DateTime.Now + " DEBUG " + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace); }
		public void Info(string message, Exception e) { Console.Out.WriteLine(DateTime.Now + " INFO " + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace); }
		public void Warn(string message, Exception e) { Console.Out.WriteLine(DateTime.Now + " WARN " + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace); }
		public void Error(string message, Exception e) { Console.Out.WriteLine(DateTime.Now + " ERROR " + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace); }
		public void Fatal(string message, Exception e) { Console.Out.WriteLine(DateTime.Now + " FATAL " + loggerName + " - " + message + "\n" + e.Message + "\n" + e.StackTrace); }

		//Empty logging Implementation
		/*
		public void Debug(string message) {}
		public void Info(string message) { }
		public void Warn(string message) { }
		public void Error(string message) { }
		public void Fatal(string message) { }
		public void Debug(string message, Exception e) { }
		public void Info(string message, Exception e) { }
		public void Warn(string message, Exception e) { }
		public void Error(string message, Exception e) { }
		public void Fatal(string message, Exception e) { }
		*/

		//Log4net Implementation
		/*
		private log4net.ILog logger;
		private LogAdapter(string name) { logger = log4net.LogManager.GetLogger(name); }
		private LogAdapter(Type type) { logger = log4net.LogManager.GetLogger(type); }
		public void Debug(string message) { logger.Debug(message); }
		public void Info(string message) { logger.Info(message); }
		public void Warn(string message) { logger.Warn(message); }
		public void Error(string message) { logger.Error(message); }
		public void Fatal(string message) { logger.Fatal(message); }
		public void Debug(string message, Exception e) { logger.Debug(message, e); }
		public void Info(string message, Exception e) { logger.Info(message, e); }
		public void Warn(string message, Exception e) { logger.Warn(message, e); }
		public void Error(string message, Exception e) { logger.Error(message, e); }
		public void Fatal(string message, Exception e) { logger.Fatal(message, e); }
		*/
	}
}

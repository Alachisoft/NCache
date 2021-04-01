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
using System.Collections;

namespace Alachisoft.NCache.Common.Logger
{
    public interface ILogger
    {
        /// <summary>
        /// Start the cache logging functionality.
        /// </summary>
        string Initialize(IDictionary properties, string partitionID, string cacheName);

        /// <summary>
        /// Start the cache logging functionality.
        /// </summary>
        string Initialize(IDictionary properties, string partitionID, string cacheName, bool isStartedAsMirror, bool inproc);

        /// <summary>
        /// intitializes Known name based log files (will not log License Logs at service Startup
        /// </summary>
        /// <param name="loggerName">Enum of Known loggerNames</param>
        void Initialize(LoggerNames loggerName);

         /// <summary>
        /// intitializes Known name based log files
        /// </summary>
        /// <param name="loggerName">Enum of Known loggerNames</param>
        /// <param name="cacheName">cacheName if Clientcache logging, if not ClientCache, use the other override</param>
        void Initialize(LoggerNames loggerNameEnum, string cacheName);

        /// <summary>
        /// Stop the cache logging functionality.
        /// </summary>
        void Close();

        void Flush();

        void SetLevel(string levelName);

        void Error(string message);

        void Error(string module, string message);

        void Fatal(string message);

        void Fatal(string module, string message);

        void CriticalInfo(string message);

        void CriticalInfo(string module, string message);

        void DevTrace(string message);

        void DevTrace(string module, string message);

        void Info(string message);

        void Info(string module, string message);

        void Debug(string message);

        void Debug(string module, string message);

        void Warn(string message);

        void Warn(string module, string message);

        bool IsInfoEnabled { get; }

        bool IsErrorEnabled{ get;  }

        bool IsWarnEnabled { get;  }

        bool IsDebugEnabled { get;  }

        bool IsFatalEnabled { get;  }
    
    }
}

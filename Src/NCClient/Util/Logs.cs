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

using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Create logs
    /// </summary>
    internal class Logs
    {
        private bool _errorLogsEnabled;
        private bool _detailedLogsEnabled;
        private ILogger _ncacheLog;

        /// <summary>
        /// Creates NCache logs
        /// </summary>
        internal ILogger NCacheLog
        {
            get { return _ncacheLog; }
            set { _ncacheLog = value; }
        }

        /// <summary>
        /// Enables/disables error logs.
        /// </summary>
        internal bool IsErrorLogsEnabled
        {
            get { return _errorLogsEnabled; }
            set { _errorLogsEnabled = value; }
        }

        /// <summary>
        /// Enables/disables detailed logs.
        /// </summary>
        internal bool IsDetailedLogsEnabled
        {
            get { return _detailedLogsEnabled; }
            set { _detailedLogsEnabled = value; }
        }
    }
}

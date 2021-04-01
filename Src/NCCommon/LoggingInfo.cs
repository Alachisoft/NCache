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
using System.Collections.Generic;

namespace Alachisoft.NCache.Common
{
    /// <summary>
    /// Provide information about client or server logging.
    /// </summary>
    [Serializable]
    public sealed class LoggingInfo : Runtime.Serialization.ICompactSerializable
    {
        private IDictionary<LoggingType, LogsStatus> _logMap;

        /// <summary>
        /// Defines the subsystem
        /// </summary>
        public enum LoggingSubsystem
        {
            /// <summary>
            /// Socket server
            /// </summary>
            Server,

            /// <summary>
            /// Remote clients
            /// </summary>
            Client
        }

        /// <summary>
        /// Defines status of client and server side logging
        /// </summary>
        public enum LogsStatus
        {
            /// <summary>
            /// Disable logging
            /// </summary>
            Disable = 0,

            /// <summary>
            /// Enable logging
            /// </summary>
            Enable,

            /// <summary>
            /// Keep the current value
            /// </summary>
            //Unchanged
        }

        /// <summary>
        /// Type of logging
        /// </summary>
        public enum LoggingType
        {
            /// <summary>
            /// Log only exception and unexpected behaviours
            /// </summary>
            Error = 0x1,

            /// <summary>
            /// Log all information related to important operations
            /// </summary>
            Detailed = 0x2
        }

        /// <summary>
        /// Create a new logging information
        /// </summary>
        public LoggingInfo()
        {
            this._logMap = new Dictionary<LoggingType, LogsStatus>(2);

            this._logMap.Add(LoggingType.Error, LogsStatus.Disable);
            this._logMap.Add(LoggingType.Detailed, LogsStatus.Disable);
        }

        /// <summary>
        /// Set logging status
        /// </summary>
        /// <param name="type">Type of logging to set status for</param>
        /// <param name="status">Status of logging</param>
        public void SetStatus(LoggingType type, LogsStatus status)
        {
            this._logMap[type] = status;
        }

        /// <summary>
        /// Get logging status for a specified type
        /// </summary>
        /// <param name="type">Type of logging</param>
        /// <returns>Logging status for that type</returns>
        public LogsStatus GetStatus(LoggingType type)
        {
            return this._logMap[type];            

        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _logMap=Common.Util.SerializationUtility.DeserializeDictionary<LoggingType,LogsStatus>(reader);
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            Common.Util.SerializationUtility.SerializeDictionary(_logMap,writer);
        } 
        #endregion
    }
}

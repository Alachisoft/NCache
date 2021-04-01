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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// This exception is thrown whenever an error occurs on remote node.
    /// </summary>
    [Serializable]
    public class MaxClientReachedException : CacheException
    {
        private string _stackTrace = "";
        private string _message = "";
        /// <summary> 
        /// Default constructor. 
        /// </summary>
        public MaxClientReachedException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">errorcode for the exception</param>
        /// <param name="reason">reason for exception</param>
        public MaxClientReachedException(int errorCode,string reason)
          : base(errorCode,reason)
        {
        }

        /// <summary>
        /// Gets the stack trace of the exception
        /// </summary>
        public override string StackTrace
        {
            get
            {
                return _stackTrace + base.StackTrace;
            }
        }

        /// <summary>
        /// Gets the message that describes the current exception.
        /// </summary>
        public override string Message
        {
            get
            {
                return _message;
            }
        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected MaxClientReachedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _stackTrace = info.GetString("_ncstackTrace");
            _message = info.GetString("_ncMessage");
        }

        /// <summary>
        /// Manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_ncstackTrace", _stackTrace);
            info.AddValue("_ncMessage", _message);
        }

        #endregion
    }
}
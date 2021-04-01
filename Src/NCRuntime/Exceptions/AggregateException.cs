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
using System.Collections;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// This exception is thrown when multiple exceptions occur from multiple nodes. It combines all
    /// the exceptions as an inner exception and throw it to the client application.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(AggregateException ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class AggregateException : CacheException
    {
        /// <summary>
        /// 
        /// </summary>
        private Exception[] _exceptions;

        /// <summary> 
        /// It takes the exceptions that are the cause of the current exception as parameter. 
        /// </summary>
        /// <param name="exceptions">The exceptions that are the cause of the current exception</param>
        public AggregateException(params Exception[] exceptions)
        {
            _exceptions = exceptions;
        }

        /// <summary> 
        /// It takes the exceptions that are the cause of the current exception as parameter. 
        /// </summary>
        /// <param name="exceptions">The exceptions that are the cause of the current exception</param>
        public AggregateException(ArrayList exceptions)
        {
            if (exceptions != null)
                _exceptions = (Exception[])exceptions.ToArray(typeof(Exception));
        }

        /// <summary> 
        /// Overloaded constructor, takes the reason as additional parameter. 
        /// </summary>
        /// <param name="reason">The reason for exception</param>
        /// <param name="exceptions">The exceptions that are the cause of the current exception</param>
        public AggregateException(string reason, ArrayList exceptions)
            : base(reason)
        {
            if (exceptions != null)
                _exceptions = (Exception[])exceptions.ToArray(typeof(Exception));
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="exceptions">The exceptions that are the cause of the current exception</param>
        public AggregateException(int errorCode,string reason, ArrayList exceptions)
         : base(errorCode,reason)
        {
            if (exceptions != null)
                _exceptions = (Exception[])exceptions.ToArray(typeof(Exception));
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="exceptions">The exceptions that are the cause of the current exception</param>
        /// <param name="stackTrace">stacktrace for exception</param>
        public AggregateException(int errorCode, string reason, ArrayList exceptions,string stackTrace)
       : base(errorCode, reason,stackTrace)
        {
            if (exceptions != null)
                _exceptions = (Exception[])exceptions.ToArray(typeof(Exception));
        }
        /// <summary>
        /// The exceptions that are the cause of the current exception.
        /// </summary>
        public Exception[] InnerExceptions
        {
            get { return _exceptions; }
        }

        /// <summary>
        /// The message of the exception. This message is the aggregate message from multiple exceptions.
        /// </summary>
        public override string Message
        {
            get
            {
                if (_exceptions != null && _exceptions.Length > 0)
                {
                    string aggregateMsg = "Aggregate Exception was found";
                    for (int i = 0; i < _exceptions.Length; i++)
                    {
                        aggregateMsg += "\r\n";
                        aggregateMsg += "Exception:" + (i + 1) + " ";
                        aggregateMsg += _exceptions[i].ToString();
                    }
                    aggregateMsg += "\r\n";
                    return aggregateMsg;
                }
                return base.Message;
            }
        }

        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected AggregateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }
}
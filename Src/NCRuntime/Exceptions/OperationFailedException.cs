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
    /// This exception is thrown whenever an API fails. In case of bulk operation, you even receive 
    /// information about existing keys or unavailable space wrapped within this exception.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(OperationFailedException ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class OperationFailedException : CacheException
    {
        private bool _isTracable = true;
        /// <summary> 
        /// Default constructor. 
        /// </summary>
        public OperationFailedException()
        {
        }

        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public OperationFailedException(string reason)
            : base(reason)
        {
        }

        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public OperationFailedException(string reason, bool isTracable)
            : base(reason)
        {
            this._isTracable = isTracable;
        }

        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public OperationFailedException(string reason, Exception inner)
            : base(reason, inner)
        {
        }

        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public OperationFailedException(string reason, Exception inner, bool isTracable)
            : base(reason, inner)
        {
            this._isTracable = isTracable;
        }

        /// <summary>
        /// Specifies whether the exception is to be logged or not
        /// </summary>
        public bool IsTracable
        {
            get { return _isTracable; }
        }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        public OperationFailedException(int errorCode):base(errorCode)
        {
        }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">Exception message</param>
        public OperationFailedException(int errorCode,string reason)
           : base(errorCode,reason)
        {
        }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">Exception message</param>
        /// <param name="isTracable"></param>
        public OperationFailedException(int errorCode,string reason, bool isTracable)
          : base(errorCode,reason)
        {
            this._isTracable = isTracable;
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned errorcode</param>
        /// <param name="reason">Exception message</param>
        /// <param name="inner">Nested Exception</param>
        public OperationFailedException(int errorCode,string reason, Exception inner)
        : base(errorCode,reason, inner)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">Nested exception</param>
        /// <param name="isTracable"></param>
        public OperationFailedException(int errorCode, string reason, Exception inner,bool isTracable)
        : base(errorCode, reason, inner)
        {
        }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="stackTrace">stacktrace</param>
        public OperationFailedException(int errorCode, string reason, string stackTrace)
            : base(errorCode, reason, stackTrace)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="isTracable"></param>
        /// <param name="stackTrace">stacktrace</param>
        public OperationFailedException(int errorCode, string reason, bool isTracable, string stackTrace) : base(errorCode, reason,stackTrace)
        {

        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected OperationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _isTracable = Convert.ToBoolean(info.GetString("_isTracable"));
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
            info.AddValue("_isTracable", _isTracable);
        }

        #endregion
    }
}
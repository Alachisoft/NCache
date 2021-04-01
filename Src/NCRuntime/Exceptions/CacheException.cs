//  Copyright (c) 2018 Alachisoft
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
using System.Security.Permissions;
using System.Runtime.Serialization;
using Alachisoft.NCache.Common;
using System.Text;
///<summary>
/// Different exceptions that are likely to occur in the client applications
/// are defined in this namespace. These exceptions can be caught in the client applications
/// and properly dealt with once this namespace is referenced.
///</summary>

namespace Alachisoft.NCache.Runtime.Exceptions
{

    /// <summary>
    /// It is the base class for all the exceptions that are thrown from NCache. 
    /// So you can catch this exception for all the exceptions thrown from within the cache.
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    ...
    /// }
    /// catch(CacheException ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class CacheException : Exception
    {
        private string _stackTrace="";
        //		private string		_sourceMachine = string.Empty;
        //		private string		_sourceInstance = string.Empty;
       
        /// <summary>
        /// Property for setting ErrorCode 
        /// </summary>
        public int ErrorCode { get; set; }
       
        /// <summary>
        /// Property for setting stack trace
        /// </summary>
        public override string StackTrace { get { return _stackTrace + base.StackTrace; } }
        /// <summary> 
        /// Default constructor. 
        /// </summary>
        public CacheException() { }
     
        /// <summary> 
        /// Overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public CacheException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// Overloaded constructor. 
        /// </summary>
        /// <param name="reason">Reason for exception</param>
        /// <param name="inner">Nested exception</param>
        public CacheException(string reason, Exception inner)
#if DEBUG
            : base($"{reason} : {inner.ToString()}", inner)
#else
            : base(reason, inner)
#endif
        {
        }
      
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        public CacheException(int errorCode)
        {
            ErrorCode = errorCode;
        }
       
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="exception">nested exception</param>
        public CacheException(int errorCode,Exception exception):base()
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">reason for exception</param>
        public CacheException(int errorCode, string reason) : base(reason)
        {
            ErrorCode = errorCode;
        }
      
        /// <summary>
        /// overloaded exception
        /// </summary>
        /// <param name="errorCode">asigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="stackTrace">stacktrace</param>
        public CacheException(int errorCode, string reason, string stackTrace) : base(reason)
        {
            ErrorCode = errorCode;
            _stackTrace = stackTrace;
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">nested exception</param>
        public CacheException(int errorCode, string reason, Exception inner) : this(reason,inner)
        {
            ErrorCode = errorCode;
        }
        //		public string	SourceMachine { get { return this._sourceMachine; } }
        //		public string	SourceInstance { get { return this._sourceInstance; } }

        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// Overloaded constructor, manual serialization. 
        /// </summary>
        protected CacheException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCode = info.GetInt32("errorCode");
            _stackTrace = info.GetString("stackTrace");
        }

        /// <summary>
        /// Manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("errorCode", ErrorCode);
            info.AddValue("stackTrace", StackTrace);
        }

        #endregion

        //		static public Exception WrapException(Exception e)
        //		{
        //			CacheException ce = e is CacheException ? (CacheException)e:new CacheException(e.Message, e);
        //			ce._sourceMachine = Environment.MachineName;
        //			ce._sourceInstance = "";
        //			return ce;
        //		}

        /// <summary>
        /// Represents the exception in string form with type and stack trace
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{this.GetType()} : {Message}");
            sb.AppendLine(this.StackTrace);
            return sb.ToString();
        }
    }
}


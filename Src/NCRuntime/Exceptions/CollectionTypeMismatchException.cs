using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace Alachisoft.NCache.Runtime.Exceptions
{


    /// <summary>
    /// Thrown whenever an API fails. In case of GetCollection with invalid Type 
    /// if created collection on cache store is of differente type then it throws this type of exception
    /// </summary>
    /// <example>The following example demonstrates how to use this exception in your code.
    /// <code>
    /// 
    /// try
    /// {
    ///	    IDistributedList<int> distributedList = cache.Collections.CreateList<int>("key");
    ///	    IDistributedQueue<int> distributedQueue = cache.Collections.GetQueue<int>("key");
    /// }
    /// catch(CollectionTypeMismatch ex)
    /// {
    ///     ...
    /// }
    /// 
    /// </code>
    /// </example>
    [Serializable]
    public class CollectionTypeMismatchException : CacheException
    {
        private bool _isTracable = true;
        /// <summary> 
        /// default constructor. 
        /// </summary>
        public CollectionTypeMismatchException()
        {
        }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public CollectionTypeMismatchException(string reason)
            : base(reason)
        {
        }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public CollectionTypeMismatchException(string reason, bool isTracable)
            : base(reason)
        {
            this._isTracable = isTracable;
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public CollectionTypeMismatchException(string reason, Exception inner)
            : base(reason, inner)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public CollectionTypeMismatchException(string reason, Exception inner, bool isTracable)
            : base(reason, inner)
        {
            this._isTracable = isTracable;
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        public CollectionTypeMismatchException(int errorCode,string reason)
          : base(errorCode,reason)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="stackTrace">stacktrace for exception</param>
        public CollectionTypeMismatchException(int errorCode, string reason,string stackTrace)
         : base(errorCode, reason,stackTrace)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="isTracable"></param>
        public CollectionTypeMismatchException(int errorCode,string reason, bool isTracable)
          : base(errorCode,reason)
        {
            this._isTracable = isTracable;
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">nested exception</param>
        public CollectionTypeMismatchException(int errorCode,string reason, Exception inner)
           : base(errorCode,reason, inner)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">nested exception</param>
        /// <param name="isTracable"></param>
        public CollectionTypeMismatchException(int errorCode,string reason, Exception inner, bool isTracable)
          : base(errorCode,reason, inner)
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

        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected CollectionTypeMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _isTracable = Convert.ToBoolean(info.GetString("_isTracable"));
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
            info.AddValue("_isTracable", _isTracable);
        }

        #endregion
    }
}

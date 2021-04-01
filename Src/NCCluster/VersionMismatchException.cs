using System;
using System.Security.Permissions;
using System.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NGroups
{
    [Serializable]
    public class VersionMismatchException:CacheException, ISerializable
    {
        /// <summary>
        /// Thrown when an exception occurs during configuration. Likely causes are badly specified
        /// configuration strings.
        /// </summary>

        /// <summary> 
        /// default constructor. 
        /// </summary>
        internal VersionMismatchException() { }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        internal VersionMismatchException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        internal VersionMismatchException(string reason, Exception inner)
            : base(reason, inner)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">reason for exception</param>
        internal VersionMismatchException(int errorCode,string reason)
          : base(errorCode,reason)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">reason for exception</param>
        /// <param name="stackTrace">stacktrace for exception</param>
        internal VersionMismatchException(int errorCode, string reason,string stackTrace)
        : base(errorCode, reason,stackTrace)
        {
        }
        /// <summary>
        /// overloaded constructor
        /// </summary>
        /// <param name="errorCode">assigned errorcode</param>
        /// <param name="reason">exception message</param>
        /// <param name="inner">nested exception</param>
        internal VersionMismatchException(int errorCode,string reason, Exception inner)
           : base(errorCode,reason, inner)
        {
        }
        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected VersionMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// manual serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }
}


using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// Thrown when a user does not have the permissions to start/stop/initialize the cache.
    /// </summary>
    [Serializable]

    //#if !NEWEXPRESS
    public class SecurityException : CacheException
//#else
//    internal class SecurityException : CacheException
//#endif
    {
        /// <summary> 
        /// default constructor. 
        /// </summary>
        public SecurityException() { }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public SecurityException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public SecurityException(string reason, Exception inner)
            : base(reason, inner)
        {
        }

        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected SecurityException(SerializationInfo info, StreamingContext context)
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
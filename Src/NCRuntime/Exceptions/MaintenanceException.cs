using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace Alachisoft.NCache.Runtime.Exceptions
{
    /// <summary>
    /// Thrown when the cluster is under maintenance
    /// </summary>
    [Serializable]
    public class MaintenanceException : ManagementException
    {
        /// <summary> 
        /// default constructor. 
        /// </summary>
        public MaintenanceException() { }

        /// <summary> 
        /// overloaded constructor, takes the reason as parameter. 
        /// </summary>
        public MaintenanceException(string reason)
            : base(reason)
        {
        }

        /// <summary>
        /// overloaded constructor. 
        /// </summary>
        /// <param name="reason">reason for exception</param>
        /// <param name="inner">nested exception</param>
        public MaintenanceException(string reason, Exception inner)
            : base(reason, inner)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        public MaintenanceException(int errorCode):base(errorCode) { }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">Excption message</param>
        public MaintenanceException(int errorCode,string reason)
           : base(errorCode,reason)
        {
        }
        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="errorCode">Assigned ErrorCode</param>
        /// <param name="reason">Exception message</param>
        /// <param name="stackTrace">stacktrace</param>
        public MaintenanceException(int errorCode, string reason,string stackTrace)
         : base(errorCode, reason,stackTrace)
        {
        }
        /// <summary>
        /// Overloaded Constructor
        /// </summary>
        /// <param name="errorCode">Assigned Errorcode</param>
        /// <param name="reason">Exception message</param>
        /// <param name="inner">nested exception</param>
        public MaintenanceException(int errorCode,string reason, Exception inner)
           : base(errorCode,reason, inner)
        {
        }

        #region /                 --- ISerializable ---           /

        /// <summary> 
        /// overloaded constructor, manual serialization. 
        /// </summary>
        protected MaintenanceException(SerializationInfo info, StreamingContext context)
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

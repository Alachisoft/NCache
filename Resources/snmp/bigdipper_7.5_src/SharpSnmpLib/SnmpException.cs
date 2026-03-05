/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/4/23
 * Time: 19:40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
#if (!SILVERLIGHT)
using System.Runtime.Serialization; 
#endif

namespace Lextm.SharpSnmpLib
{    
    /// <summary>
    /// Base exception type of #SNMP.
    /// </summary>
    [Serializable]
    public class SnmpException : Exception
    {
        /// <summary>
        /// Creates a <see cref="SnmpException"/>.
        /// </summary>
        public SnmpException() 
        { 
        }
        
        /// <summary>
        /// Creates a <see cref="SnmpException"/> instance with a specific <see cref="String"/>.
        /// </summary>
        /// <param name="message">Message</param>
        public SnmpException(string message) : base(message) 
        {
        }
        
        /// <summary>
        /// Creates a <see cref="SnmpException"/> instance with a specific <see cref="String"/> and an <see cref="Exception"/>.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="inner">Inner exception</param>
        public SnmpException(string message, Exception inner) 
            : base(message, inner) 
        {
        }

#if (!SILVERLIGHT && !CF) 
        /// <summary>
        /// Creates a <see cref="SnmpException"/> instance.
        /// </summary>
        /// <param name="info">Info</param>
        /// <param name="context">Context</param>
        protected SnmpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
       
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="SnmpException"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Details;
        }        
     
        /// <summary>
        /// Details on operation.
        /// </summary>
        protected virtual string Details
        {
            get
            {
                return base.ToString();
            }
        }
    }
}
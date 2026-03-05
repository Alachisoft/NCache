// Timeout exception type.
// Copyright (C) 2008-2010 Malcolm Crowe, Lex Li, and other contributors.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/4/23
 * Time: 19:40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Globalization;
using System.Net;
#if (!SILVERLIGHT)
using System.Runtime.Serialization;
using System.Security.Permissions; 
#endif

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Timeout exception type of #SNMP.
    /// </summary>
    [Serializable]
    public sealed class TimeoutException : OperationException
    {
        /// <summary>
        /// The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.
        /// </summary>
        public int Timeout { get; private set; }

        /// <summary>
        /// Creates a <see cref="TimeoutException"/> instance.
        /// </summary>
        public TimeoutException() 
        {
        }
        
        /// <summary>
        /// Creates a <see cref="TimeoutException"/> instance with a specific <see cref="string"/>.
        /// </summary>
        /// <param name="message">Message</param>
        public TimeoutException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// Creates a <see cref="TimeoutException"/> instance with a specific <see cref="string"/> and an <see cref="Exception"/> instance.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="inner">Inner exception</param>
        public TimeoutException(string message, Exception inner) : base(message, inner)
        {
        }

#if (!SILVERLIGHT && !CF) 
        private TimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Timeout = info.GetInt32("Timeout");
        }

        /// <summary>
        /// Gets object data.
        /// </summary>
        /// <param name="info">Info</param>
        /// <param name="context">Context</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Timeout", Timeout);
        } 
#endif
        
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="TimeoutException"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "TimeoutException: timeout: {0}", Timeout.ToString(CultureInfo.InvariantCulture));
        }
        
        /// <summary>
        /// Creates a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="agent">Agent address</param>
        /// <param name="timeout">Timeout</param>
        /// <returns></returns>
        public static TimeoutException Create(IPAddress agent, int timeout)
        {
            if (agent == null)
            {
                throw new ArgumentNullException("agent");
            }
            
            var ex = new TimeoutException { Agent = agent, Timeout = timeout };
            return ex;
        }
    }
}
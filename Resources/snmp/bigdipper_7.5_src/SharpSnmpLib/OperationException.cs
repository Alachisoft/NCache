// Operation exception type.
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

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Operation exception of #SNMP.
    /// </summary>
    [Serializable]
    public class OperationException : SnmpException
    {
        /// <summary>
        /// Agent address.
        /// </summary>
        protected IPAddress Agent { get; set; }

        /// <summary>
        /// Creates a <see cref="OperationException"/> instance.
        /// </summary>
        public OperationException()
        {
        }
        
        /// <summary>
        /// Creates a <see cref="OperationException"/> instance with a specific <see cref="string"/>.
        /// </summary>
        /// <param name="message"></param>
        public OperationException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// Creates a <see cref="OperationException"/> instance with a specific <see cref="string"/> and an <see cref="Exception"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public OperationException(string message, Exception inner) : base(message, inner) 
        { 
        }
#if (!SILVERLIGHT && !CF)    
        /// <summary>
        /// Creates a <see cref="OperationException"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected OperationException(SerializationInfo info, StreamingContext context) : base(info, context) 
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            
            Agent = (IPAddress)info.GetValue("Agent", typeof(IPAddress));
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
            info.AddValue("Agent", Agent);
        }
#endif
        /// <summary>
        /// Details on operation.
        /// </summary>
        protected override string Details
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}. Agent: {1}", Message, Agent);
            }
        }
     
        /// <summary>
        /// Creates a <see cref="OperationException"/> with a specific <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="agent">Agent address</param>
        public static OperationException Create(string message, IPAddress agent)
        {
            var ex = new OperationException(message) { Agent = agent };
            return ex;
        }
    }
}
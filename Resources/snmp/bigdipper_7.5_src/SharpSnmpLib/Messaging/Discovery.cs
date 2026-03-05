// Discovery type.
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
 * Date: 5/24/2009
 * Time: 11:56 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Discovery class that participates in SNMP v3 discovery process.
    /// </summary>
    public sealed class Discovery
    {
        private readonly GetRequestMessage _discovery;
        private static readonly UserRegistry Empty = new UserRegistry();
        private static readonly SecurityParameters DefaultSecurityParameters =
            new SecurityParameters(
                OctetString.Empty,
                Integer32.Zero,
                Integer32.Zero,
                OctetString.Empty,
                OctetString.Empty,
                OctetString.Empty);
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Discovery"/> class.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="messageId">The message id.</param>
        /// <param name="maxMessageSize">The max size of message.</param>
        public Discovery(int messageId, int requestId, int maxMessageSize)
        {
            _discovery = new GetRequestMessage(
                VersionCode.V3,
                new Header(
                    new Integer32(messageId),
                    new Integer32(maxMessageSize),
                    Levels.Reportable),
                DefaultSecurityParameters,
                new Scope(
                    OctetString.Empty,
                    OctetString.Empty,
                    new GetRequestPdu(requestId, new List<Variable>())),
                DefaultPrivacyProvider.DefaultPair,
                null);
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="receiver">The receiver.</param>
        /// <returns></returns>
        public ReportMessage GetResponse(int timeout, IPEndPoint receiver)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }
            
            using (var socket = receiver.GetSocket())
            {
                return (ReportMessage)_discovery.GetResponse(timeout, receiver, Empty, socket);
            }
        }

        /// <summary>
        /// Converts to the bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return _discovery.ToBytes();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "discovery class: message id: {0}; request id: {1}", _discovery.MessageId(), _discovery.RequestId());
        }
    }
}

// SET request message type.
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

using System;
using System.Collections.Generic;
using System.Globalization;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// SET request message.
    /// </summary>
    public sealed class SetRequestMessage : ISnmpMessage
    {
        private readonly byte[] _bytes;

        /// <summary>
        /// Creates a <see cref="SetRequestMessage"/> with all contents.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="version">Protocol version</param>
        /// <param name="community">Community name</param>
        /// <param name="variables">Variables</param>
        public SetRequestMessage(int requestId, VersionCode version, OctetString community, IList<Variable> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }
            
            if (community == null)
            {
                throw new ArgumentNullException("community");
            }
            
            if (version == VersionCode.V3)
            {
                throw new ArgumentException("only v1 and v2c are supported", "version");
            }
            
            Version = version;
            Header = Header.Empty;
            Parameters = SecurityParameters.Create(community);
            var pdu = new SetRequestPdu(
                requestId,
                variables);
            Scope = new Scope(pdu);
            Privacy = DefaultPrivacyProvider.DefaultPair;
 
            _bytes = this.PackMessage(null).ToBytes();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetRequestMessage"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="messageId">The message id.</param>
        /// <param name="requestId">The request id.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="variables">The variables.</param>
        /// <param name="privacy">The privacy provider.</param>
        /// <param name="report">The report.</param>
        [Obsolete("Please use other overloading ones.")]
        public SetRequestMessage(VersionCode version, int messageId, int requestId, OctetString userName, IList<Variable> variables, IPrivacyProvider privacy, ISnmpMessage report)
            : this(version, messageId, requestId, userName, variables, privacy, 0xFFE3, report)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SetRequestMessage"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="messageId">The message id.</param>
        /// <param name="requestId">The request id.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="variables">The variables.</param>
        /// <param name="privacy">The privacy provider.</param>
        /// <param name="maxMessageSize">Size of the max message.</param>
        /// <param name="report">The report.</param>
        public SetRequestMessage(VersionCode version, int messageId, int requestId, OctetString userName, IList<Variable> variables, IPrivacyProvider privacy, int maxMessageSize, ISnmpMessage report)
        {
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }
            
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }
            
            if (version != VersionCode.V3)
            {
                throw new ArgumentException("only v3 is supported", "version");
            }

            if (report == null)
            {
                throw new ArgumentNullException("report");
            }
            
            if (privacy == null)
            {
                throw new ArgumentNullException("privacy");
            }

            Version = version;
            Privacy = privacy;

            Header = new Header(new Integer32(messageId), new Integer32(maxMessageSize), privacy.ToSecurityLevel() | Levels.Reportable);
            var parameters = report.Parameters;
            var authenticationProvider = Privacy.AuthenticationProvider;
            Parameters = new SecurityParameters(
                parameters.EngineId,
                parameters.EngineBoots,
                parameters.EngineTime,
                userName,
                authenticationProvider.CleanDigest,
                Privacy.Salt);
            var pdu = new SetRequestPdu(
                requestId,
                variables);
            var scope = report.Scope;
            var contextEngineId = scope.ContextEngineId == OctetString.Empty ? parameters.EngineId : scope.ContextEngineId;
            Scope = new Scope(contextEngineId, scope.ContextName, pdu);

            authenticationProvider.ComputeHash(Version, Header, Parameters, Scope, Privacy);
            _bytes = this.PackMessage(null).ToBytes();
        }

        internal SetRequestMessage(VersionCode version, Header header, SecurityParameters parameters, Scope scope, IPrivacyProvider privacy, byte[] length)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }
            
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }
            
            if (header == null)
            {
                throw new ArgumentNullException("header");
            }
            
            if (privacy == null)
            {
                throw new ArgumentNullException("privacy");
            }

            Version = version;
            Header = header;
            Parameters = parameters;
            Scope = scope;
            Privacy = privacy;

            _bytes = this.PackMessage(length).ToBytes();
        }

        /// <summary>
        /// Gets the header.
        /// </summary>
        public Header Header { get; private set; }
        
        /// <summary>
        /// Gets the privacy provider.
        /// </summary>
        /// <value>The privacy provider.</value>
        public IPrivacyProvider Privacy { get; private set; }
        
        /// <summary>
        /// Returns a <see cref="string"/> that represents this <see cref="SetRequestMessage"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "SET request message: version: {0}; {1}; {2}", Version, Parameters.UserName, Scope.Pdu);
        }

        /// <summary>
        /// Converts to byte format.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return _bytes;
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public SecurityParameters Parameters { get; private set; }

        /// <summary>
        /// Gets the scope.
        /// </summary>
        /// <value>The scope.</value>
        public Scope Scope { get; private set; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public VersionCode Version { get; private set; }
    }
}

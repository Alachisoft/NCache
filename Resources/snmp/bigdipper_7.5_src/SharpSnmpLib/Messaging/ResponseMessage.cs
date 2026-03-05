// Response message type.
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
 * Date: 2008/5/1
 * Time: 18:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Response message.
    /// </summary>
    public sealed class ResponseMessage : ISnmpMessage
    {
        private readonly byte[] _bytes;

        /// <summary>
        /// Creates a <see cref="ResponseMessage"/> with all contents.
        /// </summary>
        /// <param name="requestId">Request ID.</param>
        /// <param name="version">Protocol version.</param>
        /// <param name="community">Community name.</param>
        /// <param name="error">Error code.</param>
        /// <param name="index">Error index.</param>
        /// <param name="variables">Variables.</param>
        public ResponseMessage(int requestId, VersionCode version, OctetString community, ErrorCode error, int index, IList<Variable> variables)
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
                throw new ArgumentException("Please use overload constructor for v3", "version");
            }

            Version = version;
            Header = Header.Empty;
            Parameters = SecurityParameters.Create(community);
            var pdu = new ResponsePdu(
                requestId,
                error,
                index,
                variables);
            Scope = new Scope(pdu);
            Privacy = DefaultPrivacyProvider.DefaultPair;

            _bytes = this.PackMessage(null).ToBytes();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMessage"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="header">The header.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="scope">The scope.</param>
        /// <param name="privacy">The privacy provider.</param>
        /// <param name="needAuthentication">if set to <c>true</c>, authentication is needed.</param>
        /// <param name="length">The length bytes.</param>
        public ResponseMessage(VersionCode version, Header header, SecurityParameters parameters, Scope scope, IPrivacyProvider privacy, bool needAuthentication, byte[] length)
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

            if (needAuthentication)
            {
                Privacy.AuthenticationProvider.ComputeHash(Version, Header, Parameters, Scope, Privacy);
            }

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
        /// Error status.
        /// </summary>
        public ErrorCode ErrorStatus
        {
            get { return Scope.Pdu.ErrorStatus.ToErrorCode(); }
        }

        /// <summary>
        /// Error index.
        /// </summary>
        public int ErrorIndex
        {
            get { return Scope.Pdu.ErrorIndex.ToInt32(); }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public VersionCode Version { get; private set; }

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
        /// Converts to byte format.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return _bytes;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this <see cref="ResponseMessage"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Response message: version: {0}; {1}; {2}", Version, Parameters.UserName, Scope.Pdu);
        }
    }
}

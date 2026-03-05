// Malformed message type.
// Copyright (C) 2010 Lex Li.
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
using System.Globalization;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Malformed message for v3 due to decryption failures or wrong user names. 
    /// </summary>
    public sealed class MalformedMessage : ISnmpMessage
    {
        private static readonly Scope DefaultScope = new Scope(new MalformedPdu());
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MalformedMessage"/> class.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="user">The user.</param>
        public MalformedMessage(int messageId, OctetString user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            Header = new Header(messageId);
            Parameters = SecurityParameters.Create(user);
            Scope = DefaultScope;
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
        public Scope Scope
        {
            get; private set;
        }

        /// <summary>
        /// Gets the header.
        /// </summary>
        public Header Header { get; private set; }

        /// <summary>
        /// Converts to the bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return new byte[0];
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public VersionCode Version
        {
            get { return VersionCode.V3; }
        }

        /// <summary>
        /// Gets the privacy provider.
        /// </summary>
        /// <value>The privacy provider.</value>
        public IPrivacyProvider Privacy
        {
            get { return null; }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Malformed message: message id: {0}; user: {1}", this.MessageId(), Parameters.UserName);
        }
    }
}
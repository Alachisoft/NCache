// Default authentication provider (empty class).
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

namespace Lextm.SharpSnmpLib.Security
{
    /// <summary>
    /// Default authentication provider.
    /// </summary>
    public sealed class DefaultAuthenticationProvider : IAuthenticationProvider
    {
        private DefaultAuthenticationProvider()
        {
        }

        private static IAuthenticationProvider _instance;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static IAuthenticationProvider Instance
        {
            get { return _instance ?? (_instance = new DefaultAuthenticationProvider()); }
        }

        #region IAuthenticationProvider Members

        /// <summary>
        /// Computes the hash.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="header">The header.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="data">The scope data.</param>
        /// <param name="privacy">The privacy provider.</param>
        /// <param name="length">The length bytes.</param>
        /// <returns></returns>
        public OctetString ComputeHash(VersionCode version, ISegment header, SecurityParameters parameters, ISnmpData data, IPrivacyProvider privacy, byte[] length)
        {
            if (header == null)
            {
                throw new ArgumentNullException("header");
            }

            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (privacy == null)
            {
                throw new ArgumentNullException("privacy");
            }

            return OctetString.Empty;
        }

        /// <summary>
        /// Computes the hash.
        /// </summary>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public OctetString ComputeHash(byte[] buffer, OctetString engineId)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (engineId == null)
            {
                throw new ArgumentNullException("engineId");
            }

            return OctetString.Empty;
        }

        /// <summary>
        /// Gets the clean digest.
        /// </summary>
        /// <value>The clean digest.</value>
        public OctetString CleanDigest
        {
            get { return OctetString.Empty; }
        }

        /// <summary>
        /// Converts password to key.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="engineId"></param>
        /// <returns></returns>
        public byte[] PasswordToKey(byte[] password, byte[] engineId)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            
            if (engineId == null)
            {
                throw new ArgumentNullException("engineId");
            }
            
            // IMPORTANT: this function is not used.
            return new byte[0];
        }

        #endregion

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Default authentication provider";
        }
    }
}

// Default privacy provider (empty class).
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
    /// Default privacy provider.
    /// </summary>
    public sealed class DefaultPrivacyProvider : IPrivacyProvider
    {
        private static IPrivacyProvider _defaultInstance;
        
        /// <summary>
        /// Default privacy provider with default authentication provider.
        /// </summary>
        public static IPrivacyProvider DefaultPair
        {
            get
            {
                return _defaultInstance ?? (_defaultInstance = new DefaultPrivacyProvider(DefaultAuthenticationProvider.Instance));
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPrivacyProvider"/> class.
        /// </summary>
        /// <param name="authentication">Authentication provider.</param>
        public DefaultPrivacyProvider(IAuthenticationProvider authentication)
        {
            AuthenticationProvider = authentication;
        }

        #region IPrivacyProvider Members

        /// <summary>
        /// Corresponding <see cref="IAuthenticationProvider"/>.
        /// </summary>
        public IAuthenticationProvider AuthenticationProvider { get; private set; }
        
        /// <summary>
        /// Decrypts the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public ISnmpData Decrypt(ISnmpData data, SecurityParameters parameters)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }            
            
            if (data.TypeCode != SnmpType.Sequence)
            {
                var newException = new DecryptionException("Default decryption failed");
                throw newException;
            }
            
            return data;
        }

        /// <summary>
        /// Encrypts the specified scope.
        /// </summary>
        /// <param name="data">The scope data.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public ISnmpData Encrypt(ISnmpData data, SecurityParameters parameters)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
                        
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }            
          
            if (data.TypeCode == SnmpType.Sequence || data is ISnmpPdu)
            {
                return data;
            }
            
            throw new ArgumentException("unencrypted data is expected.", "data");
        }

        /// <summary>
        /// Gets the salt.
        /// </summary>
        /// <value>The salt.</value>
        public OctetString Salt
        {
            get { return OctetString.Empty; }
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
            return "Default privacy provider";
        }
    }
}

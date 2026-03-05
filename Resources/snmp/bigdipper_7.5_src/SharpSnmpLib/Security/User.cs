// User class.
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

namespace Lextm.SharpSnmpLib.Security
{
    /// <summary>
    /// User class.
    /// </summary>
    public sealed class User
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="privacy">The privacy provider.</param>
        public User(OctetString name, IPrivacyProvider privacy)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (privacy == null)
            {
                throw new ArgumentNullException("privacy");
            }

            Name = name;
            Privacy = privacy;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public OctetString Name { get; private set; }

        /// <summary>
        /// Gets the privacy provider.
        /// </summary>
        /// <value>The provider.</value>
        public IPrivacyProvider Privacy { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "User: name: {0}; provider: {1}", Name, Privacy);
        }
    }
}
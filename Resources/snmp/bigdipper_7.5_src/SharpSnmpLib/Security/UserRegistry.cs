// User registry.
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

namespace Lextm.SharpSnmpLib.Security
{
    /// <summary>
    /// A repository to store user information for providers.
    /// </summary>
    public sealed class UserRegistry
    {
        private readonly IDictionary<OctetString, User> _users = new Dictionary<OctetString, User>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRegistry"/> class.
        /// </summary>
        /// <param name="users">The users.</param>
// ReSharper disable ParameterTypeCanBeEnumerable.Local
        public UserRegistry(User[] users)
// ReSharper restore ParameterTypeCanBeEnumerable.Local
        {
            if (users == null)
            {
                return;
            }

            foreach (var user in users)
            {
                Add(user);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRegistry"/> class.
        /// </summary>
        public UserRegistry() : this(null)
        {
        }
        
        /// <summary>
        /// Returns the user count.
        /// </summary>
        public int Count
        {
            get { return _users.Count; }
        }

        /// <summary>
        /// Adds the specified user name.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="privacy">The privacy provider.</param>
        public UserRegistry Add(OctetString userName, IPrivacyProvider privacy)
        {
            return Add(new User(userName, privacy));
        }

        /// <summary>
        /// Adds the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        public UserRegistry Add(User user)
        {
            if (user == null)
            {
                return this;
            }
            
            if (_users.ContainsKey(user.Name))
            {
                _users.Remove(user.Name);
            }

            _users.Add(user.Name, user);
            return this;
        }

        /// <summary>
        /// Finds the specified user name.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <returns></returns>
        public IPrivacyProvider Find(OctetString userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }

            if (userName == OctetString.Empty)
            {
                return DefaultPrivacyProvider.DefaultPair;
            }

            return _users.ContainsKey(userName) ? _users[userName].Privacy : null;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "User registry: count: {0}", Count);
        }
    }
}

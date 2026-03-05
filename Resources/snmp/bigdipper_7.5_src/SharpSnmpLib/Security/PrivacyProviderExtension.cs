// IPrivacyProvider extension methods.
// Copyright (C) 2010 Lex Li
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
 * Date: 10/10/2010
 * Time: 6:59 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Lextm.SharpSnmpLib.Security
{
    /// <summary>
    /// Extension class for <see cref="IPrivacyProvider"/>.
    /// </summary>
    public static class PrivacyProviderExtension
    {
        /// <summary>
        /// Converts to <see cref="Levels"/>.
        /// </summary>
        /// <returns></returns>
        public static Levels ToSecurityLevel(this IPrivacyProvider privacy)
        {
            if (privacy == null)
            {
                throw new ArgumentNullException("privacy");
            }
                
            Levels flags;
            if (privacy.AuthenticationProvider == DefaultAuthenticationProvider.Instance)
            {
                flags = 0;
            }
            else if (privacy is DefaultPrivacyProvider)
            {
                flags = Levels.Authentication;
            }
            else
            {
                flags = Levels.Authentication | Levels.Privacy;
            }

            return flags;
        } 

        internal static ISnmpData GetScopeData(this IPrivacyProvider privacy, Header header, SecurityParameters parameters, ISnmpData rawScopeData)
        {
            if (privacy == null)
            {
                throw new ArgumentNullException("privacy");
            }

            if (header == null)
            {
                throw new ArgumentNullException("header");
            }

            return Levels.Privacy == (header.SecurityLevel & Levels.Privacy)
                       ? privacy.Encrypt(rawScopeData, parameters)
                       : rawScopeData;
        }
    }
}

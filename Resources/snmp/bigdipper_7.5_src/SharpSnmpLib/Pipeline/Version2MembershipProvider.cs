// Version 2 membership provider class.
// Copyright (C) 2009-2010 Lex Li
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// SNMP version 2 membership provider, who checks community names for security.
    /// </summary>    
    public sealed class Version2MembershipProvider : IMembershipProvider
    {
        private const VersionCode Version = VersionCode.V2;
        private readonly OctetString _get;
        private readonly OctetString _set;

        /// <summary>
        /// Initializes a new instance of the <see cref="Version2MembershipProvider"/> class.
        /// </summary>
        /// <param name="getCommunity">The get community.</param>
        /// <param name="setCommunity">The set community.</param>
        public Version2MembershipProvider(OctetString getCommunity, OctetString setCommunity)
        {
            _get = getCommunity;
            _set = setCommunity;
        }

        /// <summary>
        /// Authenticates the request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public bool AuthenticateRequest(ISnmpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            
            var request = context.Request;
            if (request.Version != Version)
            {
                return false;
            }
            
            var parameters = request.Parameters;
            if (request.Pdu().TypeCode == SnmpType.SetRequestPdu)
            {
                return parameters.UserName == _set;
            }

            return parameters.UserName == _get;
        }
    }
}

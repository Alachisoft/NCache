// SNMP context factory class.
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

using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// SNMP context factory.
    /// </summary>
    internal static class SnmpContextFactory
    {
        /// <summary>
        /// Creates the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="users">The users.</param>
        /// <param name="group">The engine group.</param>
        /// <param name="binding">The binding.</param>
        /// <returns></returns>
        public static ISnmpContext Create(ISnmpMessage request, IPEndPoint sender, UserRegistry users, EngineGroup group, IListenerBinding binding)
        {
            if (request.Version == VersionCode.V3)
            {
                return new SecureSnmpContext(request, sender, users, group, binding);
            }

            return new NormalSnmpContext(request, sender, users, binding);
        }
    }
}
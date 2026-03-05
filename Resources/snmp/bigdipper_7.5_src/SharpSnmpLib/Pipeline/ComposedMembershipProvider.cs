// Composed membership provider class.
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

using System.Linq;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Composed membership provider, who owns internal providers. If the request is authenticated by any of the internal providers, it is considered as authenticated.
    /// </summary>    
    public sealed class ComposedMembershipProvider : IMembershipProvider
    {
        private readonly IMembershipProvider[] _providers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComposedMembershipProvider"/> class.
        /// </summary>
        /// <param name="providers">The internal providers.</param>
        public ComposedMembershipProvider(IMembershipProvider[] providers)
        {
            _providers = providers;
        }

        /// <summary>
        /// Authenticates the request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public bool AuthenticateRequest(ISnmpContext context)
        {
            return _providers.Any(provider => provider.AuthenticateRequest(context));
        }
    }
}

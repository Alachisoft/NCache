// Message handler factory class.
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

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 11/29/2009
 * Time: 11:01 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Linq;
using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Message handler factory.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    public sealed class MessageHandlerFactory
    {
        private readonly HandlerMapping[] _mappings;
        private readonly NullMessageHandler _nullHandler = new NullMessageHandler();

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageHandlerFactory"/> class.
        /// </summary>
        /// <param name="mappings">The mappings.</param>
        public MessageHandlerFactory(HandlerMapping[] mappings)
        {
            if (mappings == null)
            {
                throw new ArgumentNullException("mappings");
            }

            _mappings = mappings;
        }

        /// <summary>
        /// Gets the handler.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public IMessageHandler GetHandler(ISnmpMessage message)
        {
            foreach (var mapping in _mappings.Where(mapping => mapping.CanHandle(message)))
            {
                return mapping.Handler;
            }

            return _nullHandler;
        }
    }
}

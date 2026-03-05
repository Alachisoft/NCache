// Trap v2 message received event args.
// Copyright (C) 2010 Lex Li
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
using System.Net;
using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Trap v2 message received event args.
    /// </summary>
    public sealed class TrapV2MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrapV2MessageReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="request">The request.</param>
        /// <param name="binding">The binding.</param>
        public TrapV2MessageReceivedEventArgs(IPEndPoint sender, TrapV2Message request, IListenerBinding binding)
        {
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }

            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (binding == null)
            {
                throw new ArgumentNullException("binding");
            }

            Sender = sender;
            TrapV2Message = request;
            Binding = binding;
        }

        /// <summary>
        /// Gets or sets the sender.
        /// </summary>
        /// <value>The sender.</value>
        public IPEndPoint Sender { get; private set; }

        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>The request.</value>
        public TrapV2Message TrapV2Message { get; private set; }

        /// <summary>
        /// Gets or sets the binding.
        /// </summary>
        /// <value>The binding.</value>
        public IListenerBinding Binding { get; private set; }
    }
}
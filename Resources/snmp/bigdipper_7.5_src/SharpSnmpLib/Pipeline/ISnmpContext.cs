// SNMP context interface.
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
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// SNMP context interface.
    /// </summary>
    public interface ISnmpContext
    {
        /// <summary>
        /// Handles the authentication failure.
        /// </summary>
        void HandleAuthenticationFailure();

        /// <summary>
        /// Generates the response.
        /// </summary>
        /// <param name="variables">The variables.</param>
        void GenerateResponse(IList<Variable> variables);

        /// <summary>
        /// Copies the request variable bindings to response.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="index">The index.</param>
        void CopyRequest(ErrorCode status, int index);

        /// <summary>
        /// Gets or sets the binding.
        /// </summary>
        /// <value>The binding.</value>
        IListenerBinding Binding { get; }

        /// <summary>
        /// Gets the created time.
        /// </summary>
        /// <value>The created time.</value>
        DateTime CreatedTime { get; }

        /// <summary>
        /// Gets the request.
        /// </summary>
        /// <value>The request.</value>
        ISnmpMessage Request { get; }

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <value>The response.</value>
        ISnmpMessage Response { get; }

        /// <summary>
        /// Gets the sender.
        /// </summary>
        /// <value>The sender.</value>
        IPEndPoint Sender { get; }

        /// <summary>
        /// Gets a value indicating whether [too big].
        /// </summary>
        /// <value><c>true</c> if the response is too big; otherwise, <c>false</c>.</value>
        bool TooBig { get; }

        /// <summary>
        /// Sends out response message.
        /// </summary>
        void SendResponse();

        /// <summary>
        /// Handles the membership authentication.
        /// </summary>
        /// <returns></returns>
        bool HandleMembership();

        /// <summary>
        /// Generates too big message.
        /// </summary>
        void GenerateTooBig();
    }
}
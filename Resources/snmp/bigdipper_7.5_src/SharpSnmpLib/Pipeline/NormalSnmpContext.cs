// Normal SNMP context class.
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

using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Nomral SNMP context class. It is v1 and v2c specific.
    /// </summary>
    internal sealed class NormalSnmpContext : SnmpContextBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NormalSnmpContext"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="users">The users.</param>
        /// <param name="binding">The binding.</param>
        public NormalSnmpContext(ISnmpMessage request, IPEndPoint sender, UserRegistry users, IListenerBinding binding) 
            : base(request, sender, users, null, binding)
        {
        }

        /// <summary>
        /// Handles the authentication failure.
        /// </summary>
        public override void HandleAuthenticationFailure()
        {
            // TODO: implement this later according to v1 and v2c RFC.
            Response = null;
        }

        /// <summary>
        /// Copies the request variable bindings to response.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="index">The index.</param>
        public override void CopyRequest(ErrorCode status, int index)
        {
            Response = new ResponseMessage(
                Request.RequestId(),
                Request.Version,
                Request.Parameters.UserName,
                status,
                index,
                Request.Pdu().Variables);
            if (TooBig)
            {
                GenerateTooBig();
            }
        }

        /// <summary>
        /// Generates too big message.
        /// </summary>
        public override void GenerateTooBig()
        {
            Response = new ResponseMessage(
                Request.RequestId(),
                Request.Version,
                Request.Parameters.UserName,
                ErrorCode.TooBig,
                0,
                Request.Pdu().Variables);
        }

        /// <summary>
        /// Handles the membership.
        /// </summary>
        /// <returns>Always returns <code>false</code>.</returns>
        public override bool HandleMembership()
        {
            return false;
        }

        /// <summary>
        /// Generates the response.
        /// </summary>
        /// <param name="variables">The variables.</param>
        public override void GenerateResponse(IList<Variable> variables)
        {
            Response = new ResponseMessage(
                Request.RequestId(),
                Request.Version,
                Request.Parameters.UserName,
                ErrorCode.NoError,
                0,
                variables);
            if (TooBig)
            {
                GenerateTooBig();
            }
        }
    }
}
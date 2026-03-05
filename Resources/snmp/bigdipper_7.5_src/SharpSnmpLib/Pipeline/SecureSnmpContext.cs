// Secure SNMP context class.
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
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Pipeline
{
    /// <summary>
    /// Secure SNMP context. It is specific to v3.
    /// </summary>
    internal sealed class SecureSnmpContext : SnmpContextBase
    {
        private static readonly ObjectIdentifier ReportId = new ObjectIdentifier("1.3.6.1.6.3.15.1.1.4.0");
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SecureSnmpContext"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="users">The users.</param>
        /// <param name="group">The engine core group.</param>
        /// <param name="binding">The binding.</param>
        public SecureSnmpContext(ISnmpMessage request, IPEndPoint sender, UserRegistry users, EngineGroup group, IListenerBinding binding)
            : base(request, sender, users, group, binding)
        {
        }

        /// <summary>
        /// Handles the authentication failure.
        /// </summary>
        public override void HandleAuthenticationFailure()
        {
            var defaultPair = DefaultPrivacyProvider.DefaultPair;
            Response = new ResponseMessage(
                Request.Version,
                new Header(
                    new Integer32(Request.MessageId()),
                    new Integer32(Messenger.MaxMessageSize),
                    0), // no need to encrypt.
                new SecurityParameters(
                    Group.EngineId,
                    new Integer32(Group.EngineBoots),
                    new Integer32(Group.EngineTime),
                    Request.Parameters.UserName,
                    defaultPair.AuthenticationProvider.CleanDigest,
                    defaultPair.Salt),
                new Scope(
                    Group.EngineId,
                    OctetString.Empty,
                    new ResponsePdu(
                        Request.RequestId(),
                        ErrorCode.AuthorizationError,
                        0,
                        Request.Pdu().Variables)),
                defaultPair,
                true,
                null);
            if (TooBig)
            {
                GenerateTooBig();
            }
        }

        public override void CopyRequest(ErrorCode status, int index)
        {
            var userName = Request.Parameters.UserName;
            var privacy = Users.Find(userName);
            Response = new ResponseMessage(
                    Request.Version,
                    new Header(
                        new Integer32(Request.MessageId()),
                        new Integer32(Messenger.MaxMessageSize),
                        privacy.ToSecurityLevel()),
                    new SecurityParameters(
                        Group.EngineId,
                        new Integer32(Group.EngineBoots),
                        new Integer32(Group.EngineTime),
                        userName,
                        privacy.AuthenticationProvider.CleanDigest,
                        privacy.Salt),
                    new Scope(
                        Group.EngineId,
                        OctetString.Empty,
                        new ResponsePdu(
                            Request.RequestId(),
                            status,
                            index,
                            Request.Pdu().Variables)),
                    privacy,
                    true,
                    null);
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
            var userName = Request.Parameters.UserName;
            var privacy = Users.Find(userName);
            Response = new ResponseMessage(
                Request.Version,
                new Header(
                    new Integer32(Request.MessageId()),
                    new Integer32(Messenger.MaxMessageSize),
                    privacy.ToSecurityLevel()),
                new SecurityParameters(
                    Group.EngineId,
                    new Integer32(Group.EngineBoots),
                    new Integer32(Group.EngineTime),
                    userName,
                    privacy.AuthenticationProvider.CleanDigest,
                    privacy.Salt),
                new Scope(
                    Group.EngineId,
                    OctetString.Empty,
                    new ResponsePdu(
                        Request.RequestId(),
                        ErrorCode.TooBig,
                        0,
                        Request.Pdu().Variables)),
                privacy,
                true,
                null);
            if (TooBig)
            {
                Response = null;
                
                // TODO: snmpSilentDrops++;
            }
        }

        /// <summary>
        /// Handles the membership.
        /// </summary>
        /// <returns></returns>
        public override bool HandleMembership()
        {
            var request = Request;
            if (request is MalformedMessage)
            {
                return false;
            }

            var parameters = request.Parameters;
            if (parameters.UserName == OctetString.Empty)
            {
                HandleDiscovery();
                return true;
            }

            if (parameters.EngineId != Group.EngineId)
            {
                // not from this engine.
                return false;
            }

            if (parameters.EngineBoots.ToInt32() != Group.EngineBoots)
            {
                // does not match boot count.
                return false;
            }

            return EngineGroup.IsInTime(parameters.EngineTime.ToInt32(), Group.EngineTime);
        }

        private void HandleDiscovery()
        {
            Group.ReportCount++;
            
            // discovery message received.
            Response = new ReportMessage(
                VersionCode.V3,
                new Header(
                    new Integer32(Request.MessageId()),
                    new Integer32(Messenger.MaxMessageSize),
                    0), // no need to encrypt for discovery.
                new SecurityParameters(
                    Group.EngineId,
                    Integer32.Zero,
                    new Integer32(Environment.TickCount),
                    OctetString.Empty,
                    OctetString.Empty,
                    OctetString.Empty),
                new Scope(
                    Group.EngineId,
                    OctetString.Empty,
                    new ReportPdu(
                        Request.RequestId(),
                        ErrorCode.NoError,
                        0,
                        new List<Variable>(1)
                            {
                                new Variable(
                                    ReportId,
                                    new Counter32(Group.ReportCount))
                            })),
                DefaultPrivacyProvider.DefaultPair,
                null);
            if (TooBig)
            {
                GenerateTooBig();
            }
        }

        /// <summary>
        /// Generates the response.
        /// </summary>
        /// <param name="variables">The variables.</param>
        public override void GenerateResponse(IList<Variable> variables)
        {
            var userName = Request.Parameters.UserName;
            var privacy = Users.Find(userName);
            Response = new ResponseMessage(
                Request.Version,
                new Header(
                    new Integer32(Request.MessageId()),
                    new Integer32(Messenger.MaxMessageSize),
                    privacy.ToSecurityLevel()),
                new SecurityParameters(
                    Group.EngineId,
                    new Integer32(Group.EngineBoots),
                    new Integer32(Group.EngineTime),
                    userName,
                    privacy.AuthenticationProvider.CleanDigest,
                    privacy.Salt),
                new Scope(
                    Group.EngineId,
                    OctetString.Empty,
                    new ResponsePdu(
                        Request.RequestId(),
                        ErrorCode.NoError,
                        0,
                        variables)),
                privacy,
                true,
                null);
            if (TooBig)
            {
                GenerateTooBig();
            }
        }
    }
}
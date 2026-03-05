// Message factory type.
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

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/1
 * Time: 11:17
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Factory that creates <see cref="ISnmpMessage"/> instances from byte format.
    /// </summary>
    public static class MessageFactory
    {
        /// <summary>
        /// Creates <see cref="ISnmpMessage"/> instances from a string.
        /// </summary>
        /// <param name="bytes">Byte string.</param>
        /// <param name="registry">The registry.</param>
        /// <returns></returns>
        public static IList<ISnmpMessage> ParseMessages(IEnumerable<char> bytes, UserRegistry registry)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            
            if (registry == null)
            {
                throw new ArgumentNullException("registry");
            }
            
            return ParseMessages(ByteTool.Convert(bytes), registry);
        }

        /// <summary>
        /// Creates <see cref="ISnmpMessage"/> instances from buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="registry">The registry.</param>
        /// <returns></returns>
        public static IList<ISnmpMessage> ParseMessages(byte[] buffer, UserRegistry registry)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (registry == null)
            {
                throw new ArgumentNullException("registry");
            }

            return ParseMessages(buffer, 0, buffer.Length, registry);
        }

        /// <summary>
        /// Creates <see cref="ISnmpMessage"/> instances from buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <param name="index">The index.</param>
        /// <param name="length">The length.</param>
        /// <param name="registry">The registry.</param>
        /// <returns></returns>
        public static IList<ISnmpMessage> ParseMessages(byte[] buffer, int index, int length, UserRegistry registry)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            
            if (registry == null)
            {
                throw new ArgumentNullException("registry");
            }

            IList<ISnmpMessage> result = new List<ISnmpMessage>();
            using (Stream stream = new MemoryStream(buffer, index, length, true))
            {
                int first;
                while ((first = stream.ReadByte()) != -1)
                {
                    var message = ParseMessage(first, stream, registry);
                    if (message == null)
                    {
                        continue;
                    }
                    
                    result.Add(message);
                }
            }

            return result;
        }

        private static ISnmpMessage ParseMessage(int first, Stream stream, UserRegistry registry)
        {
            var array = DataFactory.CreateSnmpData(first, stream);

            if (array == null)
            {
                return null;
            }

            if (array.TypeCode != SnmpType.Sequence)
            {
                throw new SnmpException("not an SNMP message");
            }

            var body = (Sequence)array;
            if (body.Length != 3 && body.Length != 4)
            {
                throw new SnmpException("not an SNMP message");
            }

            var version = (VersionCode)((Integer32)body[0]).ToInt32();
            Header header;
            SecurityParameters parameters;
            IPrivacyProvider privacy;
            Scope scope;
            if (body.Length == 3)
            {
                header = Header.Empty;
                parameters = SecurityParameters.Create((OctetString)body[1]);
                privacy = DefaultPrivacyProvider.DefaultPair;
                scope = new Scope((ISnmpPdu)body[2]);
            }
            else
            {
                header = new Header(body[1]);
                parameters = new SecurityParameters((OctetString)body[2]);
                privacy = registry.Find(parameters.UserName);
                if (privacy == null)
                {
                    // handle decryption exception.
                    return new MalformedMessage(header.MessageId, parameters.UserName);
                }

                var code = body[3].TypeCode;
                if (code == SnmpType.Sequence)
                {
                    // v3 not encrypted
                    scope = new Scope((Sequence)body[3]);
                }
                else if (code == SnmpType.OctetString)
                {
                    // v3 encrypted
                    try
                    {
                        scope = new Scope((Sequence)privacy.Decrypt(body[3], parameters));
                    }
                    catch (DecryptionException)
                    {
                        // handle decryption exception.
                        return new MalformedMessage(header.MessageId, parameters.UserName);
                    }
                }
                else
                {
                    throw new SnmpException(string.Format(CultureInfo.InvariantCulture, "invalid v3 packets scoped data: {0}", code));
                }
              
                if (!privacy.AuthenticationProvider.VerifyHash(version, header, parameters, body[3], privacy, body.GetLengthBytes()))
                {
                    throw new SnmpException("invalid v3 packet data hash detected");
                }
            }

            var scopeCode = scope.Pdu.TypeCode;
            switch (scopeCode)
            {
                case SnmpType.TrapV1Pdu:
                    return new TrapV1Message(body);
                case SnmpType.TrapV2Pdu:
                    return new TrapV2Message(version, header, parameters, scope, privacy, body.GetLengthBytes());
                case SnmpType.GetRequestPdu:
                    return new GetRequestMessage(version, header, parameters, scope, privacy, body.GetLengthBytes());
                case SnmpType.ResponsePdu:
                    return new ResponseMessage(version, header, parameters, scope, privacy, false, body.GetLengthBytes());
                case SnmpType.SetRequestPdu:
                    return new SetRequestMessage(version, header, parameters, scope, privacy, body.GetLengthBytes());
                case SnmpType.GetNextRequestPdu:
                    return new GetNextRequestMessage(version, header, parameters, scope, privacy, body.GetLengthBytes());
                case SnmpType.GetBulkRequestPdu:
                    return new GetBulkRequestMessage(version, header, parameters, scope, privacy, body.GetLengthBytes());
                case SnmpType.ReportPdu:
                    return new ReportMessage(version, header, parameters, scope, privacy, body.GetLengthBytes());
                case SnmpType.InformRequestPdu:
                    return new InformRequestMessage(version, header, parameters, scope, privacy, body.GetLengthBytes());
                default:
                    throw new SnmpException(string.Format(CultureInfo.InvariantCulture, "unsupported pdu: {0}", scopeCode));
            }
        }
    }
}

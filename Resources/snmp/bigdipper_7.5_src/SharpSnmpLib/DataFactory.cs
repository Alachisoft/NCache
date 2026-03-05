// Data factory type.
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
 * Date: 2008/4/30
 * Time: 19:49
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Globalization;
using System.IO;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Factory that creates <see cref="ISnmpData"/> instances.
    /// </summary>
    public static class DataFactory
    {
        /// <summary>
        /// Creates an <see cref="ISnmpData"/> instance from buffer.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <returns></returns>
        public static ISnmpData CreateSnmpData(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            return CreateSnmpData(buffer, 0, buffer.Length);
        }
        
        /// <summary>
        /// Creates an <see cref="ISnmpData"/> instance from stream.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="type">Type code.</param>
        /// <returns></returns>
        public static ISnmpData CreateSnmpData(int type, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            
            var length = stream.ReadPayloadLength();
            switch ((SnmpType)type)
            {
                case SnmpType.Counter32:
                    return new Counter32(length, stream);
                case SnmpType.Counter64:
                    return new Counter64(length, stream);
                case SnmpType.Gauge32:
                    return new Gauge32(length, stream); 
                case SnmpType.ObjectIdentifier:
                    return new ObjectIdentifier(length, stream);
                case SnmpType.Null:
                    return new Null(length, stream);
                case SnmpType.NoSuchInstance:
                    return new NoSuchInstance(length, stream);
                case SnmpType.NoSuchObject:
                    return new NoSuchObject(length, stream);
                case SnmpType.EndOfMibView:
                    return new EndOfMibView(length, stream);
                case SnmpType.Integer32:
                    return new Integer32(length, stream);
                case SnmpType.OctetString:
                    return new OctetString(length, stream);
                case SnmpType.IPAddress:
                    return new IP(length, stream);
                case SnmpType.TimeTicks:
                    return new TimeTicks(length, stream);
                case SnmpType.Sequence:
                    return new Sequence(length, stream);
                case SnmpType.TrapV1Pdu:
                    return new TrapV1Pdu(length, stream);
                case SnmpType.TrapV2Pdu:
                    return new TrapV2Pdu(length, stream);
                case SnmpType.GetRequestPdu:
                    return new GetRequestPdu(length, stream);
                case SnmpType.ResponsePdu:
                    return new ResponsePdu(length, stream);
                case SnmpType.GetBulkRequestPdu:
                    return new GetBulkRequestPdu(length, stream);
                case SnmpType.GetNextRequestPdu:
                    return new GetNextRequestPdu(length, stream);
                case SnmpType.SetRequestPdu:
                    return new SetRequestPdu(length, stream);
                case SnmpType.InformRequestPdu:
                    return new InformRequestPdu(length, stream);
                case SnmpType.ReportPdu:
                    return new ReportPdu(length, stream);
                case SnmpType.Opaque:
                    return new Opaque(length, stream);
                case SnmpType.EndMarker:
                    return null;
                default:
                    throw new SnmpException(string.Format(CultureInfo.InvariantCulture, "unsupported data type: {0}", (SnmpType)type));
            }
        }

        /// <summary>
        /// Creates an <see cref="ISnmpData"/> instance from buffer.
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="index">Index</param>
        /// <param name="count">Count</param>
        /// <returns></returns>
        public static ISnmpData CreateSnmpData(byte[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            
            using (var m = new MemoryStream(buffer, index, count, false))
            {
                return CreateSnmpData(m);
            }
        }
        
        /// <summary>
        /// Creates an <see cref="ISnmpData"/> instance from stream.
        /// </summary>
        /// <param name="stream">Stream</param>
        /// <returns></returns>
        public static ISnmpData CreateSnmpData(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            return CreateSnmpData(stream.ReadByte(), stream);
        }
    }
}

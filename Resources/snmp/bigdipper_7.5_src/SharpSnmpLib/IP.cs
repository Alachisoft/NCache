// SNMP IP address type.
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

using System;
using System.IO;
using System.Net;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// IPAddress type.
    /// </summary>
    public sealed class IP : ISnmpData, IEquatable<IP>
    {
        private readonly IPAddress _ip;
        private readonly byte[] _length;

// ReSharper disable InconsistentNaming
        private const int IPv4Length = 4;
        private const int IPv6Length = 16;
// ReSharper restore InconsistentNaming      
  
        /// <summary>
        /// Creates an <see cref="IP"/> with a specific <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="ip">IP address</param>
        public IP(IPAddress ip)
        {
            if (ip == null)
            {
                throw new ArgumentNullException("ip");
            }
            
            _ip = ip;
        }

        /// <summary>
        /// Creates an <see cref="IP"/> from a specific <see cref="String"/>.
        /// </summary>
        /// <param name="ip">IP string</param>
        public IP(string ip) : this(ParseString(ip))
        {
        }

        private static IPAddress ParseString(string ip)
        {
#if CF
            return IPAddress.Parse(ip);
#else
            IPAddress temp;
            if (IPAddress.TryParse(ip, out temp))
            {
                return temp;
            }

            throw new ArgumentException("This is not a valid IP address", "ip");
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IP"/> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="stream">The stream.</param>
        public IP(Tuple<int, byte[]> length, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (length.Item1 != IPv4Length && length.Item1 != IPv6Length)
            {
                throw new ArgumentException("bytes must contain 4 or 16 elements");
            }

            var raw = new byte[length.Item1];
            stream.Read(raw, 0, length.Item1);
            _ip = new IPAddress(raw);
            _length = length.Item2;
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="IP"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _ip.ToString();
        }
        
        /// <summary>
        /// Returns an <see cref="IPAddress"/> that represents this <see cref="IP"/>.
        /// </summary>
        /// <returns></returns>
        public IPAddress ToIPAddress()
        {
            return _ip;
        }
        
        /// <summary>
        /// Type code.
        /// </summary>
        public SnmpType TypeCode
        {
            get
            {
                return SnmpType.IPAddress;
            }
        }

        /// <summary>
        /// Appends the bytes to <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void AppendBytesTo(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            
            stream.AppendBytes(TypeCode, _length, _ip.GetAddressBytes());
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="IP"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="IP"/>. </param>
        /// <returns><value>true</value> if the specified <see cref="Object"/> is equal to the current <see cref="IP"/>; otherwise, <value>false</value>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as IP);
        }
        
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><value>true</value> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <value>false</value>.
        /// </returns>
        public bool Equals(IP other)
        {
            return Equals(this, other);
        }
        
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="IP"/>.</returns>
        public override int GetHashCode()
        {
            return _ip.GetHashCode();
        }
        
        /// <summary>
        /// The equality operator.
        /// </summary>
        /// <param name="left">Left <see cref="IP"/> object</param>
        /// <param name="right">Right <see cref="IP"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are equal, <c>false</c> otherwise.</returns>
        public static bool operator ==(IP left, IP right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// The inequality operator.
        /// </summary>
        /// <param name="left">Left <see cref="IP"/> object</param>
        /// <param name="right">Right <see cref="IP"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        public static bool operator !=(IP left, IP right)
        {
            return !(left == right);
        }
        
        /// <summary>
        /// The comparison.
        /// </summary>
        /// <param name="left">Left <see cref="IP"/> object</param>
        /// <param name="right">Right <see cref="IP"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        private static bool Equals(IP left, IP right)
        {
            object lo = left;
            object ro = right;
            if (lo == ro)
            {
                return true;
            }

            if (lo == null || ro == null)
            {
                return false;
            }

            return left.ToIPAddress().Equals(right.ToIPAddress());           
        }
    }
}

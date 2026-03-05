// SNMP COUNTER64 type (v2 and above only).
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Counter64 type.
    /// </summary>
    public sealed class Counter64 : ISnmpData, IEquatable<Counter64>
    {
        private readonly ulong _count;
        private readonly byte[] _length;

        private byte[] _raw;

        /// <summary>
        /// Creates a <see cref="Counter64"/> instance from raw bytes.
        /// </summary>
        /// <param name="raw"></param>
        internal Counter64(byte[] raw) : this(new Tuple<int, byte[]>(raw.Length, raw.Length.WritePayloadLength()), new MemoryStream(raw))
        {
            // IMPORTANT: for test project only.
        }
        
        /// <summary>
        /// Creates a <see cref="Counter64"/> with a specific <see cref="UInt64"/>.
        /// </summary>
        /// <param name="value">Value</param>
        [CLSCompliant(false)]
        public Counter64(ulong value)
        {
            _count = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Counter64"/> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="stream">The stream.</param>
        public Counter64(Tuple<int, byte[]> length, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (length.Item1 <= 0 || length.Item1 > 9)
            {
                throw new ArgumentException("byte length must between 1 and 9");
            }

            _raw = new byte[length.Item1];
            stream.Read(_raw, 0, length.Item1);
            if (length.Item1 == 9 && _raw[0] != 0)
            {
                throw new ArgumentException("if byte length is 5, then first byte must be empty");
            }

            var list = new List<byte>(_raw);
            list.Reverse();
            while (list.Count > 8)
            {
                list.RemoveAt(list.Count - 1);
            }

            while (list.Count < 8)
            {
                list.Add(0);
            }

            _count = BitConverter.ToUInt64(list.ToArray(), 0);
            _length = length.Item2;
        }

        #region ISnmpData Members
        /// <summary>
        /// Type code.
        /// </summary>
        public SnmpType TypeCode
        {
            get { return SnmpType.Counter64; }
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
            
            stream.AppendBytes(TypeCode, _length, GetRaw());
        }

        #endregion
        /// <summary>
        /// Returns a <see cref="UInt64"/> that represents a <see cref="Counter64"/>.
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public ulong ToUInt64()
        {
            return _count;
        }
        
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="Counter64"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToUInt64().ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets that raw bytes.
        /// </summary>
        /// <returns></returns>
        private byte[] GetRaw()
        {
            return _raw ?? (_raw = ByteTool.GetRawBytes(BitConverter.GetBytes(_count), false));
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><value>true</value> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <value>false</value>.
        /// </returns>
        public bool Equals(Counter64 other)
        {
            return Equals(this, other);
        }
        
        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Counter64"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Counter64"/>. </param>
        /// <returns><value>true</value> if the specified <see cref="Object"/> is equal to the current <see cref="Counter64"/>; otherwise, <value>false</value>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as Counter64);
        }
        
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="Counter64"/>.</returns>
        public override int GetHashCode()
        {
            return ToUInt64().GetHashCode();
        }
        
        /// <summary>
        /// The equality operator.
        /// </summary>
        /// <param name="left">Left <see cref="Counter64"/> object</param>
        /// <param name="right">Right <see cref="Counter64"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are equal, <c>false</c> otherwise.</returns>
        public static bool operator ==(Counter64 left, Counter64 right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// The inequality operator.
        /// </summary>
        /// <param name="left">Left <see cref="Counter64"/> object</param>
        /// <param name="right">Right <see cref="Counter64"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        public static bool operator !=(Counter64 left, Counter64 right)
        {
            return !(left == right);
        }
        
        /// <summary>
        /// The comparison.
        /// </summary>
        /// <param name="left">Left <see cref="Counter64"/> object</param>
        /// <param name="right">Right <see cref="Counter64"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        private static bool Equals(Counter64 left, Counter64 right)
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
            
            return left.ToUInt64() == right.ToUInt64();
        }
    }
}
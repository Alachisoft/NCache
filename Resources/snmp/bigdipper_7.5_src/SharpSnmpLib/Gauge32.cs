// SNMP GAUGE32 type.
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
using System.Globalization;
using System.IO;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Gauge32 type.
    /// </summary>
    public sealed class Gauge32 : ISnmpData, IEquatable<Gauge32>
    {
        private readonly Counter32 _count;
        
        /// <summary>
        /// Creates a <see cref="Gauge32"/> instance from raw bytes.
        /// </summary>
        /// <param name="raw"></param>
        internal Gauge32(byte[] raw) : this(new Tuple<int, byte[]>(raw.Length, raw.Length.WritePayloadLength()), new MemoryStream(raw))
        {
            // IMPORTANT: for test project only.
        }
        
        /// <summary>
        /// Creates a <see cref="Gauge32"/> with a specific <see cref="UInt32"/>.
        /// </summary>
        /// <param name="value">Value</param>
        [CLSCompliant(false)]
        public Gauge32(uint value)
        {
            _count = new Counter32(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Gauge32"/> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="stream">The stream.</param>
        public Gauge32(Tuple<int, byte[]> length, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            
            _count = new Counter32(length, stream);
        }

        #region ISnmpData Members
        /// <summary>
        /// Type code.
        /// </summary>
        public SnmpType TypeCode
        {
            get { return SnmpType.Gauge32; }
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
            
            stream.AppendBytes(TypeCode, _count.GetLengthBytes(), _count.GetRaw());
        }

        #endregion
        /// <summary>
        /// Returns a <see cref="UInt32"/> that represents a <see cref="Gauge32"/>.
        /// </summary>
        /// <returns></returns>
        [CLSCompliant(false)]
        public uint ToUInt32()
        {
            return _count.ToUInt32();
        }
        
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="Gauge32"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToUInt32().ToString(CultureInfo.InvariantCulture);
        }
        
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><value>true</value> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <value>false</value>.
        /// </returns>
        public bool Equals(Gauge32 other)
        {
            return Equals(this, other);
        }
        
        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Gauge32"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Gauge32"/>. </param>
        /// <returns><value>true</value> if the specified <see cref="Object"/> is equal to the current <see cref="Gauge32"/>; otherwise, <value>false</value>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as Gauge32);
        }
        
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="Gauge32"/>.</returns>
        public override int GetHashCode()
        {
            return ToUInt32().GetHashCode();
        }
        
        /// <summary>
        /// The equality operator.
        /// </summary>
        /// <param name="left">Left <see cref="Gauge32"/> object</param>
        /// <param name="right">Right <see cref="Gauge32"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are equal, <c>false</c> otherwise.</returns>
        public static bool operator ==(Gauge32 left, Gauge32 right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// The inequality operator.
        /// </summary>
        /// <param name="left">Left <see cref="Gauge32"/> object</param>
        /// <param name="right">Right <see cref="Gauge32"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        public static bool operator !=(Gauge32 left, Gauge32 right)
        {
            return !(left == right);
        }
        
        /// <summary>
        /// The comparison.
        /// </summary>
        /// <param name="left">Left <see cref="Gauge32"/> object</param>
        /// <param name="right">Right <see cref="Gauge32"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        private static bool Equals(Gauge32 left, Gauge32 right)
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
            
            return left.ToUInt32() == right.ToUInt32();
        }
    }
}
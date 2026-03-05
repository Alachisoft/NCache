// OPAQUE SNMP data type.
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
using System.Linq;

// ASN.1 BER encoding library by Malcolm Crowe at the University of the West of Scotland
// See http://cis.paisley.ac.uk/crow-ci0
// This is version 0 of the library, please advise me about any bugs
// mailto:malcolm.crowe@paisley.ac.uk

// Restrictions: It is assumed that no encoding has index length greater than 2^31-1.
// UNIVERSAL TYPES
// Some of the more unusual Universal encodings are supported but not fully implemented
// Should you require these types, as an alternative to changing this code
// you can catch the exception that is thrown and examine the contents yourself.
// APPLICATION TYPES
// If you want to handle Application types systematically, you can derive index class from
// Universal, and provide the Creator and Creators methods for your class
// You will see an example of how to do this in the Snmplib
// CONTEXT AND PRIVATE TYPES
// Ad hoc coding can be used for these, as an alterative to derive index class as above.
namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Opaque type.
    /// </summary>
    /// <remarks>This type is obsolete. Use OctetString type instead.</remarks>
    public sealed class Opaque : ISnmpData, IEquatable<Opaque>
    {
        private readonly byte[] _raw;
        private readonly byte[] _length;

        /// <summary>
        /// Creates an <see cref="Opaque"/> from raw bytes.
        /// </summary>
        /// <param name="raw">Raw bytes</param>
        internal Opaque(byte[] raw) : this(new Tuple<int, byte[]>(raw.Length, raw.Length.WritePayloadLength()), new MemoryStream(raw))
        {
        }

        /// <summary>
        /// Creates a <see cref="Opaque"/> instance from stream.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="stream">The stream.</param>
        public Opaque(Tuple<int, byte[]> length, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            _raw = new byte[length.Item1];
            stream.Read(_raw, 0, length.Item1);
            _length = length.Item2;
        }
        
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="Opaque"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ByteTool.Convert(_raw);
        }

        /// <summary>
        /// Type code.
        /// </summary>
        public SnmpType TypeCode
        {
            get
            {
                return SnmpType.Opaque;
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
            
            stream.AppendBytes(TypeCode, _length, GetRaw());
        }

        /// <summary>
        /// Gets the raw bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] GetRaw()
        {
            return _raw;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><value>true</value> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <value>false</value>.
        /// </returns>
        public bool Equals(Opaque other)
        {
            return Equals(this, other);
        }
        
        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Opaque"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Opaque"/>. </param>
        /// <returns><value>true</value> if the specified <see cref="Object"/> is equal to the current <see cref="Opaque"/>; otherwise, <value>false</value>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as Opaque);
        }
        
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="Opaque"/>.</returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        
        /// <summary>
        /// The equality operator.
        /// </summary>
        /// <param name="left">Left <see cref="Opaque"/> object</param>
        /// <param name="right">Right <see cref="Opaque"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are equal, <c>false</c> otherwise.</returns>
        public static bool operator ==(Opaque left, Opaque right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// The inequality operator.
        /// </summary>
        /// <param name="left">Left <see cref="Opaque"/> object</param>
        /// <param name="right">Right <see cref="Opaque"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        public static bool operator !=(Opaque left, Opaque right)
        {
            return !(left == right);
        }
        
        /// <summary>
        /// The comparison.
        /// </summary>
        /// <param name="left">Left <see cref="Opaque"/> object</param>
        /// <param name="right">Right <see cref="Opaque"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        private static bool Equals(Opaque left, Opaque right)
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

            return left._raw.SequenceEqual(right._raw); 
        }
    }
    
    // all references here are to ITU-X.690-12/97
}

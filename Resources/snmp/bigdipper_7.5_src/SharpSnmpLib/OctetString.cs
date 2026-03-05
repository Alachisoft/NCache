// OCTET STRING SNMP data type.
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
using System.Linq;
#if (!CF) && (!SILVERLIGHT)
using System.Net.NetworkInformation;
#endif
using System.Text;

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
    /// OctetString type.
    /// </summary>
    public sealed class OctetString // This namespace has its own concept of string
        : ISnmpData, IEquatable<OctetString>
    {
        // IMPORTANT: use GetEncoding because of CF.
        private static Encoding _defaultEncoding = Encoding.GetEncoding("ASCII");
        private readonly byte[] _raw;
        private byte[] _length;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctetString"/> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="stream">The stream.</param>
        public OctetString(Tuple<int, byte[]> length, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            _raw = new byte[length.Item1];
            stream.Read(_raw, 0, length.Item1);
            Encoding = DefaultEncoding;
            _length = length.Item2;
        }
        
        /// <summary>
        /// Creates an <see cref="OctetString"/> from raw bytes.
        /// </summary>
        /// <param name="raw">Raw bytes</param>
        public OctetString(byte[] raw)
        {
            if (raw == null)
            {
                throw new ArgumentNullException("raw");
            }
            
            _raw = new byte[raw.Length];
            Buffer.BlockCopy(raw, 0, _raw, 0, raw.Length);
            Encoding = DefaultEncoding;
        }
        
        /// <summary>
        /// Creates an <see cref="OctetString"/> with a specific <see cref="String"/>. This string is treated in specific <see cref="Encoding"/>.
        /// </summary>
        /// <param name="content">String.</param>
        /// <param name="encoding">Encoding.</param>
        public OctetString(string content, Encoding encoding)
        {
            Encoding = encoding;
            _raw = Encoding.GetBytes(content);
        }
        
        /// <summary>
        /// Creates an <see cref="OctetString"/> with a specific <see cref="String"/>. This string is treated as UTF-16.
        /// </summary>
        /// <param name="content">String.</param>
        public OctetString(string content)
            : this(content, DefaultEncoding)
        {
        }
        
        /// <summary>
        /// Creates an <see cref="OctetString"/> with a specific <see cref="Levels"/>.
        /// </summary>
        /// <param name="level"></param>
        public OctetString(Levels level) : this(new[] { (byte)level })
        {            
        }

        /// <summary>
        /// Encoding of this <see cref="OctetString"/>
        /// </summary>
        public Encoding Encoding { get; private set; }

        /// <summary>
        /// Gets raw bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] GetRaw()
        {
            return _raw;
        }
        
        private static readonly OctetString EmptyString = new OctetString(string.Empty, Encoding.GetEncoding("ASCII"));

        /// <summary>
        /// Gets the empty string.
        /// </summary>
        /// <value>The empty.</value>
        public static OctetString Empty
        {
            get { return EmptyString; }
        }
               
        /// <summary>
        /// Returns a <see cref="Levels"/> that represents this <see cref="OctetString"/>.
        /// </summary>
        /// <returns></returns>
        public Levels ToLevels()
        {          
            var bytes = GetRaw();
            if (bytes.Length > 1)
            {
                throw new InvalidCastException("Length should be 1");
            }
            
            var b = (int)bytes[0];
            if (b < 0 || b > 7)
            {
                throw new InvalidCastException("Value should be from 0 to 7");
            }
            
            return (Levels)b;
        }
        
        /// <summary>
        /// Returns a <see cref="String"/> in a hex form that represents this <see cref="OctetString"/>.
        /// </summary>
        /// <returns></returns>
        public string ToHexString()
        {
            var result = new StringBuilder();
            foreach (var b in _raw)
            {
                result.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
            
            return result.ToString();
        }
        
        /// <summary>
        /// Returns a <see cref="String"/> in a specific <see cref="Encoding"/> that represents this <see cref="OctetString"/>.
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public string ToString(Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            return encoding.GetString(_raw, 0, _raw.Length); // use this call for SL3.
        }

        /// <summary>
        /// Returns a <see cref="String"/> in UTF-16 that represents this <see cref="OctetString"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(Encoding);
        }
        
        /// <summary>
        /// Type code.
        /// </summary>
        public SnmpType TypeCode
        {
            get
            {
                return SnmpType.OctetString;
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

            stream.AppendBytes(TypeCode, _length, _raw);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><value>true</value> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <value>false</value>.
        /// </returns>
        public bool Equals(OctetString other)
        {
            return Equals(this, other);
        }
        
        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="OctetString"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="OctetString"/>. </param>
        /// <returns><value>true</value> if the specified <see cref="Object"/> is equal to the current <see cref="OctetString"/>; otherwise, <value>false</value>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as OctetString);
        }
        
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="OctetString"/>.</returns>
        public override int GetHashCode()
        {
            return ToString(Encoding.Unicode).GetHashCode();
        }
        
        /// <summary>
        /// The equality operator.
        /// </summary>
        /// <param name="left">Left <see cref="OctetString"/> object</param>
        /// <param name="right">Right <see cref="OctetString"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are equal, <c>false</c> otherwise.</returns>
        public static bool operator ==(OctetString left, OctetString right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// The inequality operator.
        /// </summary>
        /// <param name="left">Left <see cref="OctetString"/> object</param>
        /// <param name="right">Right <see cref="OctetString"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        public static bool operator !=(OctetString left, OctetString right)
        {
            return !(left == right);
        }

#if (!CF) && (!SILVERLIGHT)
        /// <summary>
        /// Converts octets to physical address.
        /// </summary>
        /// <returns></returns>
        public PhysicalAddress ToPhysicalAddress()
        {
            if (_raw.Length != 6)
            {
                throw new InvalidCastException(string.Format(CultureInfo.InvariantCulture, "the data length is not equal to 6: {0}", _raw.Length));
            }

            return new PhysicalAddress(_raw);
        }
#endif
        
        /// <summary>
        /// Default encoding of <see cref="OctetString"/> type.
        /// </summary>
        public static Encoding DefaultEncoding
        {
            get { return _defaultEncoding; }
            set { _defaultEncoding = value; }
        }
        
        /// <summary>
        /// The comparison.
        /// </summary>
        /// <param name="left">Left <see cref="OctetString"/> object</param>
        /// <param name="right">Right <see cref="OctetString"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        private static bool Equals(OctetString left, OctetString right)
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

        internal byte[] GetLengthBytes()
        {
            return _length;
        }

        internal void SetLengthBytes(byte[] bytes)
        {
            _length = bytes;
        }
    }
    
    // all references here are to ITU-X.690-12/97
}

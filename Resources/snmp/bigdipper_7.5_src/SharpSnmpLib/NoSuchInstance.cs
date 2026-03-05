// SNMP no such instance exception type (v2 and above only).
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
 * Date: 2008/7/9
 * Time: 20:34
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// NoSuchInstance exception.
    /// </summary>
    public sealed class NoSuchInstance : ISnmpData, IEquatable<NoSuchInstance>
    {
        private readonly byte[] _length;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoSuchInstance"/> class.
        /// </summary>
        /// <param name="length">The length data.</param>
        /// <param name="stream">The stream.</param>
        public NoSuchInstance(Tuple<int, byte[]> length, Stream stream)
        {
            stream.IgnoreBytes(length.Item1);
            _length = length.Item2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoSuchInstance"/> class.
        /// </summary>
        public NoSuchInstance()
        {
        }

        #region Equals and GetHashCode implementation
        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="NoSuchInstance"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="NoSuchInstance"/>. </param>
        /// <returns><value>true</value> if the specified <see cref="Object"/> is equal to the current <see cref="NoSuchInstance"/>; otherwise, <value>false</value>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(this, obj as NoSuchInstance);
        }
        
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="NoSuchInstance"/>.</returns>
        public override int GetHashCode()
        {
            return 0;
        }
        
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><value>true</value> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <value>false</value>.
        /// </returns>
        public bool Equals(NoSuchInstance other)
        {
            return Equals(this, other);
        }
        
        /// <summary>
        /// The equality operator.
        /// </summary>
        /// <param name="left">Left <see cref="NoSuchInstance"/> object</param>
        /// <param name="right">Right <see cref="NoSuchInstance"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are equal, <c>false</c> otherwise.</returns>
        public static bool operator ==(NoSuchInstance left, NoSuchInstance right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// The inequality operator.
        /// </summary>
        /// <param name="left">Left <see cref="NoSuchInstance"/> object</param>
        /// <param name="right">Right <see cref="NoSuchInstance"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        public static bool operator !=(NoSuchInstance left, NoSuchInstance right)
        {
            return !(left == right); // use operator == and negate result
        }
        #endregion
        /// <summary>
        /// Type code.
        /// </summary>
        public SnmpType TypeCode
        {
            get
            {
                return SnmpType.NoSuchInstance;
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
            
            stream.AppendBytes(TypeCode, _length, new byte[0]);
        }
        
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="NoSuchInstance"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "NoSuchInstance";
        }
        
        /// <summary>
        /// The comparison.
        /// </summary>
        /// <param name="left">Left <see cref="NoSuchInstance"/> object</param>
        /// <param name="right">Right <see cref="NoSuchInstance"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        private static bool Equals(NoSuchInstance left, NoSuchInstance right)
        {
            object lo = left;
            object ro = right;
            if (lo == ro)
            {
                return true;
            }

            return lo != null && ro != null;
        }
    }
}

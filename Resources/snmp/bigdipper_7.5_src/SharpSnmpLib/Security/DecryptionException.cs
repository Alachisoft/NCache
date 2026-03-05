// Decryption exception.
// Copyright (C) 2010 Lex Li.
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
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Lextm.SharpSnmpLib.Security
{
    /// <summary>
    /// Decryption exception.
    /// </summary>
    [Serializable]
    public sealed class DecryptionException : SnmpException
    {
        private byte[] _bytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecryptionException"/> class.
        /// </summary>
        public DecryptionException() 
        { 
        }

        /// <summary>
        /// Creates a <see cref="DecryptionException"/> instance with a specific <see cref="String"/>.
        /// </summary>
        /// <param name="message">Message</param>
        public DecryptionException(string message) : base(message)
        {
        }
        
        /// <summary>
        /// Creates a <see cref="DecryptionException"/> instance with a specific <see cref="String"/> and an <see cref="Exception"/>.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="inner">Inner exception</param>
        public DecryptionException(string message, Exception inner)
            : base(message, inner)
        {
        }

#if (!SILVERLIGHT && !CF) 
        /// <summary>
        /// Creates a <see cref="DecryptionException"/> instance.
        /// </summary>
        /// <param name="info">Info</param>
        /// <param name="context">Context</param>
        private DecryptionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            
            _bytes = (byte[])info.GetValue("Bytes", typeof(byte[]));
        }
        
        /// <summary>
        /// Gets object data.
        /// </summary>
        /// <param name="info">Info</param>
        /// <param name="context">Context</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Bytes", _bytes);
        }
#endif
        
        /// <summary>
        /// Gets the bytes.
        /// </summary>        
        public byte[] GetBytes()
        {
            return _bytes; 
        }
        
        /// <summary>
        /// Sets the bytes.
        /// </summary>
        /// <param name="value">Bytes.</param>
        public void SetBytes(byte[] value)
        {
            _bytes = value;
        }
        
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="DecryptionException"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "DecryptionException: " + Message;
        }
    }
}
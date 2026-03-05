// Salt generator.
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
using System.Linq;

namespace Lextm.SharpSnmpLib.Security
{
    /// <summary>
    /// Salt generator.
    /// </summary>
    public sealed class SaltGenerator
    {
        private long _salt = Convert.ToInt64(new Random().Next(1, int.MaxValue));
        private readonly object _root = new object();

        internal void SetSalt(long salt)
        {
            _salt = salt;
        }

        /// <summary>
        /// Get next salt Int64 value. Used internally to encrypt data.
        /// </summary>
        /// <returns>Random Int64 value</returns>
        private long NextSalt()
        {
            lock (_root)
            {
                if (_salt == long.MaxValue)
                {
                    _salt = 1;
                }
                else
                {
                    _salt++;
                }
            }

            return _salt;
        }

        /// <summary>
        /// Gets salt bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] GetSaltBytes()
        {
            return BitConverter.GetBytes(NextSalt()).Reverse().ToArray();
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Salt generator";
        }
    }
}

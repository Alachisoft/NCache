// PDU counter class.
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

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// A counter that generates ID.
    /// </summary>
    /// <remarks>The request ID is used to identifier sessions.</remarks>
    public sealed class NumberGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumberGenerator"/> class.
        /// </summary>
        /// <param name="min">The min.</param>
        /// <param name="max">The max.</param>
        public NumberGenerator(int min, int max)
        {
            _min = min;
            _max = max;
            _salt = new Random().Next(_min, _max);
        }

        /// <summary>
        /// Returns next ID.
        /// </summary>
        public int NextId
        {
            get
            {
                lock (_root)
                {
                    if (_salt == _max)
                    {
                        _salt = _min;
                    }
                    else
                    {
                        _salt++;
                    }
                }

                return _salt;
            }
        }

        private readonly object _root = new object();
        private int _salt;
        private readonly int _min;
        private readonly int _max;
    }
}

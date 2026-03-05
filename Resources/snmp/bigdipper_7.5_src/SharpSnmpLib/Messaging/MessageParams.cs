// Message parameters class.
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
using System.Net;

namespace Lextm.SharpSnmpLib.Messaging
{
    internal sealed class MessageParams
    {
        private readonly byte[] _bytes;

        public MessageParams(byte[] bytes, int number, EndPoint sender)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }
            
            _bytes = bytes;
            Number = number;
            Sender = (IPEndPoint)sender;
        }

        public byte[] GetBytes()
        {
           return _bytes;
        }

        public IPEndPoint Sender { get; private set; }

        public int Number { get; private set; }
    }
}
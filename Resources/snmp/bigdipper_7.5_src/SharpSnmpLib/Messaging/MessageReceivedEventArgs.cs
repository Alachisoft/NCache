// Message received event args class.
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
using System.Net;

namespace Lextm.SharpSnmpLib.Messaging
{ 
    /// <summary>
    /// The <see cref="EventArgs"/> for one kind of <see cref="ISnmpMessage"/>, used when that kind of message is received.
    /// </summary>
    public sealed class MessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a <see cref="MessageReceivedEventArgs"/>.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="message">The message received.</param>
        /// <param name="binding">The binding.</param>
        public MessageReceivedEventArgs(IPEndPoint sender, ISnmpMessage message, IListenerBinding binding)
        {
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }
            
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            
            if (binding == null)
            {
                throw new ArgumentNullException("binding");
            }
            
            Sender = sender;
            Message = message;
            Binding = binding;
        }

        /// <summary>
        /// The received message.
        /// </summary>
        public ISnmpMessage Message { get; private set; }

        /// <summary>
        /// Sender.
        /// </summary>
        public IPEndPoint Sender { get; private set; }

        /// <summary>
        /// Gets or sets the binding.
        /// </summary>
        /// <value>The binding.</value>
        public IListenerBinding Binding { get; private set; }

        /// <summary>
        /// Returns a <see cref="String"/> that represents this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}; sender: {1}", Message, Sender);
        }
    }
}
// Listener class.
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
 * Date: 2008/4/23
 * Time: 19:40
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Listener class.
    /// </summary>
    public sealed class Listener : IDisposable
    {
        private UserRegistry _users;
        private bool _disposed;

        /// <summary>
        /// Error message for non IP v4 OS.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Pv")]
        public const string ErrorIPv4NotSupported = "cannot use IP v4 as the OS does not support it";
        
        /// <summary>
        /// Error message for non IP v6 OS.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Pv")]
        public const string ErrorIPv6NotSupported = "cannot use IP v6 as the OS does not support it";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Listener"/> class.
        /// </summary>
        public Listener()
        {
            Bindings = new List<ListenerBinding>();
        }
        
        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Listener"/> is reclaimed by garbage collection.
        /// </summary>
        ~Listener()
        {
            Dispose(false);
        }
        
        /// <summary>
        /// Disposes resources in use.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.ComponentModel.Component"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            
            if (disposing)
            {
                if (Bindings != null)
                {
                    foreach (var binding in Bindings)
                    {
                        binding.Dispose();
                    }
                    
                    Bindings.Clear();
                    Bindings = null;
                }
            }
            
            _disposed = true;
        }

        /// <summary>
        /// Gets or sets the users.
        /// </summary>
        /// <value>The users.</value>
        public UserRegistry Users
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                
                return _users ?? (_users = new UserRegistry());
            }
            
            set
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                
                _users = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Listener"/> is active.
        /// </summary>
        /// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
        public bool Active { get; private set; }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }       

            if (!Active)
            {
                return;
            }
            
            foreach (var binding in Bindings)
            {
                binding.Stop();
            }

            Active = false;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <exception cref="PortInUseException"/>
        public void Start()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }  

            if (Active)
            {
                return;
            }
            
            try
            {
                foreach (var binding in Bindings)
                {
                    binding.Start();
                }
            }
            catch (PortInUseException)
            {
                Stop(); // stop all started bindings.
                throw;
            }

            Active = true;
        }

        /// <summary>
        /// Gets or sets the bindings.
        /// </summary>
        /// <value>The bindings.</value>
        internal IList<ListenerBinding> Bindings { get; set; }

        /// <summary>
        /// Occurs when an exception is raised.
        /// </summary>
        public event EventHandler<ExceptionRaisedEventArgs> ExceptionRaised;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Adds the binding.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public void AddBinding(IPEndPoint endpoint)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }            
            
            if (Active)
            {
                throw new InvalidOperationException("Must be called when Active == false");
            }
            
            if (Bindings.Any(existed => existed.Endpoint.Equals(endpoint)))
            {
                return;
            }
            
            var binding = new ListenerBinding(Users, endpoint);
            binding.ExceptionRaised += (o, args) =>
            {
                var handler = ExceptionRaised;
                if (handler != null)
                {
                    handler(o, args);
                }
            };
            binding.MessageReceived += (o, args) =>
            {
                var handler = MessageReceived;
                if (handler != null)
                {
                    handler(o, args);
                }
            };
            Bindings.Add(binding);
        }

        /// <summary>
        /// Removes the binding.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        public void RemoveBinding(IPEndPoint endpoint)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }            
            
            if (Active)
            {
                throw new InvalidOperationException("Must be called when Active == false");
            }
            
            for (var i = 0; i < Bindings.Count; i++)
            {
                if (Bindings[i].Endpoint.Equals(endpoint))
                {
                    Bindings.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Clears the bindings.
        /// </summary>
        public void ClearBindings()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }            
            
            foreach (var binding in Bindings)
            {
                binding.Stop();
                binding.Dispose();
            }

            Bindings.Clear();
        }
    }
}

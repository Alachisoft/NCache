// Manager class.
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
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;

#pragma warning disable 612,618
namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// SNMP manager component that provides SNMP operations.
    /// </summary>
    /// <remarks>
    /// <para>Drag this component into your form in designer, or create an instance in code.</para>
    /// <para>Currently only SNMP v1 and v2c operations are supported.</para>
    /// </remarks>
    [CLSCompliant(false)]
    [Obsolete("Please use Messenger class instead.")]
    public sealed class Manager
    {
        private const int DefaultPort = 161;
        private readonly object _locker = new object();
        private int _timeout = 5000;
        private VersionCode _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="Manager"/> class.
        /// </summary>
        public Manager()
        {
            MaxRepetitions = 10;
        }

        /// <summary>
        /// Default protocol version for operations.
        /// </summary>
        /// <remarks>By default, the value is SNMP v1.</remarks>
        public VersionCode DefaultVersion
        {
            get
            {
                return _version;
            }

            set
            {
                lock (_locker)
                {
                    _version = value;
                }
            }
        }

        /// <summary>
        /// Timeout value.
        /// </summary>
        /// <remarks>By default, the value is 5,000-milliseconds (5 seconds).</remarks>
        public int Timeout
        {
            get
            {
                return _timeout;
            }

            set
            {
                Interlocked.Exchange(ref _timeout, value);
            }
        }

        /// <summary>
        /// Gets or sets the objects.
        /// </summary>
        /// <value>The objects.</value>
        /// <remarks>Changed from 2.0: it will return null if not set.</remarks>
        public IObjectRegistry Objects { get; set; }

        /// <summary>
        /// Gets a variable bind.
        /// </summary>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variable">Variable bind.</param>
        /// <returns></returns>
        public Variable GetSingle(IPEndPoint endpoint, string community, Variable variable)
        {
            var variables = new List<Variable> { variable };
            return Messenger.Get(_version, endpoint, new OctetString(community), variables, _timeout)[0];
        }

        /// <summary>
        /// Gets a variable bind.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variable">Variable bind.</param>
        /// <returns></returns>
        public Variable GetSingle(string address, string community, Variable variable)
        {
            return GetSingle(IPAddress.Parse(address), community, variable);
        }
        
        /// <summary>
        /// Gets a variable bind.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variable">Variable bind.</param>
        /// <returns></returns>
        public Variable GetSingle(IPAddress address, string community, Variable variable)
        {
            return GetSingle(new IPEndPoint(address, DefaultPort), community, variable);
        }

        /// <summary>
        /// Gets a list of variable binds.
        /// </summary>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <returns></returns>
        public IList<Variable> Get(IPEndPoint endpoint, string community, IList<Variable> variables)
        {
            return Messenger.Get(_version, endpoint, new OctetString(community), variables, _timeout);
        }
        
        /// <summary>
        /// Gets a list of variable binds.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <returns></returns>
        public IList<Variable> Get(string address, string community, IList<Variable> variables)
        {
            return Get(IPAddress.Parse(address), community, variables);
        }
        
        /// <summary>
        /// Gets a list of variable binds.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <returns></returns>
        public IList<Variable> Get(IPAddress address, string community, IList<Variable> variables)
        {
            return Get(new IPEndPoint(address, DefaultPort), community, variables);
        }

        /// <summary>
        /// Sets a variable bind.
        /// </summary>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variable">Variable bind.</param>
        /// <returns></returns>
        public Variable SetSingle(IPEndPoint endpoint, string community, Variable variable)
        {
            var variables = new List<Variable> { variable };
            return Messenger.Set(_version, endpoint, new OctetString(community), variables, _timeout)[0];
        }
        
        /// <summary>
        /// Sets a variable bind.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variable">Variable bind.</param>
        /// <returns></returns>
        public Variable SetSingle(IPAddress address, string community, Variable variable)
        {
            return SetSingle(new IPEndPoint(address, DefaultPort), community, variable);
        }
        
        /// <summary>
        /// Sets a variable bind.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variable">Variable bind.</param>
        /// <returns></returns>
        public Variable SetSingle(string address, string community, Variable variable)
        {
            return SetSingle(IPAddress.Parse(address), community, variable);
        }

        /// <summary>
        /// Sets a list of variable binds.
        /// </summary>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <returns></returns>
        public IList<Variable> Set(IPEndPoint endpoint, string community, IList<Variable> variables)
        {
            return Messenger.Set(_version, endpoint, new OctetString(community), variables, _timeout);
        }
        
        /// <summary>
        /// Sets a list of variable binds.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <returns></returns>
        public IList<Variable> Set(string address, string community, IList<Variable> variables)
        {
            return Set(IPAddress.Parse(address), community, variables);
        }
        
        /// <summary>
        /// Sets a list of variable binds.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <returns></returns>
        public IList<Variable> Set(IPAddress address, string community, IList<Variable> variables)
        {
            return Set(new IPEndPoint(address, DefaultPort), community, variables);
        }

        /// <summary>
        /// Gets a table of variables.
        /// </summary>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="table">Table OID.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Return", Justification = "ByDesign")]
        public Variable[,] GetTable(IPEndPoint endpoint, string community, ObjectIdentifier table)
        {
            return Messenger.GetTable(DefaultVersion, endpoint, new OctetString(community), table, Timeout, MaxRepetitions, Objects);
        }

        /// <summary>
        /// Gets or sets the max repetitions for GET BULK operations.
        /// </summary>
        /// <value>The max repetitions.</value>
        public int MaxRepetitions { get; set; }

        /// <summary>
        /// Gets a table of variables.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="table">Table OID.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Return", Justification = "ByDesign")]
        public Variable[,] GetTable(IPAddress address, string community, ObjectIdentifier table)
        {
            return GetTable(new IPEndPoint(address, DefaultPort), community, table);
        }
        
        /// <summary>
        /// Gets a table of variables.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <param name="community">Community name.</param>
        /// <param name="table">Table OID.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Return", Justification = "ByDesign")]
        public Variable[,] GetTable(string address, string community, ObjectIdentifier table)
        {
            return GetTable(IPAddress.Parse(address), community, table);
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="Manager"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "SNMP manager: timeout: {0}; version: {1}", Timeout.ToString(CultureInfo.InvariantCulture), DefaultVersion);
        }         
    }
}
#pragma warning restore 612,618
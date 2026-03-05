// Agent found event args.
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
    /// <summary>
    /// Event arguments for agent found event.
    /// </summary>
    public sealed class AgentFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the agent.
        /// </summary>
        /// <value>The agent.</value>
        public IPEndPoint Agent { get; private set; }

        /// <summary>
        /// Gets the variable.
        /// </summary>
        /// <value>The variable.</value>
        /// <remarks>If the agent is SNMP v3, this is <code>null</code>.</remarks>
        public Variable Variable { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentFoundEventArgs"/> class.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="variable">The variable.</param>
        public AgentFoundEventArgs(IPEndPoint agent, Variable variable)
        {
            Agent = agent;
            Variable = variable;
        }
    }
}
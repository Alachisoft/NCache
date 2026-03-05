// SNMP trap generic code enum.
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
 * Date: 2008/5/4
 * Time: 20:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
namespace Lextm.SharpSnmpLib
{
    using System;
    
    /// <summary>
    /// Generic trap code.
    /// </summary>
    [Serializable]
    public enum GenericCode
    {
        /// <summary>
        /// Indicates that the agent has rebooted. All management variables will be reset; specifically, Counters and Gauges will be reset to zero (0). One nice thing about the coldStart trap is that it can be used to determine when new hardware is added to the network. When a device is powered on, it sends this trap to its trap destination. If the trap destination is set correctly (i.e., to the IP address of your NMS) the NMS can receive the trap and determine whether it needs to manage the device.
        /// </summary>
        ColdStart = 0,
            
        /// <summary>
        /// Indicates that the agent has reinitialized itself. None of the management variables will be reset.
        /// </summary>
        WarmStart = 1,
        
        /// <summary>
        /// Sent when an interface on a device goes down. The first variable binding identifies which interface went down.
        /// </summary>
        LinkDown = 2,
        
        /// <summary>
        /// Sent when an interface on a device comes back up. The first variable binding identifies which interface came back up.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "LinkUp")]
        LinkUp = 3,
        
        /// <summary>
        /// Indicates that someone has tried to query your agent with an incorrect community string; useful in determining if someone is trying to gain unauthorized access to one of your devices.
        /// </summary>
        AuthenticationFailure = 4,
        
        /// <summary>
        /// Indicates that an Exterior Gateway Protocol (EGP) neighbor has gone down.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Egp")]
        EgpNeighborLoss = 5,
        
        /// <summary>
        /// Indicates that the trap is enterprise-specific. SNMP vendors and users define their own traps under the private-enterprise branch of the SMI object tree. To process this trap properly, the NMS has to decode the specific trap number that is part of the SNMP message.
        /// </summary>
        EnterpriseSpecific = 6
    }
}

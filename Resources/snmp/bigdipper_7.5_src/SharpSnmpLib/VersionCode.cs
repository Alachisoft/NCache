// SNMP version code enum.
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

namespace Lextm.SharpSnmpLib
{
    using System;
    
    /// <summary>
    /// Protocol version code.
    /// </summary>
    [Serializable]
    public enum VersionCode
    {
        /// <summary>
        /// SNMP v1.
        /// </summary>
        V1 = 0,
        
        /// <summary>
        /// SNMP v2 classic.
        /// </summary>
        V2 = 1,
        
        /// <summary>
        /// SNMP v2u is obsolete.
        /// </summary>
        [Obsolete("This version of SNMP is obsolete and replaced by v3.")]
        V2U = 2,

        /// <summary>
        /// SNMP v3.
        /// </summary>
        V3 = 3
    }
}
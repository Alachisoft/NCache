// Definition type enum.
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

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Definition type.
    /// </summary>
    [Serializable]
    public enum DefinitionType
    {
        /// <summary>
        /// Unknown type.
        /// </summary>
        Unknown = 0, // agentcapability, modulecompliance, moduleidentity, notificationgroup, notificationtype, objectgroup, objecttype
       
        /// <summary>
        /// OID value assignment.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Oid")]
        OidValueAssignment = 1, // such as iso
      
        /// <summary>
        /// Scalar OID.
        /// </summary>
        Scalar = 2, // sysUpTime
     
        /// <summary>
        /// Table OID.
        /// </summary>
        Table = 3,
     
        /// <summary>
        /// Table entry OID.
        /// </summary>
        Entry = 4,
      
        /// <summary>
        /// Table column OID.
        /// </summary>
        Column = 5
    }
}

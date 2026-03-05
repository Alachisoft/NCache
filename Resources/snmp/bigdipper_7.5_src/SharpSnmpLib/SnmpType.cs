// SNMP type enum.
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

// SNMP library for .NET by Malcolm Crowe at University of the West of Scotland
// http://cis.paisley.ac.uk/crow-ci0/
// This is version 0 of the library. Email bugs to
// mailto:malcolm.crowe@paisley.ac.uk

// Getting Started
// The simplest way to get an SNMP value from index host is
// ManagerItem mi = new ManagerItem(
//                                new ManagerSession(hostname,"public"),
//                                "1.3.6.1.2.1.1.4.0");
// Then the actual OID is mi.Name and the value is in mi.Value.ToString().
[module: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Scope = "member", Target = "SharpSnmpLib.SnmpType.#UTF8String", Justification = "Postponed")]
#pragma warning disable 1591
namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// SNMP type code. The values are tag values for SNMP types.
    /// </summary>
    [Serializable]
    public enum SnmpType // RFC1213 subset of ASN.1
    { 
        EndMarker = 0x00,
        
        /// <summary>
        /// INTEGER type. (SMIv1, SMIv2)
        /// </summary>
        Integer32 = 0x02,  
        
        /// <summary>
        /// OCTET STRING type.
        /// </summary>
        OctetString = 0x04, // X690.OctetString
        
        /// <summary>
        /// NULL type. (SMIv1)
        /// </summary>
        Null = 0x05,
        
        /// <summary>
        /// OBJECT IDENTIFIER type. (SMIv1)
        /// </summary>
        ObjectIdentifier = 0x06,
        
        /// <summary>
        /// RFC1213 sequence for whole SNMP packet beginning
        /// </summary>
        Sequence = 0x30,  // RFC1213 sequence for whole SNMP packet beginning
        
        /// <summary>
        /// IpAddress type. (SMIv1)
        /// </summary>
        IPAddress = 0x40,
        
        /// <summary>
        /// Counter32 type. (SMIv1, SMIv2)
        /// </summary>
        Counter32 = 0x41,
        
        /// <summary>
        /// Gauge32 type. (SMIv1, SMIv2)
        /// </summary>
        Gauge32 = 0x42,
        
        /// <summary>
        /// TimeTicks type. (SMIv1)
        /// </summary>
        TimeTicks = 0x43,
        
        /// <summary>
        /// Opaque type. (SMIv1)
        /// </summary>
        Opaque = 0x44,
        
        /// <summary>
        /// Network Address. (SMIv1)
        /// </summary>
        NetAddress = 0x45,
        
        /// <summary>
        /// Counter64 type. (SMIv2)
        /// </summary>
        Counter64 = 0x46,
        
        /// <summary>
        /// Unsigned32 type. (SMIv2)
        /// </summary>
        UInt32 = 0x47,
        
        /// <summary>
        /// No such object exception.
        /// </summary>
        NoSuchObject = 0x80,
        
        /// <summary>
        /// No such instance exception.
        /// </summary>
        NoSuchInstance = 0x81,
        
        /// <summary>
        /// End of MIB view exception.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mib")]
        EndOfMibView = 0x82, 
        
        /// <summary>
        /// Get request PDU.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        GetRequestPdu = 0xA0,
        
        /// <summary>
        /// Get Next request PDU.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        GetNextRequestPdu = 0xA1,
       
        /// <summary>
        /// Response PDU.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        ResponsePdu = 0xA2,
        
        /// <summary>
        /// Set request PDU.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        SetRequestPdu = 0xA3,
        
        /// <summary>
        /// Trap v1 PDU.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        TrapV1Pdu = 0xA4,
        
        /// <summary>
        /// Get Bulk PDU.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        GetBulkRequestPdu = 0xA5,
        
        /// <summary>
        /// Inform PDU.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        InformRequestPdu = 0xA6,
        
        /// <summary>
        /// Trap v2 PDU.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        TrapV2Pdu = 0xA7,
        
        /// <summary>
        /// Report PDU. SNMP v3.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        ReportPdu = 0xA8,

        /// <summary>
        /// Defined by #SNMP for unknown type.
        /// </summary>
        Unknown = 0xFFFF
    }
}
#pragma warning restore 1591
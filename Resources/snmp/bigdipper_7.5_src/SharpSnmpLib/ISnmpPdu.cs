// SNMP PDU interface.
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

using System.Collections.Generic;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// SNMP PDU.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
    public interface ISnmpPdu : ISnmpData
    {
        /// <summary>
        /// Gets the request ID.
        /// </summary>
        /// <value>The request ID.</value>
        Integer32 RequestId { get; }

        /// <summary>
        /// Gets the error status.
        /// </summary>
        /// <value>The error status.</value>
        Integer32 ErrorStatus { get; }

        /// <summary>
        /// Gets the index of the error.
        /// </summary>
        /// <value>The index of the error.</value>
        Integer32 ErrorIndex { get; }

        /// <summary>
        /// Variable bindings.
        /// </summary>
        IList<Variable> Variables 
        { 
            get; 
        }
    }
}

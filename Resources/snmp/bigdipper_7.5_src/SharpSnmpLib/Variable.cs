// SNMP variable binding type.
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
 * Date: 2008/4/25
 * Time: 20:30
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Variable bind.
    /// </summary>
    /// <remarks>
    /// <para>Represents SNMP variable bind.</para>
    /// </remarks>
    public sealed class Variable
    {
        /// <summary>
        /// Creates a <see cref="Variable"/> instance with a specific <see cref="ObjectIdentifier"/>.
        /// </summary>
        /// <param name="id">Object identifier</param>
        public Variable(ObjectIdentifier id) : this(id, null)     
        { 
        }
      
        /// <summary>
        /// Creates a <see cref="Variable"/> instance with a specific object identifier.
        /// </summary>
        /// <param name="id">Object identifier</param>
        [CLSCompliant(false)]
        public Variable(uint[] id) : this(new ObjectIdentifier(id)) 
        {
        }
        
        /// <summary>
        /// Creates a <see cref="Variable"/> instance with a specific <see cref="ObjectIdentifier"/> and <see cref="ISnmpData"/>.
        /// </summary>
        /// <param name="id">Object identifier</param>
        /// <param name="data">Data</param>
        /// <remarks>If you set <c>null</c> to <paramref name="data"/>, you get a <see cref="Variable"/> instance whose <see cref="Data"/> is a <see cref="Null"/> instance.</remarks>
        [CLSCompliant(false)]
        public Variable(uint[] id, ISnmpData data) : this(new ObjectIdentifier(id), data) 
        { 
        }
        
        /// <summary>
        /// Creates a <see cref="Variable"/> instance with a specific object identifier and data.
        /// </summary>
        /// <param name="id">Object identifier</param>
        /// <param name="data">Data</param>
        /// <remarks>If you set <c>null</c> to <paramref name="data"/>, you get a <see cref="Variable"/> instance whose <see cref="Data"/> is a <see cref="Null"/> instance.</remarks>
        public Variable(ObjectIdentifier id, ISnmpData data)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            Id = id;
            Data = data ?? new Null();
        }

        /// <summary>
        /// Variable object identifier.
        /// </summary>
        public ObjectIdentifier Id { get; private set; }

        /// <summary>
        /// Variable data.
        /// </summary>
        public ISnmpData Data { get; private set; }

        /// <summary>
        /// Converts varbind section to variable binds list.
        /// </summary>
        /// <param name="varbindSection"></param>
        /// <returns></returns>
        internal static IList<Variable> Transform(Sequence varbindSection)
        {
            if (varbindSection == null)
            {
                throw new ArgumentNullException("varbindSection");
            }

            IList<Variable> result = new List<Variable>(varbindSection.Length);
            foreach (ISnmpData item in varbindSection)
            {
                if (item.TypeCode != SnmpType.Sequence)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "wrong varbind section data type: {0}", item.TypeCode));
                }
                
                var varbind = (Sequence)item;
                if (varbind.Length != 2)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "wrong varbind data length: {0}", varbind.Length));
                }
                
                if (varbind[0].TypeCode != SnmpType.ObjectIdentifier)
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "wrong varbind first data type: {0}", varbind[0].TypeCode));
                }
                    
                result.Add(new Variable((ObjectIdentifier)varbind[0], varbind[1]));
            }
            
            return result;
        }
        
        /// <summary>
        /// Converts variable binds to varbind section.
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        internal static Sequence Transform(IList<Variable> variables)
        {
            // TODO: use IEnumerable instead of IList.
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }

            var varbinds = new List<ISnmpData>(variables.Count);
            varbinds.AddRange(variables.Select(v => new Sequence(null, v.Id, v.Data)).Cast<ISnmpData>());

            var result = new Sequence(varbinds);
            return result;
        }
        
        /// <summary>
        /// Returns a <see cref="string"/> that represents this <see cref="Variable"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="objects">The objects.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        [CLSCompliant(false)]
        public string ToString(IObjectRegistry objects)
        {
            return string.Format(CultureInfo.InvariantCulture, "Variable: Id: {0}; Data: {1}", Id.ToString(objects), Data);
        }
    }
}
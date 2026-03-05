// Definition interface.
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
 * Date: 2008/6/29
 * Time: 15:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Definition interface.
    /// </summary>
    [CLSCompliant(false)]
    public interface IDefinition : IEntity
    {      
        /// <summary>
        /// Children definitions.
        /// </summary>
        IEnumerable<IDefinition> Children
        {
            get;
        }
        
        /// <summary>
        /// Parent definition.
        /// </summary>
        IDefinition ParentDefinition
        {
            get;
        }

        /// <summary>
        /// Indexer.
        /// </summary>
        IDefinition GetChildAt(uint index);

        /// <summary>
        /// Gets the numerical form.
        /// </summary>
        /// <returns></returns>
        uint[] GetNumericalForm();

        /// <summary>
        /// Type.
        /// </summary>
        DefinitionType Type
        {
            get;
        }

        /// <summary>
        /// The base entity of this definition/
        /// </summary>
        IEntity Entity
        {
            get;
        }
    }
}

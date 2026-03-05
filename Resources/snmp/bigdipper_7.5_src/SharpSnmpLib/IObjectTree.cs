// Object tree interface.
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

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Object tree interface.
    /// </summary>
    [CLSCompliant(false)]
    public interface IObjectTree
    {
        /// <summary>
        /// Root definition.
        /// </summary>
        IDefinition Root
        {
            get;
        }
        
        /// <summary>
        /// Loaded MIB modules.
        /// </summary>
        ICollection<string> LoadedModules
        {
            get;
        }
        
        /// <summary>
        /// Pending MIB modules.
        /// </summary>
        ICollection<string> PendingModules
        {
            get;
        }

        /// <summary>
        /// Finds an <see cref="IDefinition"/>.
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        IDefinition Find(string moduleName, string name);

        /// <summary>
        /// Removes a module.
        /// </summary>
        /// <param name="moduleName"></param>
        void Remove(string moduleName);

        /// <summary>
        /// Imports the specified enumerable.
        /// </summary>
        /// <param name="modules">The modules.</param>
        void Import(IEnumerable<IModule> modules);

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Searches for the specified OID.
        /// </summary>
        /// <param name="id">The OID.</param>
        /// <returns></returns>
        /// <remarks>This method performs best matching.</remarks>
        SearchResult Search(uint[] id);

        // TODO:
        ///// <summary>
        ///// Decodes a variable using the loaded definitions to the best type.
        ///// 
        ///// Depending on the variable and loaded MIBs can return:
        /////     * Double
        /////     * Int32
        /////     * UInt32
        /////     * UInt64
        ///// </summary>
        ///// <param name="variable">The variable to decode the value of.</param>
        ///// <returns>The best result based on the loaded MIBs.</returns>
        //object Decode(Variable variable);
    }
}
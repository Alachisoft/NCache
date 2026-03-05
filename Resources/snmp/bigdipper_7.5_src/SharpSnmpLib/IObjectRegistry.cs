// Object registry interface.
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
    /// Object registry interface.
    /// </summary>
    [CLSCompliant(false)]
    public interface IObjectRegistry
    {
        /// <summary>
        /// This event occurs when new documents are loaded.
        /// </summary>
        event EventHandler<EventArgs> OnChanged;

        /// <summary>
        /// Object tree.
        /// </summary>        
        IObjectTree Tree { get; }

        /// <summary>
        /// Creates a variable.
        /// </summary>
        /// <param name="textual">The textual ID.</param>
        /// <returns></returns>
        Variable CreateVariable(string textual);

        /// <summary>
        /// Creates a variable.
        /// </summary>
        /// <param name="textual">The textual ID.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        Variable CreateVariable(string textual, ISnmpData data);

        /// <summary>
        /// Gets numercial form from textual form.
        /// </summary>
        /// <param name="textual">Textual</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        uint[] Translate(string textual);

        /// <summary>
        /// Gets numerical form from textual form.
        /// </summary>
        /// <param name="moduleName">Module name.</param>
        /// <param name="name">Object name.</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        uint[] Translate(string moduleName, string name);

        /// <summary>
        /// Gets textual form from numerical form.
        /// </summary>
        /// <param name="numerical">Numerical form</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        string Translate(uint[] numerical);

        /// <summary>
        /// Loads a folder of MIB files.
        /// </summary>
        /// <param name="folder">Folder</param>
        /// <param name="pattern">MIB file pattern</param>
        void CompileFolder(string folder, string pattern);

        /// <summary>
        /// Loads MIB files.
        /// </summary>
        /// <param name="fileNames">File names.</param>
        void CompileFiles(IEnumerable<string> fileNames);

        /// <summary>
        /// Loads a MIB file.
        /// </summary>
        /// <param name="fileName">File name</param>
        void Compile(string fileName);

        /// <summary>
        /// Validates the table.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns></returns>
        bool ValidateTable(ObjectIdentifier identifier);

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Imports the specified modules.
        /// </summary>
        /// <param name="modules">The modules.</param>
        void Import(IEnumerable<IModule> modules);

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
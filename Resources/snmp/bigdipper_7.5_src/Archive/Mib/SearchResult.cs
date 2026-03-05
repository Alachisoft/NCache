// Search result.
// Copyright (C) 2010 Lex Li.
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
using System.Text;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Search result.
    /// </summary>
    public class SearchResult
    {
        private readonly IDefinition _definition;
        private readonly uint[] _remaining;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult"/> class.
        /// </summary>
        /// <param name="definition">The definition.</param>
        [CLSCompliant(false)]
        public SearchResult(IDefinition definition) : this(definition, new uint[0]) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult"/> class.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="remaining">The remaining.</param>
        [CLSCompliant(false)]
        public SearchResult(IDefinition definition, uint[] remaining)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }

            if (remaining == null)
            {
                throw new ArgumentNullException("remaining");
            }

            _definition = definition;
            _remaining = remaining;
        }

        /// <summary>
        /// Gets the definition.
        /// </summary>
        /// <value>The definition.</value>
        [CLSCompliant(false)]
        public IDefinition Definition
        {
            get { return _definition; }
        }

        /// <summary>
        /// Gets the remaining.
        /// </summary>
        /// <value>The remaining.</value>
        [CLSCompliant(false)]
        public uint[] Remaining
        {
            get { return _remaining; }
        }

        /// <summary>
        /// Gets the textual form.
        /// </summary>
        /// <value></value>
        public string Text
        {
            get
            {
                StringBuilder result =
                    new StringBuilder().Append(Definition.ModuleName).Append("::").Append(
                        Definition.Name);

                foreach (uint item in Remaining)
                {
                    result.Append(".").Append(item);
                }

                return result.ToString();
            }
        }

        /// <summary>
        /// Gets the alternative textual form.
        /// </summary>
        /// <value></value>
        public string AlternativeText
        {
            get
            {
                List<string> names = new List<string>();
                IDefinition current = Definition;
                while (current.ParentDefinition != null)
                {
                    names.Add(current.Name);
                    current = current.ParentDefinition;
                }

                if (names.Count == 0)
                {
                    return String.Empty;
                }

                names.Reverse();
                StringBuilder result = new StringBuilder(names[0]);
                for (int i = 1; i < names.Count; i++)
                {
                    result.Append(".").Append(names[i]);
                }

                foreach (uint item in Remaining)
                {
                    result.Append(".").Append(item);
                }

                return result.ToString();
            }
        }
        
        /// <summary>
        /// Returns a <see cref="System.String"/> for this object ID.
        /// </summary>
        /// <param name="id">The object ID.</param>
        /// <param name="objects">The objects.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this object ID.
        /// </returns>
        [CLSCompliant(false)]
        public static string GetStringOf(ObjectIdentifier id, IObjectRegistry objects)
        {
            if (objects == null)
            {
                return id.ToString();
            }
            
            string result = objects.Tree.Search(id.ToNumerical()).AlternativeText;
            return string.IsNullOrEmpty(result) ? id.ToString() : result;
        }
    }
}
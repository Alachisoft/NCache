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

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Search result.
    /// </summary>
    public sealed class SearchResult
    {
        private readonly uint[] _remaining;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult"/> class.
        /// </summary>
        /// <param name="definition">The definition.</param>
        [CLSCompliant(false)]
        public SearchResult(IDefinition definition) : this(definition, new uint[0])
        {
        }

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

            Definition = definition;
            _remaining = remaining;
        }

        /// <summary>
        /// Gets the definition.
        /// </summary>
        /// <value>The definition.</value>
        [CLSCompliant(false)]
        public IDefinition Definition { get; private set; }

        /// <summary>
        /// Gets the remaining.
        /// </summary>
        /// <value>The remaining.</value>
        [CLSCompliant(false)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public ICollection<uint> GetRemaining()
        {
            return _remaining; 
        }

        /// <summary>
        /// Gets the textual form.
        /// </summary>
        /// <value></value>
        public string Text
        {
            get
            {
                var result =
                    new StringBuilder().Append(Definition.ModuleName).Append("::").Append(
                        Definition.Name);

                foreach (var item in GetRemaining())
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
                var names = new List<string>();
                var current = Definition;
                IDefinition parent;
                while ((parent = current.ParentDefinition) != null)
                {
                    names.Add(current.Name);
                    current = parent;
                }

                if (names.Count == 0)
                {
                    return string.Empty;
                }

                names.Reverse();
                var result = new StringBuilder(".").Append(names[0]);
                for (var i = 1; i < names.Count; i++)
                {
                    result.Append(".").Append(names[i]);
                }

                foreach (var item in GetRemaining())
                {
                    result.Append(".").Append(item);
                }

                return result.ToString();
            }
        }
    }
}
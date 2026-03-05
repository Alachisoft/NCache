// Portions copyright (c) 2008-2012, Lex Li
// Portions copyright (c) 2010, Wim Looman
// All rights reserved.
//   
// Redistribution and use in source and binary forms, with or without modification, are 
// permitted provided that the following conditions are met:
//   
// - Redistributions of source code must retain the above copyright notice, this list 
//   of conditions and the following disclaimer.
//   
// - Redistributions in binary form must reproduce the above copyright notice, this list
//   of conditions and the following disclaimer in the documentation and/or other materials 
//   provided with the distribution.
//   
// - Neither the name of the <ORGANIZATION> nor the names of its contributors may be used to 
//   endorse or promote products derived from this software without specific prior written 
//   permission.
//   
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS 
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY 
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR 
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER 
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Definition class.
    /// </summary>
    [DebuggerDisplay("{_module}::{_name} ({_value})")]
    internal sealed class Definition : IDefinition
    {
        private readonly uint[] _id;
        private readonly string _name;
        private readonly string _module;
        private readonly string _parent;
        private readonly uint _value;
        private readonly IDictionary<uint, IDefinition> _children = new Dictionary<uint, IDefinition>();
        private Definition _parentNode;
        private readonly string _typeString;
        
        private Definition()
        {
        }
        
        internal Definition(uint[] id, string name, string parent, string module, string typeString)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            _id = id;
            _name = name;
            _parent = parent;
            _module = module;
            _value = id[id.Length - 1];
            _typeString = typeString;
        }
        
        /// <summary>
        /// Creates a <see cref="Definition"/> instance.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="entity"></param>
        internal Definition(IEntity entity, Definition parent)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            _parentNode = parent;
            uint[] id = string.IsNullOrEmpty(parent.Name) ?
                null : parent._id; // null for root node    (use _id rather than GetNumericalForm to avoid the Clone)
            _id = ObjectIdentifier.AppendTo(id, entity.Value);
            _parent = parent.Name;
            _name = entity.Name;
            _module = entity.ModuleName;
            _value = entity.Value;
            _parentNode.Append(this);
            Type = DetermineType(entity.GetType().ToString(), _name, _parentNode);

            Entity = entity;
        }

        internal void DetermineType(IDefinition parent)
        {
            Type = DetermineType(_typeString, _name, parent);
        }
        
        private static DefinitionType DetermineType(string type, string name, IDefinition parent)
        {
            if (type == typeof(ObjectIdentifierType).ToString()) 
            {
                return DefinitionType.OidValueAssignment;
            }

            if (type != typeof(ObjectTypeMacro).ToString())
            {
                return DefinitionType.Unknown;
            }

            if (name.EndsWith("Table", StringComparison.Ordinal))
            {
                return DefinitionType.Table;
            }

            if (name.EndsWith("Entry", StringComparison.Ordinal)) 
            {
                return DefinitionType.Entry;
            }

            return parent.Type == DefinitionType.Entry ? DefinitionType.Column : DefinitionType.Scalar;
        }
        
        /// <summary>
        /// Value.
        /// </summary>
        public uint Value
        {
            get { return _value; }
            set { }
        }

        public bool Validate(List<IConstruct> knownConstructs, string fileName)
        {
            throw new NotImplementedException();
        }

        public string Parent
        {
            get { return _parent; }
            set { }
        }
        
        public IDefinition ParentDefinition
        {
            get
            {
                return _parentNode;
            }

            internal set
            {
                _parentNode = (Definition)value;
                if (_parentNode != null)
                {
                    _parentNode.Append(this);
                }
            }
        }

        /// <summary>
        /// Children definitions.
        /// </summary>
        public IEnumerable<IDefinition> Children
        {
            get { return _children.Values; }
        }

        public DefinitionType Type { get; private set; }

        internal static Definition RootDefinition
        {
            get { return new Definition(); }
        }
        
        /// <summary>
        /// Returns the textual form.
        /// </summary>
        internal string TextualForm
        {
            get { return _module + "::" + _name; }
        }

        /// <summary>
        /// Indexer.
        /// </summary>
        public IDefinition GetChildAt(uint index)
        {
            // Since this method is very often used (when parsing OID, and so when displaying a MIB tree),
            // we avoid to call d.GetNumericalForm (which clones the uint[] of the OID) but cast 'd' as a Definition
            // and then call directly it _id field (without modifying it of course).
            // Assume all IDefinition are Definition
            return (from Definition d in _children.Values let id = d._id where id[id.Length - 1] == index select d).FirstOrDefault();
        }

        /// <summary>
        /// Module name.
        /// </summary>
        public string ModuleName
        {
            get { return _module; }
            set { }
        }
        
        /// <summary>
        /// Name.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { }
        }

        /// <summary>
        /// Gets the numerical form.
        /// </summary>
        /// <returns></returns>
        public uint[] GetNumericalForm()
        {
            return (uint[])_id.Clone();
        }
        
        /// <summary>
        /// Add an <see cref="IEntity"/> node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Definition Add(IEntity node)
        {
            // * algorithm 1: recursive
            if (_name == node.Parent)
            {
                return new Definition(node, this);
            }

            return (from Definition d in _children.Values select d.Add(node)).FirstOrDefault(result => result != null);

            // */

            /* algorithm 2: put parent locating task outside. slower, so dropped.
            if (_name != node.Parent)
            {
                throw new ArgumentException("this node is not child of mine", "node");
            }
            return new Definition(node, this);
            // */
        }
        
        /// <summary>
        /// Adds a <see cref="Definition"/> child to this <see cref="Definition"/>.
        /// </summary>
        /// <param name="def"></param>
        private void Append(IDefinition def)
        {
            if (!_children.ContainsKey(def.Value))
            {
                _children.Add(def.Value, def);
            }
        }
        
        internal static uint[] GetParent(IDefinition definition)    // Assume all IDefinition are Definition
        {
            uint[] self = ((Definition)definition)._id;        // use _id rather than GetNumericalForm to avoid the Clone.
            uint[] result = new uint[self.Length - 1];
            Array.Copy(self, result, self.Length - 1);            
            return result;
        }
        
        public string Description
        {
            // TODO: implement this.
            get { return string.Empty; }
        }

        public IEntity Entity { get; private set; }
    }
}

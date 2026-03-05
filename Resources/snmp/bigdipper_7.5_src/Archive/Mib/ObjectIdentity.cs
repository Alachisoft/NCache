using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Object identifier node.
    /// </summary>
    internal sealed class ObjectIdentity : IEntity
    {
        private readonly string _module;
        private readonly string _name;
        private string _parent;
        private readonly uint _value;
        
        /// <summary>
        /// Creates a <see cref="ObjectIdentity"/>.
        /// </summary>
        /// <param name="module">Module name</param>
        /// <param name="header">Header</param>
        /// <param name="lexer">Lexer</param>
        public ObjectIdentity(string module, IList<Symbol> header, Lexer lexer)
        {
            _module = module;
            _name = header[0].ToString();
            lexer.ParseOidValue(out _parent, out _value);
            if (_parent == "0")
            {
                _parent = "ccitt";
            }
        }

        /// <summary>
        /// Module name.
        /// </summary>
        public string ModuleName
        {
            get
            {
                return _module;
            }
        }
        
        /// <summary>
        /// Name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        
        /// <summary>
        /// Parent name.
        /// </summary>
        public string Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }
        
        /// <summary>
        /// Value.
        /// </summary>
        public uint Value
        {
            get
            {
                return _value;
            }
        }
        
        public string Description
        {
            // TODO: implement this.
            get { return string.Empty; }
        }
    }
}
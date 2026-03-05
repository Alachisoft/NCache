/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/17
 * Time: 20:49
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Object identifier node.
    /// </summary>
    internal sealed class OidValueAssignment : IEntity
    {
        private readonly string _module;
        private readonly string _name;
        private string _parent;
        private readonly uint _value;
        
        /// <summary>
        /// Creates an <see cref="OidValueAssignment"/>.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <param name="value"></param>
        public OidValueAssignment(string module, string name, string parent, uint value)
        {
            _module = module;
            _name = name;
            _parent = parent;
            _value = value;
        }
        
        /// <summary>
        /// Creates a <see cref="OidValueAssignment"/>.
        /// </summary>
        /// <param name="module">Module name</param>
        /// <param name="name">Name</param>
        /// <param name="lexer">Lexer</param>
        public OidValueAssignment(string module, string name, Lexer lexer)
        {            
            _module = module;
            _name = name;
            lexer.ParseOidValue(out _parent, out _value);
        }
        
        /// <summary>
        /// Module name.
        /// </summary>
        public string ModuleName
        {
            get { return _module; }
        }
        
        /// <summary>
        /// Name.
        /// </summary>
        public string Name
        {
            get { return _name; }
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
            get { return _value; }
        }
        
        public string Description
        {
            // TODO: implement this.
            get { return string.Empty; }
        }
    }
}
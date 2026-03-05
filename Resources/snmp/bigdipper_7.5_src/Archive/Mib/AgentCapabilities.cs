/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/31
 * Time: 13:18
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// The AGENT-CAPABILITIES construct is used to specify implementation characteristics of an SNMP agent sub-system with respect to object types and events.
    /// </summary>
    internal sealed class AgentCapabilities : IEntity
    {
        private readonly string _module;
        private readonly string _name;
        private string _parent;
        private readonly uint _value;
        
        /// <summary>
        /// Creates an <see cref="AgentCapabilities"/> instance.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="header"></param>
        /// <param name="lexer"></param>
        public AgentCapabilities(string module, IList<Symbol> header, Lexer lexer)
        {
            _module = module;
            _name = header[0].ToString();
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
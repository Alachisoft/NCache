/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/17
 * Time: 17:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// MIB document.
    /// </summary>
    internal sealed class MibDocument
    {
        /// <summary>
        /// Creates a <see cref="MibDocument"/> instance.
        /// </summary>
        /// <param name="lexer"></param>
        public MibDocument(Lexer lexer)
        {
            Symbol temp;
            while ((temp = lexer.GetNextNonEOLSymbol()) != null)
            {
                temp.ValidateIdentifier();
                _modules.Add(new MibModule(temp.ToString(), lexer));                
            }
        }
        
        private readonly IList<IModule> _modules = new List<IModule>();
        
        /// <summary>
        /// <see cref="MibModule"/> containing in this document.
        /// </summary>
        public IList<IModule> Modules
        {
            get
            {
                return _modules;
            }
        }
    }
}
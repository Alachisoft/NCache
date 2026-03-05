/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/31
 * Time: 12:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    internal sealed class ImportsFrom
    {
        private readonly string _module;
        private readonly IList<string> _types = new List<string>();
        
        public ImportsFrom(Symbol last, Lexer lexer)
        {
            Symbol previous = last;
            Symbol temp;
            while ((temp = lexer.GetNextSymbol()) != Symbol.From)
            {
                if (temp == Symbol.EOL) 
                {
                    continue;
                }
                
                if (temp == Symbol.Comma)
                {
                    previous.ValidateIdentifier();
                    _types.Add(previous.ToString());
                }
                
                previous = temp;
            }
            
            _module = lexer.GetNextSymbol().ToString().ToUpperInvariant(); // module names are uppercase
        }
        
        public string Module
        {
            get { return _module; }
        }
    }
}
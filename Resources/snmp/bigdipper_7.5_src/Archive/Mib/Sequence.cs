/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/21
 * Time: 19:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Generic;
namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// The SEQUENCE type represents a list of specified types. This is roughtly analogous to a <code>struct</code> in C.
    /// </summary>
    internal sealed class Sequence : ITypeAssignment
    {
        private string _name;

        /// <summary>
        /// Creates a <see cref="Sequence"/> instance.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="lexer"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "module")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name")]
        public Sequence(string module, string name, Lexer lexer)
        {
            _name = name;
            // parse between ( )
            Symbol temp = lexer.GetNextNonEOLSymbol(); 
            int bracketSection = 0;
            temp.Expect(Symbol.OpenBracket);
            bracketSection++;
            while (bracketSection > 0)
            {
                temp = lexer.GetNextNonEOLSymbol();
                if (temp == Symbol.OpenBracket)
                {
                    bracketSection++;
                }
                else if (temp == Symbol.CloseBracket)
                {
                    bracketSection--;
                }
            }
        }

        public Sequence(string module, string name, IEnumerator<Symbol> enumerator)
        {
            // parse between ( )
            Symbol temp = enumerator.NextNonEOLSymbol();
            int bracketSection = 0;
            temp.Expect(Symbol.OpenBracket);
            bracketSection++;
            while (bracketSection > 0)
            {
                temp = enumerator.NextNonEOLSymbol();
                if (temp == Symbol.OpenBracket)
                {
                    bracketSection++;
                }
                else if (temp == Symbol.CloseBracket)
                {
                    bracketSection--;
                }
            }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}
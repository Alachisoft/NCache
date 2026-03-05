/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/31
 * Time: 11:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// The CHOICE type represents a list of alternatives..
    /// </summary>
    internal sealed class Choice : ITypeAssignment
    {
        private string _name;

        /// <summary>
        /// Creates a <see cref="Choice"/> instance.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="lexer"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "module")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "temp")]
        public Choice(string module, string name, Lexer lexer)
        {
            _name = name;

            Symbol temp;
            while ((temp = lexer.GetNextSymbol()) != Symbol.OpenBracket)
            {
            }
            
            while ((temp = lexer.GetNextSymbol()) != Symbol.CloseBracket)
            {
            }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}
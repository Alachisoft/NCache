using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    internal sealed class Macro : ITypeAssignment
    {
        private string _name;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "temp")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "module")]
        public Macro(string module, IList<Symbol> header, Lexer lexer)
        {
            _name = header[0].ToString();
            Symbol temp;
            while ((temp = lexer.GetNextSymbol()) != Symbol.Begin)
            {                
            }
            
            while ((temp = lexer.GetNextSymbol()) != Symbol.End)
            {
            }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}
using System.Collections.Generic;
using System.Text;

namespace Lextm.SharpSnmpLib.Mib
{
    internal class CharBuffer
    {
        private readonly StringBuilder _builder = new StringBuilder();

        public void Fill(IList<Symbol> symbols, string file, int row, int column)
        {
            if (_builder.Length == 0)
            {
                return;
            }

            string content = _builder.ToString();
            _builder.Length = 0;
            symbols.Add(new Symbol(file, content, row, column));
        }

        public void Append(char current)
        {
            _builder.Append(current);
        }
    }
}
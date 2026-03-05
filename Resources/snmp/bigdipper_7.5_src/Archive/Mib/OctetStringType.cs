using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    internal class OctetStringType : TypeAssignmentBase
    {
        private string _module;
        private string _name;
        private IList<ValueRange> _size;

        public OctetStringType(string module, string name, Lexer lexer)
        {
            _module = module;
            _name = name;
            _size = new List<ValueRange>();

            Symbol temp = lexer.GetNextSymbol();
            if (temp == Symbol.OpenParentheses)
            {
                _size = DecodeRanges(lexer);
            }

        }

        public OctetStringType(string module, string name, IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            _module = module;
            _name = name;
            _size = new List<ValueRange>();

            temp = enumerator.NextSymbol();
            if (temp == Symbol.OpenParentheses)
            {
                _size = DecodeRanges(enumerator);
                temp = enumerator.NextNonEOLSymbol();
            }
        }

        public override string Name
        {
            get { return _name; }
        }

        public bool Contains(int p)
        {
            foreach (ValueRange range in _size)
            {
                if (range.Contains(p))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

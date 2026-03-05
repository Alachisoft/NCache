using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    internal class BitsType : TypeAssignmentBase
    {
        private string _module;
        private string _name;
        private IDictionary<int, string> _map;

        public BitsType(string module, string name, Lexer lexer)
        {
            _module = module;
            _name = name;
            lexer.GetNextNonEOLSymbol().Expect(Symbol.OpenBracket);
            _map = DecodeEnumerations(lexer);
        }

        public BitsType(string module, string name, IEnumerator<Symbol> enumerator)
        {
            _module = module;
            _name = name;
            enumerator.NextNonEOLSymbol().Expect(Symbol.OpenBracket);
            _map = DecodeEnumerations(enumerator);
        }

        public string this[int identifier]
        {
            get
            {
                return _map[identifier];
            }
        }

        public override string Name
        {
            get { return _name; }
        }
    }
}

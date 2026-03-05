using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    class Counter64Type : ITypeAssignment
    {
        private string _module;
        private string _name;

        public Counter64Type(string module, string name, Lexer lexer)
        {
            _module = module;
            _name = name;
        }

        public Counter64Type(string module, string name, IEnumerator<Symbol> enumerator)
        {
            _module = module;
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }
    }
}

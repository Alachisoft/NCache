using System.Collections.Generic;
using System.Linq;

namespace Lextm.SharpSnmpLib.Mib
{
    /**
     * As this type is used for Counter32 and TimeTicks as well as Unsigned32
     * and Gauge32 it incorrectly allows range restrictions of Counter32 and
     * TimeTicks.  This is ok as currently we do not care about detecting
     * incorrect MIBs and this doesn't block the decoding of correct MIBs.
     */
    class UnsignedType : TypeAssignmentBase
    {
        private string _module;
        private string _name;
        private IList<ValueRange> _ranges;

        public UnsignedType(string module, string name, Lexer lexer)
        {
            _module = module;
            _name = name;

            Symbol temp = lexer.CheckNextNonEOLSymbol();
            if (temp == Symbol.OpenParentheses)
            {
                lexer.GetNextNonEOLSymbol();
                _ranges = DecodeRanges(lexer);
            }
        }

        public UnsignedType(string module, string name, IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            _module = module;
            _name = name;

            temp = enumerator.NextNonEOLSymbol();
            if (temp == Symbol.OpenParentheses)
            {
                _ranges = DecodeRanges(enumerator);
                temp = enumerator.NextNonEOLSymbol();
            }
        }

        public bool Contains(int value)
        {
            return _ranges.Any(range => range.Contains(value));
        }

        public override string Name
        {
            get { return _name; }
        }
    }
}

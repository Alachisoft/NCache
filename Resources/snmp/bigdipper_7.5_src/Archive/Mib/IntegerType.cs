/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/7/25
 * Time: 20:41
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Generic;
using System.Linq;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// The INTEGER type represents a list of alternatives, or a range of numbers..
    /// Includes Integer32 as it's indistinguishable from INTEGER.
    /// </summary>
    /**
     * As this type is used for Integer32 as well as INTEGER it incorrectly
     * allows enumeration sub-typing of Integer32.  This is ok as currently we
     * do not care about detecting incorrect MIBs and this doesn't block the
     * decoding of correct MIBs.
     */
    internal sealed class IntegerType : TypeAssignmentBase
    {
        private bool _isEnumeration;
        private IDictionary<int, string> _map;
        private IList<ValueRange> _ranges;
        private string _name;

        /// <summary>
        /// Creates an <see cref="IntegerType"/> instance.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="name"></param>
        /// <param name="lexer"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "module")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name")]
        public IntegerType(string module, string name, Lexer lexer)
        {
            _name = name;

            Symbol temp = lexer.CheckNextNonEOLSymbol();
            if (temp == Symbol.OpenBracket)
            {
                lexer.GetNextNonEOLSymbol();
                _isEnumeration = true;
                _map = DecodeEnumerations(lexer);
            }
            else if (temp == Symbol.OpenParentheses)
            {
                lexer.GetNextNonEOLSymbol();
                _isEnumeration = false;
                _ranges = DecodeRanges(lexer);
            }
        }

        /// <summary>
        /// Creates an <see cref="IntegerType"/> instance.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="name">The name.</param>
        /// <param name="enumerator">The enumerator.</param>
        /// <param name="temp">The temp.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "module")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name")]
        public IntegerType(string module, string name, IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            _name = name;
            temp = enumerator.NextNonEOLSymbol();
            if (temp == Symbol.OpenBracket)
            {
                _isEnumeration = true;
                _map = DecodeEnumerations(enumerator);
                temp = enumerator.NextNonEOLSymbol();
            }
            else if (temp == Symbol.OpenParentheses)
            {
                _isEnumeration = false;
                _ranges = DecodeRanges(enumerator);
                temp = enumerator.NextNonEOLSymbol();
            }
        }

        public override string Name
        {
            get { return _name; }
        }

        public bool IsEnumeration
        {
            get
            {
                return _isEnumeration;
            }
        }

        public string this[int identifier]
        {
            get
            {
                return _isEnumeration ? _map[identifier] : null;
            }
        }

        public bool Contains(int value)
        {
            return _ranges.Any(range => range.Contains(value));
        }
    }
}
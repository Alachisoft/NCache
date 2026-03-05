using System;
using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Mib
{
    internal static class Extensions
    {
        /// <summary>
        /// Verifies if this symbol is a comment.
        /// </summary>
        internal static bool IsComment(this Symbol symbol)
        {
            return symbol != null && symbol.ToString().StartsWith("--", StringComparison.Ordinal);
        }

        public static Symbol NextSymbol(this IEnumerator<Symbol> enumerator)
        {
            return enumerator.MoveNext() ? enumerator.Current : null;
        }

        internal static Symbol NextNonEOLSymbol(this IEnumerator<Symbol> enumerator)
        {
            bool continuing = enumerator.MoveNext();
            while (continuing && (enumerator.Current == null || enumerator.Current == Symbol.EOL))
            {
                continuing = enumerator.MoveNext();
            }

            return continuing ? enumerator.Current : null;
        }

        internal static Symbol Next(this object o)
        {
            var lexer = o as Lexer;
            if (lexer != null)
            {
                return lexer.GetNextNonEOLSymbol();
            }

            var enumerator = o as IEnumerator<Symbol>;
            if (enumerator != null)
            {
                return enumerator.NextNonEOLSymbol();
            }

            throw new ArgumentException("Argument must be of type Lexer or IEnumerable<Symbol>: " + o.GetType());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lextm.SharpSnmpLib.Mib
{
    abstract class TypeAssignmentBase : ITypeAssignment
    {
        protected static IList<ValueRange> DecodeRanges(object enumerator)
        {
            Symbol temp = null;
            var ranges = new List<ValueRange>();
            bool size = false;
            while (temp != Symbol.CloseParentheses)
            {
                Symbol value1 = enumerator.Next();
                Symbol value2 = null;

                if (value1 == Symbol.Size)
                {
                    size = true;
                    enumerator.Next().Expect(Symbol.OpenParentheses);
                    continue;
                }

                temp = enumerator.Next();
                if (temp == Symbol.DoubleDot)
                {
                    value2 = enumerator.Next();
                    temp = enumerator.Next();
                }

                var range = new ValueRange(value1, value2);
                if (size)
                {
                    value1.Assert(range.Start >= 0, "invalid sub-typing; size must be greater than 0");
                }

                value1.Assert(!Contains(range.Start, ranges), "invalid sub-typing");
                if (value2 != null)
                {
                    value2.Assert(!Contains(range.End, ranges), "invalid sub-typing");
                }

                foreach (ValueRange other in ranges)
                {
                    value1.Assert(!range.Contains(other.Start), "invalid sub-typing");
                    if (other.End != null)
                    {
                        value1.Assert(!range.Contains((int)other.End), "invalid sub-typing");
                    }
                }

                ranges.Add(range);
            }

            if (size)
            {
                enumerator.Next().Expect(Symbol.CloseParentheses);
            }
            return ranges;
        }

        protected static IDictionary<int, string> DecodeEnumerations(object enumerator)
        {
            var map = new Dictionary<int, string>();

            Symbol last = enumerator.Next();
            while (true)
            {
                ParseOneItem(last, enumerator, map);
                last = enumerator.Next();
                if (last == Symbol.Comma)
                {
                    last = enumerator.Next();
                    if (last == Symbol.CloseBracket)
                    {
                        break;
                    }

                    continue;
                }

                if (last == Symbol.CloseBracket)
                {
                    break;
                }

                last.Throw("invalid enumeration section");
            } 

            return map;
        }

        private static void ParseOneItem(Symbol last, object enumerator, Dictionary<int, string> map)
        {
            string identifier = last.ToString();
            enumerator.Next().Expect(Symbol.OpenParentheses);
            Symbol value = enumerator.Next();
            int signedNumber;
            if (int.TryParse(value.ToString(), out signedNumber))
            {
                try
                {
                    // Have to include the number as it seems repeated identifiers are allowed ??
                    map.Add(signedNumber, String.Format("{0}({1})", identifier, signedNumber));
                }
                catch (ArgumentException ex)
                {
                    value.Throw(ex.Message);
                }
            }
            else
            {
                // Need to get "DefinedValue".
            }

            enumerator.Next().Expect(Symbol.CloseParentheses);
        }

        private static bool Contains(long? value, IEnumerable<ValueRange> ranges)
        {
            if (!value.HasValue)
            {
                return false;
            }

            long v = value.Value;
            return ranges.Any(range => range.Contains(v));
        }

        public abstract string Name { get; }
    }
}

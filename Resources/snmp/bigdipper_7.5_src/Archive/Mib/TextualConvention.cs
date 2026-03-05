using System;

namespace Lextm.SharpSnmpLib.Mib
{
    internal sealed class TextualConvention : ITypeAssignment
    {
        private readonly string _name;
        private readonly DisplayHint _displayHint;
        private readonly Status _status;
        private readonly string _description;
        private readonly string _reference;
        private readonly ITypeAssignment _syntax;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "module")]
        public TextualConvention(string module, string name, Lexer lexer)
        {
            _name = name;

            Symbol temp = lexer.GetNextNonEOLSymbol();

            if (temp == Symbol.DisplayHint)
            {
                // TODO: this needs decoding to a useful format.
                _displayHint = new DisplayHint(lexer.GetNextNonEOLSymbol().ToString().Trim(new[] { '"' }));
                temp = lexer.GetNextNonEOLSymbol();
            }

            temp.Expect(Symbol.Status);
            try
            {
                _status = StatusHelper.CreateStatus(lexer.GetNextNonEOLSymbol().ToString());
                temp = lexer.GetNextNonEOLSymbol();
            }
            catch (ArgumentException)
            {
                temp.Throw("Invalid status");
            }

            temp.Expect(Symbol.Description);
            _description = lexer.GetNextNonEOLSymbol().ToString().Trim(new[] { '"' });
            temp = lexer.GetNextNonEOLSymbol();

            if (temp == Symbol.Reference)
            {
                _reference = lexer.GetNextNonEOLSymbol().ToString();
                temp = lexer.GetNextNonEOLSymbol();
            }

            temp.Expect(Symbol.Syntax);

            /* 
             * RFC2579 definition:
             *       Syntax ::=   -- Must be one of the following:
             *                    -- a base type (or its refinement), or
             *                    -- a BITS pseudo-type
             *               type
             *             | "BITS" "{" NamedBits "}"
             *
             * From section 3.5:
             *      The data structure must be one of the alternatives defined
             *      in the ObjectSyntax CHOICE or the BITS construct.  Note
             *      that this means that the SYNTAX clause of a Textual
             *      Convention can not refer to a previously defined Textual
             *      Convention.
             *      
             *      The SYNTAX clause of a TEXTUAL CONVENTION macro may be
             *      sub-typed in the same way as the SYNTAX clause of an
             *      OBJECT-TYPE macro.
             * 
             * Therefore the possible values are (grouped by underlying type):
             *      INTEGER, Integer32
             *      OCTET STRING, Opaque
             *      OBJECT IDENTIFIER
             *      IpAddress
             *      Counter64
             *      Unsigned32, Counter32, Gauge32, TimeTicks
             *      BITS
             * With appropriate sub-typing.
             */

            temp = lexer.GetNextNonEOLSymbol();
            if (temp == Symbol.Bits)
            {
                _syntax = new BitsType(module, string.Empty, lexer);
            }
            else if (temp == Symbol.Integer || temp == Symbol.Integer32)
            {
                _syntax = new IntegerType(module, string.Empty, lexer);
            }
            else if (temp == Symbol.Octet)
            {
                temp = lexer.GetNextSymbol();
                temp.Expect(Symbol.String);
                _syntax = new OctetStringType(module, string.Empty, lexer);
            }
            else if (temp == Symbol.Opaque)
            {
                _syntax = new OctetStringType(module, string.Empty, lexer);
            }
            else if (temp == Symbol.IpAddress)
            {
                _syntax = new IpAddressType(module, string.Empty, lexer);
            }
            else if (temp == Symbol.Counter64)
            {
                _syntax = new Counter64Type(module, string.Empty, lexer);
            }
            else if (temp == Symbol.Unsigned32 || temp == Symbol.Counter32 || temp == Symbol.Gauge32 || temp == Symbol.TimeTicks)
            {
                _syntax = new UnsignedType(module, string.Empty, lexer);
            }
            else if (temp == Symbol.Object)
            {
                temp = lexer.GetNextSymbol();
                temp.Expect(Symbol.Identifier);
                _syntax = new ObjectIdentifierType(module, string.Empty, lexer);
            }
            else
            {
                //temp.Throw("illegal syntax for textual convention");
                _syntax = new CustomType(module, string.Empty, lexer);
            }
        }

        public string Name
        {
            get { return _name; }
        }

        public string DisplayHint
        {
            get { return _displayHint == null ? null : _displayHint.ToString(); }
        }

        public Status Status
        {
            get { return _status; }
        }

        public string Description
        {
            get { return _description; }
        }

        public string Reference
        {
            get { return _reference; }
        }

        public ITypeAssignment Syntax
        {
            get { return _syntax; }
        }

        internal object Decode(Variable v)
        {
            if (_syntax is IntegerType)
            {
                var i = v.Data as Integer32;
                if (i == null || (_syntax as IntegerType).IsEnumeration)
                {
                    return null;
                }

                return _displayHint != null ? _displayHint.Decode(i.ToInt32()) : i.ToInt32();
            }

            if (_syntax is UnsignedType)
            {
                var i = v.Data as Integer32;
                if (i == null)
                {
                    return null;
                }

                return _displayHint != null ? _displayHint.Decode(i.ToInt32()) : i.ToInt32();
            }

            if (_syntax is OctetStringType)
            {
                var o = v.Data as OctetString;
                if (o == null)
                {
                    return null;
                }

                // TODO: Follow the format specifier for octet strings.
                return null;
            }

            return null;
        }
    }
}
using System.Collections.Generic;
using System;

namespace Lextm.SharpSnmpLib.Mib
{
    internal sealed class ObjectType : IEntity
    {
        private readonly string _module;
        private string _parent;
        private readonly uint _value;
        private readonly string _name;
        private ITypeAssignment _syntax;
        private string _units;
        private MaxAccess _access;
        private Status _status;
        private string _description;
        private string _reference;
        private IList<string> _indices;
        private string _augment;
        private Symbol _defVal;


        public ObjectType(string module, IList<Symbol> header, Lexer lexer)
        {
            _module = module;
            _name = header[0].ToString();
            ParseProperties(header);
            lexer.ParseOidValue(out _parent, out _value);
        }

        private void ParseProperties(IEnumerable<Symbol> header)
        {
            IEnumerator<Symbol> enumerator = header.GetEnumerator();
            Symbol temp = enumerator.NextNonEOLSymbol();

            // Skip name
            temp = enumerator.NextNonEOLSymbol();

            temp.Expect(Symbol.ObjectType);
            temp = enumerator.NextNonEOLSymbol();

            _syntax         = ParseSyntax       (_module, _name, enumerator, ref temp);
            _units          = ParseUnits        (enumerator, ref temp);
            _access         = ParseAccess       (enumerator, ref temp);
            _status         = ParseStatus       (enumerator, ref temp);
            _description    = ParseDescription  (enumerator, ref temp);
            _reference      = ParseReference    (enumerator, ref temp);
            _indices        = ParseIndices      (enumerator, ref temp);
            _augment        = ParseAugments     (enumerator, ref temp);
            _defVal         = ParseDefVal       (enumerator, ref temp);
        }

        private static string ParseAugments(IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            string augment = null;
            if (temp == Symbol.Augments)
            {
                temp = enumerator.NextNonEOLSymbol();

                temp.Expect(Symbol.OpenBracket);
                temp = enumerator.NextNonEOLSymbol();

                augment = temp.ToString();
                temp = enumerator.NextNonEOLSymbol();

                temp.Expect(Symbol.CloseBracket);
                temp = enumerator.NextNonEOLSymbol();
            }
            return augment;
        }

        private static Symbol ParseDefVal(IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            Symbol defVal = null;
            if (temp == Symbol.DefVal)
            {
                temp = enumerator.NextNonEOLSymbol();

                temp.Expect(Symbol.OpenBracket);
                temp = enumerator.NextNonEOLSymbol();

                if (temp == Symbol.OpenBracket)
                {
                    var depth = 1;
                    // TODO: decode this.
                    while (depth > 0)
                    {
                        temp = enumerator.NextNonEOLSymbol();
                        if (temp == Symbol.OpenBracket)
                        {
                            depth++;
                        }
                        else if (temp == Symbol.CloseBracket)
                        {
                            depth--;
                        }

                    }
                }
                else
                {
                    defVal = temp;
                    temp = enumerator.NextNonEOLSymbol();
                }
            }
            return defVal;
        }

        private static IList<string> ParseIndices(IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            IList<string> indices = null;
            if (temp == Symbol.Index)
            {
                temp = enumerator.NextNonEOLSymbol();

                indices = new List<string>();

                while (temp != Symbol.CloseBracket)
                {
                    if (temp != Symbol.Comma)
                    {
                        indices.Add(temp.ToString());
                    }
                    temp = enumerator.NextNonEOLSymbol();
                }

                temp = enumerator.NextNonEOLSymbol();
            }
            return indices;
        }

        private static string ParseReference(IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            string reference = null;
            if (temp == Symbol.Reference)
            {
                temp = enumerator.NextNonEOLSymbol();

                reference = temp.ToString();
                temp = enumerator.NextNonEOLSymbol();
            }
            return reference;
        }

        private static string ParseDescription(IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            string description = null;
            if (temp == Symbol.Description)
            {
                temp = enumerator.NextNonEOLSymbol();

                description = temp.ToString().Trim(new[] { '"' });
                temp = enumerator.NextNonEOLSymbol();
            }
            return description;
        }

        private static Status ParseStatus(IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            Status status = Status.Obsolete;

            temp.Expect(Symbol.Status);
            temp = enumerator.NextNonEOLSymbol();

            try
            {
                status = StatusHelper.CreateStatus(temp.ToString());
                temp = enumerator.NextNonEOLSymbol();
            }
            catch (ArgumentException)
            {
                temp.Throw("Invalid status");
            }
            return status;
        }

        private static MaxAccess ParseAccess(IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            MaxAccess access = MaxAccess.NotAccessible;

            if (temp == Symbol.MaxAccess || temp == Symbol.Access)
            {
                temp = enumerator.NextNonEOLSymbol();

                switch (temp.ToString())
                {
                    case "not-accessible":
                        access = MaxAccess.NotAccessible;
                        break;
                    case "accessible-for-notify":
                        access = MaxAccess.AccessibleForNotify;
                        break;
                    case "read-only":
                        access = MaxAccess.ReadOnly;
                        break;
                    case "read-write":
                        access = MaxAccess.ReadWrite;
                        break;
                    case "read-create":
                        access = MaxAccess.ReadCreate;
                        break;
                    case "write-only":
                        access = MaxAccess.ReadWrite;
                        break;
                    default:
                        temp.Throw("Invalid access");
                        break;
                }
            }
            else
            {
                temp.Throw("missing access");
            }

            temp = enumerator.NextNonEOLSymbol();
            return access;
        }

        private static string ParseUnits(IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            string units = null;

            if (temp == Symbol.Units)
            {
                temp = enumerator.NextNonEOLSymbol();

                units = temp.ToString();
                temp = enumerator.NextNonEOLSymbol();
            }

            return units;
        }

        private static ITypeAssignment ParseSyntax(string module, string name, IEnumerator<Symbol> enumerator, ref Symbol temp)
        {
            ITypeAssignment syntax;

            temp.Expect(Symbol.Syntax);
            temp = enumerator.NextNonEOLSymbol();

            if (temp == Symbol.Bits)
            {
                syntax = new BitsType(string.Empty, string.Empty, enumerator);
                temp = enumerator.NextNonEOLSymbol();
            }
            else if (temp == Symbol.Integer || temp == Symbol.Integer32)
            {
                syntax = new IntegerType(module, name, enumerator, ref temp);
            }
            else if (temp == Symbol.Octet)
            {
                temp = enumerator.NextNonEOLSymbol();

                temp.Expect(Symbol.String);
                syntax = new OctetStringType(string.Empty, string.Empty, enumerator, ref temp);
            }
            else if (temp == Symbol.Opaque)
            {
                syntax = new OctetStringType(string.Empty, string.Empty, enumerator, ref temp);
            }
            else if (temp == Symbol.IpAddress)
            {
                syntax = new IpAddressType(string.Empty, string.Empty, enumerator);
                temp = enumerator.NextNonEOLSymbol();
            }
            else if (temp == Symbol.Counter64)
            {
                syntax = new Counter64Type(string.Empty, string.Empty, enumerator);
                temp = enumerator.NextNonEOLSymbol();
            }
            else if (temp == Symbol.Unsigned32 || temp == Symbol.Counter32 || temp == Symbol.Gauge32 || temp == Symbol.TimeTicks)
            {
                syntax = new UnsignedType(string.Empty, string.Empty, enumerator, ref temp);
            }
            else if (temp == Symbol.Object)
            {
                temp = enumerator.NextNonEOLSymbol();

                temp.Expect(Symbol.Identifier);
                syntax = new ObjectIdentifierType(string.Empty, string.Empty, enumerator);
                temp = enumerator.NextNonEOLSymbol();
            }
            else if (temp == Symbol.Sequence)
            {
                temp = enumerator.NextNonEOLSymbol();

                if (temp == Symbol.Of)
                {
                    temp = enumerator.NextNonEOLSymbol();
                    syntax = new TypeAssignment(string.Empty, string.Empty, enumerator, ref temp);
                }
                else
                {
                    syntax = new Sequence(string.Empty, string.Empty, enumerator);
                    temp = enumerator.NextNonEOLSymbol();
                }
            }
            else
            {
                syntax = new TypeAssignment(string.Empty, string.Empty, enumerator, ref temp);
            }

            return syntax;
        }

        private static bool IsProperty(Symbol sym)
        {
            string s = sym.ToString();
            return s == "SYNTAX" || s == "MAX-ACCESS" || s == "STATUS" || s == "DESCRIPTION";
        }

        public string ModuleName
        {
            get { return _module; }
        }

        public string Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public uint Value
        {
            get { return _value; }
        }

        public string Name
        {
            get { return _name; }
        }

        public string Description
        {
            get { return _description; }
        }

        public ITypeAssignment Syntax
        {
            get { return _syntax; }
            internal set { _syntax = value; }
        }
    }
}
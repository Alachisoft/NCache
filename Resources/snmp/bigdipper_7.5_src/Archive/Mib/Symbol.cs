/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/17
 * Time: 17:14
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Configuration;
using System.Globalization;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Description of Symbol.
    /// </summary>
    public sealed class Symbol : IEquatable<Symbol>
    {
        private readonly string _text;
        private readonly int _row;
        private readonly int _column;
        private readonly string _file;
        
        private Symbol(string text) : this(null, text, -1, -1)
        {
        }
        
        /// <summary>
        /// Creates a <see cref="Symbol"/>.
        /// </summary>
        /// <param name="file">File</param>
        /// <param name="text">Text</param>
        /// <param name="row">Row number</param>
        /// <param name="column">column number</param>
        public Symbol(string file, string text, int row, int column)
        {
            _file = file;
            _text = text;
            _row = row;
            _column = column;
        }
        
        /// <summary>
        /// File.
        /// </summary>
        public string File
        {
            get
            {
                return _file;
            }
        }
        
        /// <summary>
        /// Row number.
        /// </summary>
        public int Row
        {
            get
            {
                return _row;
            }
        }
        
        /// <summary>
        /// Column number.
        /// </summary>
        public int Column
        {
            get
            {
                return _column;
            }
        }        
        
        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="Symbol"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _text;
        }
        
        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Symbol"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Symbol"/>. </param>
        /// <returns><value>true</value> if the specified <see cref="Object"/> is equal to the current <see cref="Symbol"/>; otherwise, <value>false</value>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            
            return GetType() == obj.GetType() && Equals((Symbol)obj);
        }
        
        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current <see cref="Symbol"/>.</returns>
        public override int GetHashCode()
        {
            return _text.GetHashCode();
        }

        /// <summary>
        /// The equality operator.
        /// </summary>
        /// <param name="left">Left <see cref="Symbol"/> object</param>
        /// <param name="right">Right <see cref="Symbol"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are equal, <c>false</c> otherwise.</returns>
        public static bool operator ==(Symbol left, Symbol right)
        {
            return Equals(left, right);
        }
        
        /// <summary>
        /// Determines whether the specified <see cref="Symbol"/> is equal to the current <see cref="Symbol"/>.
        /// </summary>
        /// <param name="left">Left <see cref="Symbol"/> object</param>
        /// <param name="right">Right <see cref="Symbol"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are equal, <c>false</c> otherwise.</returns>
        private static bool Equals(Symbol left, Symbol right)
        {
            object l = left;
            object r = right;
            if (l == r)
            {
                return true;
            }

            if (l == null || r == null)
            {
                return false;
            }

            return left._text.Equals(right._text);
        }
        
        /// <summary>
        /// The inequality operator.
        /// </summary>
        /// <param name="left">Left <see cref="Symbol"/> object</param>
        /// <param name="right">Right <see cref="Symbol"/> object</param>
        /// <returns>
        /// Returns <c>true</c> if the values of its operands are not equal, <c>false</c> otherwise.</returns>
        public static bool operator !=(Symbol left, Symbol right)
        {
            return !(left == right);
        }

        #region IEquatable<Symbol> Members
        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><value>true</value> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <value>false</value>.
        /// </returns>
        public bool Equals(Symbol other)
        {
            return Equals(this, other);
        }

        #endregion
        
        internal static readonly Symbol Definitions = new Symbol("DEFINITIONS");        
        internal static readonly Symbol Begin = new Symbol("BEGIN");        
        internal static readonly Symbol Object = new Symbol("OBJECT");        
        internal static readonly Symbol Identifier = new Symbol("IDENTIFIER");
        internal static readonly Symbol Assign = new Symbol("::=");
        internal static readonly Symbol OpenBracket = new Symbol("{");
        internal static readonly Symbol CloseBracket = new Symbol("}");
        internal static readonly Symbol Imports = new Symbol("IMPORTS");
        internal static readonly Symbol Semicolon = new Symbol(";");
        internal static readonly Symbol From = new Symbol("FROM");
        internal static readonly Symbol ModuleIdentity = new Symbol("MODULE-IDENTITY");
        internal static readonly Symbol ObjectType = new Symbol("OBJECT-TYPE");
        internal static readonly Symbol ObjectGroup = new Symbol("OBJECT-GROUP");
        internal static readonly Symbol NotificationGroup = new Symbol("NOTIFICATION-GROUP");
        internal static readonly Symbol ModuleCompliance = new Symbol("MODULE-COMPLIANCE");
        internal static readonly Symbol Sequence = new Symbol("SEQUENCE");
        internal static readonly Symbol NotificationType = new Symbol("NOTIFICATION-TYPE");
        internal static readonly Symbol EOL = new Symbol(Environment.NewLine);
        internal static readonly Symbol ObjectIdentity = new Symbol("OBJECT-IDENTITY");
        internal static readonly Symbol End = new Symbol("END");
        internal static readonly Symbol Macro = new Symbol("MACRO");
        internal static readonly Symbol Choice = new Symbol("CHOICE");
        internal static readonly Symbol TrapType = new Symbol("TRAP-TYPE");
        internal static readonly Symbol AgentCapabilities = new Symbol("AGENT-CAPABILITIES");
        internal static readonly Symbol Comma = new Symbol(",");
        internal static readonly Symbol TextualConvention = new Symbol("TEXTUAL-CONVENTION");
        internal static readonly Symbol Syntax = new Symbol("SYNTAX");
        internal static readonly Symbol Integer = new Symbol("INTEGER");
        internal static readonly Symbol Bits = new Symbol("BITS");
        internal static readonly Symbol Octet = new Symbol("OCTET");
        internal static readonly Symbol String = new Symbol("STRING");
        internal static readonly Symbol OpenParentheses = new Symbol("(");
        internal static readonly Symbol CloseParentheses = new Symbol(")");
        internal static readonly Symbol Exports = new Symbol("EXPORTS");
        internal static readonly Symbol DisplayHint = new Symbol("DISPLAY-HINT");
        internal static readonly Symbol Status = new Symbol("STATUS");
        internal static readonly Symbol Description = new Symbol("DESCRIPTION");
        internal static readonly Symbol Reference = new Symbol("REFERENCE");
        internal static readonly Symbol DoubleDot = new Symbol("..");
        internal static readonly Symbol Integer32 = new Symbol("Integer32");
        internal static readonly Symbol IpAddress = new Symbol("IpAddress");
        internal static readonly Symbol Counter32 = new Symbol("Counter32");
        internal static readonly Symbol TimeTicks = new Symbol("TimeTicks");
        internal static readonly Symbol Opaque = new Symbol("Opaque");
        internal static readonly Symbol Counter64 = new Symbol("Counter64");
        internal static readonly Symbol Unsigned32 = new Symbol("Unsigned32");
        internal static readonly Symbol Gauge32 = new Symbol("Gauge32");
        internal static readonly Symbol Size = new Symbol("SIZE");
        internal static readonly Symbol Units = new Symbol("UNITS");
        internal static readonly Symbol MaxAccess = new Symbol("MAX-ACCESS");
        internal static readonly Symbol Access = new Symbol("ACCESS");
        internal static readonly Symbol Index = new Symbol("INDEX");
        internal static readonly Symbol Augments = new Symbol("AUGMENTS");
        internal static readonly Symbol DefVal = new Symbol("DEFVAL");
        internal static readonly Symbol Of = new Symbol("OF");

        internal void Expect(Symbol expected)
        {
            Assert(this == expected, expected + " expected");
        }

        internal void Assert(bool condition, string message)
        {
            if (condition)
            {
                return;
            }

            throw MibException.Create(message, this);
        }

        internal void Throw(string message)
        {
            Assert(false, message);
        }

        internal void ValidateIdentifier()
        {
            string message;
            bool condition = IsValidIdentifier(ToString(), out message);
            Assert(condition, message);
        }

        internal bool ValidateType()
        {      
            string message;
            return IsValidIdentifier(ToString(), out message);
        }

        private static bool IsValidIdentifier(string name, out string message)
        {
            if (UseStricterValidation && (name.Length < 1 || name.Length > 64))
            {
                message = "an identifier must consist of 1 to 64 letters, digits, and hyphens";
                return false;
            }

            if (!Char.IsLetter(name[0]))
            {
                message = "the initial character must be a letter";
                return false;
            }

            if (name.EndsWith("-", StringComparison.Ordinal))
            {
                message = "a hyphen cannot be the last character of an identifier";
                return false;
            }

            if (name.Contains("--"))
            {
                message = "a hyphen cannot be immediately followed by another hyphen in an identifier";
                return false;
            }

            if (UseStricterValidation && name.Contains("_"))
            {
                message = "underscores are not allowed in identifiers";
                return false;
            }

            // TODO: SMIv2 forbids "-" except in module names and keywords
            message = null;
            return true;
        }

        private static bool? _useStricterValidation;

        private static bool UseStricterValidation
        {
            get
            {
                if (_useStricterValidation == null)
                {
                    object setting = ConfigurationManager.AppSettings["StricterValidationEnabled"];
                    _useStricterValidation = setting != null && Convert.ToBoolean(setting.ToString(), CultureInfo.InvariantCulture);
                }

                return _useStricterValidation.Value;
            }
        }
    }
}
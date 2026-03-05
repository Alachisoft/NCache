/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/17
 * Time: 16:50
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Lexer class that parses MIB files into symbol list.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Lexer")]
    public sealed class Lexer
    {
        private readonly IList<Symbol> _symbols = new List<Symbol>();

        internal void Parse(TextReader stream)
        {
            Parse(null, stream);
        }

        /// <summary>
        /// Parses MIB file to symbol list.
        /// </summary>
        /// <param name="file">File</param>
        /// <param name="stream">File stream</param>
        public void Parse(string file, TextReader stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            
            _assignAhead = false;
            _assignSection = false;
            _stringSection = false;
            
            string line;
            int i = 0;
            while ((line = stream.ReadLine()) != null)
            {
                ParseLine(file, line, i);
                i++;
            }
        }

        private void ParseLine(string file, string line, int row)
        {
            line = line + "\n";
            int count = line.Length;
            for (int i = 0; i < count; i++)
            {
                char current = line[i];
                bool moveNext = Parse(file, current, row, i);
                if (moveNext)
                {
                    break;
                }
            }

            // IMPORTANT: comment does not span lines.
            _commentSection = false;
            _singleDashFound = false;
        }

        private int _index;

        /// <summary>
        /// Next <see cref="Symbol"/> which is not <see cref="Symbol.EOL"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "EOL")]
        public Symbol GetNextNonEOLSymbol()
        {
            Symbol result;
            while ((result = GetNextSymbol()) == Symbol.EOL)
            {
            }

            return result;
        }

        /// <summary>
        /// Next <see cref="Symbol"/>.
        /// </summary>
        public Symbol GetNextSymbol()
        {
            while (_index < _symbols.Count)
            {
                Symbol next = _symbols[_index++];
                if (next.IsComment())
                {
                    continue;
                }

                return next;
            }

            return null;
        }

        /// <summary>
        /// Checks next few <see cref="Symbols"/>, without advancing the cursor.
        /// </summary>
        /// <param name="length">Length.</param>
        /// <returns></returns>
        public IList<Symbol> CheckNextSymbols(int length)
        {
            var result = new List<Symbol>();
            for (int i = 0; i < length; i++)
            {
                result.Add(_symbols[_index + i]);
            }

            return result;
        }

        public Symbol CheckNextSymbol()
        {
            return _symbols[_index];
        }

        public Symbol CheckNextNonEOLSymbol()
        {
            int length = 0;
            while (_symbols[_index + length] == Symbol.EOL)
            {
                length++;
            }

            return _symbols[_index + length];
        }

        internal int SymbolCount
        {
            get
            {
                return _symbols.Count;
            }
        }

        private readonly CharBuffer _buffer = new CharBuffer();
        private bool _stringSection;
        private bool _assignSection;
        private bool _assignAhead;
        private bool _dotSection;
        private bool _singleDashFound;
        private bool _commentSection;

        /// <summary>
        /// Parses a list of <see cref="char"/> to <see cref="Symbol"/>.
        /// </summary>
        /// <param name="file">File</param>
        /// <param name="current">Current <see cref="char"/></param>
        /// <param name="row">Row number</param>
        /// <param name="column">Column number</param>
        /// <returns><code>true</code> if no need to process this line. Otherwise, <code>false</code> is returned.</returns>
        private bool Parse(string file, char current, int row, int column)
        {
            switch (current)
            {
                case '\n':
                    if (!_stringSection)
                    {
                        _buffer.Fill(_symbols, file, row, column);
                        _symbols.Add(CreateSpecialSymbol(file, current, row, column));
                        return false;
                    }

                    break;
                case '{':
                case '}':
                case '(':
                case ')':
                case '[':
                case ']':
                case ';':
                case ',':
                case '|':
                    if (_commentSection)
                    {
                        break;
                    }

                    if (!_stringSection)
                    {
                        _buffer.Fill(_symbols, file, row, column);
                        _symbols.Add(CreateSpecialSymbol(file, current, row, column));
                        return false;
                    }

                    break;
                case '"':
                    if (_commentSection)
                    {
                        break;
                    }

                    _stringSection = !_stringSection;
                    break;
                case '-':
                    if (_stringSection)
                    {
                        break;
                    }

                    if (!_singleDashFound)
                    {
                        _singleDashFound = true;
                        break;
                    }

                    _singleDashFound = false;
                    _commentSection = !_commentSection;
                    break;
                case '\r':
                    return false;
                default:
                    if (current == 0x1A)
                    {
                        // IMPORTANT: ignore invisible characters such as SUB.
                        return false;
                    }

                    _singleDashFound = false;
                    if (Char.IsWhiteSpace(current) && !_assignSection && !_stringSection && !_commentSection)
                    {
                        _buffer.Fill(_symbols, file, row, column);
                        return false;
                    }

                    if (_commentSection)
                    {
                        // TODO: ignore everything here in comment
                        break;
                    }

                    if (_assignAhead)
                    {
                        _assignAhead = false;
                        _buffer.Fill(_symbols, file, row, column);
                        break;
                    }

                    if (_dotSection && current != '.')
                    {
                        _buffer.Fill(_symbols, file, row, column);
                        _dotSection = false;
                    }

                    if (current == '.' && !_stringSection)
                    {
                        if (!_dotSection)
                        {
                            _buffer.Fill(_symbols, file, row, column);
                            _dotSection = true;
                        }
                    }
                    
                    if (current == ':' && !_stringSection)
                    {    
                        if (!_assignSection)
                        {
                            _buffer.Fill(_symbols, file, row, column);
                        }
                        
                        _assignSection = true;
                    }
                    
                    if (current == '=' && !_stringSection)
                    {
                        _assignSection = false; 
                        _assignAhead = true;
                    }

                    break;
            }

            _buffer.Append(current);
            return false;
        }

        private static Symbol CreateSpecialSymbol(string file, char value, int row, int column)
        {
            string str;
            switch (value)
            {
                case '\n':
                    str = Environment.NewLine;
                    break;
                case '{':
                    str = "{";
                    break;
                case '}':
                    str = "}";
                    break;
                case '(':
                    str = "(";
                    break;
                case ')':
                    str = ")";
                    break;
                case '[':
                    str = "[";
                    break;
                case ']':
                    str = "]";
                    break;
                case ';':
                    str = ";";
                    break;
                case ',':
                    str = ",";
                    break;
                case '|':
                    str = "|";
                    break;
                default:
                    throw new ArgumentException("value is not a special character");
            }

            return new Symbol(file, str, row, column);
        }

        internal void ParseOidValue(out string parent, out uint value)
        {
            parent = null;
            value = 0;
            Symbol previous = null;
            
            Symbol temp = GetNextNonEOLSymbol();
            temp.Expect(Symbol.OpenBracket);
            var longParent = new StringBuilder();
            temp = GetNextNonEOLSymbol();
            longParent.Append(temp);
            
            while ((temp = GetNextNonEOLSymbol()) != null)
            {
                if (temp == Symbol.OpenParentheses)
                {
                    longParent.Append(temp);
                    temp = GetNextNonEOLSymbol();
                    bool succeed = UInt32.TryParse(temp.ToString(), out value);
                    temp.Assert(succeed, "not a decimal");
                    longParent.Append(temp);
                    temp = GetNextNonEOLSymbol();
                    temp.Expect(Symbol.CloseParentheses);
                    longParent.Append(temp);
                    continue;
                }
                
                if (temp == Symbol.CloseBracket)
                {
                    parent = longParent.ToString();
                    return;
                }
                
                bool succeeded = UInt32.TryParse(temp.ToString(), out value);
                if (succeeded)
                {
                    // numerical way
                    while ((temp = GetNextNonEOLSymbol()) != Symbol.CloseBracket)
                    {
                        longParent.Append(".").Append(value);
                        succeeded = UInt32.TryParse(temp.ToString(), out value);
                        temp.Assert(succeeded, "not a decimal");
                    }
                    
                    temp.Expect(Symbol.CloseBracket);
                    parent = longParent.ToString();
                    return;
                }
                
                longParent.Append(".");
                longParent.Append(temp);
                temp = GetNextNonEOLSymbol();
                temp.Expect(Symbol.OpenParentheses);
                longParent.Append(temp);
                temp = GetNextNonEOLSymbol();
                succeeded = UInt32.TryParse(temp.ToString(), out value);
                temp.Assert(succeeded, "not a decimal");
                longParent.Append(temp);
                temp = GetNextNonEOLSymbol();
                temp.Expect(Symbol.CloseParentheses);
                longParent.Append(temp);
                previous = temp;
            }
            
            throw MibException.Create("end of file reached", previous);
        }
    }
}

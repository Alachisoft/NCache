// Copyright (c) 2008-2012, Lex Li
// All rights reserved.
//   
// Redistribution and use in source and binary forms, with or without modification, are 
// permitted provided that the following conditions are met:
//   
// - Redistributions of source code must retain the above copyright notice, this list 
//   of conditions and the following disclaimer.
//   
// - Redistributions in binary form must reproduce the above copyright notice, this list
//   of conditions and the following disclaimer in the documentation and/or other materials 
//   provided with the distribution.
//   
// - Neither the name of the <ORGANIZATION> nor the names of its contributors may be used to 
//   endorse or promote products derived from this software without specific prior written 
//   permission.
//   
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS 
// OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY 
// AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR 
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER 
// IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
// OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Antlr.Runtime;

namespace Lextm.SharpSnmpLib.Mib
{
    internal static class TokenExtension
    {
        internal static bool IsValidIdentifier(this IToken token, SmiParser parser)
        {
            string message;
            string name = token.Text;
            if (UseStricterValidation && (name.Length < 1 || name.Length > 64))
            {
                message = string.IsNullOrEmpty(parser.FileName)
                              ? string.Format(
                                  "warning N0006 : an identifier must consist of 1 to 64 letters, digits, and hyphens. {0}",
                                  token.Text)
                              : string.Format(
                                  "{0} ({1},{2}) : warning N0006 : an identifier must consist of 1 to 64 letters, digits, and hyphens. {3}",
                                  parser.FileName,
                                  token.Line, token.CharPositionInLine + 1, token.Text);
                parser.Warnings.Add(new CompilerWarning(token, parser.FileName, message));
                return false;
            }

            if (!Char.IsLetter(name[0]))
            {
                message = string.IsNullOrEmpty(parser.FileName)
                              ? string.Format(
                                  "warning N0007 : the initial character must be a letter. {0}", token.Text)
                              : string.Format(
                                  "{0} ({1},{2}) : warning N0007 : the initial character must be a letter. {3}",
                                  parser.FileName,
                                  token.Line, token.CharPositionInLine + 1, token.Text);
                parser.Warnings.Add(new CompilerWarning(token, parser.FileName, message));
                return false;
            }

            if (name.EndsWith("-", StringComparison.Ordinal))
            {
                message = string.IsNullOrEmpty(parser.FileName)
                              ? string.Format(
                                  "warning N0008 : a hyphen cannot be the last character of an identifier. {0}",
                                  token.Text)
                              : string.Format(
                                  "{0} ({1},{2}) : warning N0008 : a hyphen cannot be the last character of an identifier. {3}",
                                  parser.FileName,
                                  token.Line, token.CharPositionInLine + 1, token.Text);
                parser.Warnings.Add(new CompilerWarning(token, parser.FileName, message));
                return false;
            }

            if (name.Contains("--"))
            {
                message = string.IsNullOrEmpty(parser.FileName)
                              ? string.Format(
                                  "warning N0009 : a hyphen cannot be immediately followed by another hyphen in an identifier. {0}",
                                  token.Text)
                              : string.Format(
                                  "{0} ({1},{2}) : warning N0009 : a hyphen cannot be immediately followed by another hyphen in an identifier. {3}",
                                  parser.FileName,
                                  token.Line, token.CharPositionInLine + 1, token.Text);
                parser.Warnings.Add(new CompilerWarning(token, parser.FileName, message));
                return false;
            }

            if (UseStricterValidation && name.Contains("_"))
            {
                message = string.IsNullOrEmpty(parser.FileName)
                              ? string.Format(
                                  "warning N0010 : underscores are not allowed in identifiers. {0}", token.Text)
                              : string.Format(
                                  "{0} ({1},{2}) : warning N0010 : underscores are not allowed in identifiers. {3}",
                                  parser.FileName,
                                  token.Line, token.CharPositionInLine + 1, token.Text);
                parser.Warnings.Add(new CompilerWarning(token, parser.FileName, message));
                return false;
            }

            // TODO: SMIv2 forbids "-" except in module names and keywords
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

        internal static bool IsUppercase(this IToken token)
        {
            var name = token.Text;
            if (String.IsNullOrEmpty(name))
            {
                return true;
            }

            return name.All(c => !Char.IsLower(c));
        }

        // check if name is PascalCased

        internal static bool IsPascalCase(this IToken token)
        {
            var name = token.Text;
            return String.IsNullOrEmpty(name) || Char.IsUpper(name[0]);
        }

        // convert name to PascalCase

        internal static string PascalCase(this string name)
        {
            if (String.IsNullOrEmpty(name))
                return String.Empty;

            if (name.Length == 1)
                return name.ToUpperInvariant();

            int index = IndexOfFirstCorrectChar(name);
            return Char.ToUpperInvariant(name[index]).ToString(CultureInfo.InvariantCulture) + name.Substring(index + 1);
        }

        // check if name is camelCased

        internal static bool IsCamelCase(this IToken token)
        {
            var name = token.Text;
            if (String.IsNullOrEmpty(name))
                return true;

            return Char.IsLower(name[0]);
        }

        // convert name to camelCase

        internal static string CamelCase(this string name)
        {
            if (String.IsNullOrEmpty(name))
                return String.Empty;

            if (name.Length == 1)
                return name.ToLowerInvariant();

            int index = IndexOfFirstCorrectChar(name);
            return Char.ToLowerInvariant(name[index]).ToString(CultureInfo.InvariantCulture) + name.Substring(index + 1);
        }

        private static int IndexOfFirstCorrectChar(string s)
        {
            int index = 0;
            while ((index < s.Length) && (s[index] == '_'))
                index++;
            // it's possible that we won't find one, e.g. something called "_"
            return (index == s.Length) ? 0 : index;
        }
    }
}
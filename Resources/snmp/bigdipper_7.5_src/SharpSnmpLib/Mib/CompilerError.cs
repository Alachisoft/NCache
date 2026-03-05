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

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/17
 * Time: 16:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Text;
using Antlr.Runtime;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Description of CompilerError.
    /// </summary>
    [Serializable]
    public class CompilerError
    {
        /// <summary>
        /// Creates a <see cref="CompilerError"/> instance with a specific <see cref="Exception"/>.
        /// </summary>
        /// <param name="exception">Compiler exception.</param>
        public CompilerError(RecognitionException exception)
        {
            Token = exception.Token;
            var result = new StringBuilder();
            if (!string.IsNullOrEmpty(FileName))
            {
                result.AppendFormat("{0} ({1},{2}) : ", FileName, Token.Line, Token.CharPositionInLine + 1);
            }

            result.AppendFormat("error S0001 : invalid token {0}", Token.Text);
            Details = result.ToString();
        }

        public CompilerError(IToken token, string fileName, string details)
        {
            Token = token;
            FileName = fileName;
            Details = details;
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents this <see cref="CompilerError"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "CompilerError: " + Details;
        }

        public string Details { get; set; }

        public string FileName { private get; set; }

        public IToken Token { get; set; }
    }
}
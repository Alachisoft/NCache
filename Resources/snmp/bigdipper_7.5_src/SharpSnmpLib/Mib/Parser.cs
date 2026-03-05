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
 * Date: 2008/10/2
 * Time: 17:32
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Antlr.Runtime;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Description of Parser.
    /// </summary>
    public sealed class Parser
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Lextm.SharpSnmpLib.Mib");

        /// <summary>
        /// Parses MIB documents to module files (*.module).
        /// </summary>
        /// <param name="files">The files.</param>
        /// <param name="errors">The errors.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public IEnumerable<IModule> ParseToModules(IEnumerable<string> files, out IEnumerable<CompilerError> errors, out IEnumerable<CompilerWarning> warnings)
        { 
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            var errorlist = new List<CompilerError>();
            var warninglist = new List<CompilerWarning>();
            var modules = new List<IModule>();
            foreach (string file in files)
            {
                modules.AddRange(Compile(file, errorlist, warninglist));
                Logger.Info(file + " compiled");
            }

            errors = errorlist;
            warnings = warninglist;
            return modules;
        }

        /// <summary>
        /// Loads a MIB file.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="errors">Errors.</param>
        public static IEnumerable<IModule> Compile(string fileName, List<CompilerError> errors, List<CompilerWarning> warnings)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (fileName.Length == 0)
            {
                throw new ArgumentException("fileName cannot be empty");
            }

            if (!File.Exists(fileName))
            {
                throw new ArgumentException("file does not exist: " + fileName);
            }

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            var lexer = new SmiLexer(new ANTLRFileStream(fileName));
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            try
            {
                var doc = parser.GetDocument(fileName);
                Logger.Info(watch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + "-ms used to parse " +
                            fileName);
                watch.Stop();
                errors.AddRange(parser.Errors);
                warnings.AddRange(parser.Warnings);
                return doc.Modules.OfType<IModule>();
            }
            catch (RecognitionException ex)
            {
                errors.Add(new CompilerError(ex) {FileName = fileName});
                errors.AddRange(parser.Errors);
                warnings.AddRange(parser.Warnings);
                return new IModule[0];
            }
        }

        /// <summary>
        /// Loads a MIB file.
        /// </summary>
        public static IEnumerable<IModule> Compile(Stream stream, List<CompilerError> errors, List<CompilerWarning> warnings)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            var lexer = new SmiLexer(new ANTLRInputStream(stream));
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            try
            {
                var doc = parser.GetDocument(string.Empty);
                Logger.Info(watch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + "-ms used to parse");
                watch.Stop();
                errors.AddRange(parser.Errors);
                warnings.AddRange(parser.Warnings);
                return doc.Modules.OfType<IModule>().ToList();
            }
            catch (RecognitionException ex)
            {
                errors.Add(new CompilerError(ex));
                errors.AddRange(parser.Errors);
                warnings.AddRange(parser.Warnings);
                return new IModule[0];
            }
        }
    }
}

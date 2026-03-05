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
        public IEnumerable<IModule> ParseToModules(IEnumerable<string> files, out IEnumerable<MibException> errors)
        { 
            if (files == null)
            {
                throw new ArgumentNullException("files");
            }

            IList<MibException> list = new List<MibException>();
            List<IModule> modules = new List<IModule>();
            foreach (string file in files)
            {
                try
                {
                    modules.AddRange(Compile(file));
                }
                catch (MibException ex)
                {
                    list.Add(ex);
                }
                finally
                {
                    Logger.Info(file + " compiled");
                }
            }
            
            errors = list;
            return modules;
        }

        /// <summary>
        /// Loads a MIB file.
        /// </summary>
        /// <param name="fileName">File name</param>
        public static IEnumerable<IModule> Compile(string fileName)
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

            return Compile(fileName, File.OpenText(fileName));
        }

        private static IList<IModule> Compile(string file, TextReader stream)
        {
            try
            {
                return CompileToModules(file, stream);
            }
            finally
            {
                stream.Close();
            }
        }

        internal static IList<IModule> Compile(TextReader stream)
        {
            return Compile(string.Empty, stream);
        }

        private static IList<IModule> CompileToModules(string file, TextReader stream)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            Lexer lexer = new Lexer();
            lexer.Parse(file, stream);
            MibDocument doc = new MibDocument(lexer);
            Logger.Info(watch.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + "-ms used to parse " + file);
            watch.Stop();
            return doc.Modules;
        }
    }
}

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2009/2/8
 * Time: 8:45
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Description of ModuleLoader.
    /// </summary>
    internal sealed class ModuleLoader
    {
        private readonly List<Definition> _nodes;
        private readonly List<string> _dependents;
        private readonly MibModule _module;
        
        public ModuleLoader(TextReader reader, string moduleName)
        {
            _nodes = new List<Definition>();
            _dependents = new List<string>();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    _dependents.AddRange(ParseDependents(line));
                    continue;
                }

                _nodes.Add(ParseLine(line, moduleName));
            }

            _module = new MibModule(moduleName, _dependents);
        }
        
        public MibModule Module
        {
            get { return _module; }
        }
        
        public IEnumerable<Definition> Nodes
        {
            get { return _nodes; }
        }

        private static IEnumerable<string> ParseDependents(string line)
        {
            return line.Substring(1).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static Definition ParseLine(string line, string module)
        {
            string[] content = line.Split(',');
            /* 0: id
             * 1: type
             * 2: name
             * 3: parent name
             */
            uint[] id = ObjectIdentifier.Convert(content[0]);
            return new Definition(id, content[2], content[3], module, content[1]);
        }
    }
}

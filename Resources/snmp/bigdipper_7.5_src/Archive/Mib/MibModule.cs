/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/17
 * Time: 17:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// MIB module class.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mib")]
    internal sealed class MibModule : IModule
    {
        private readonly string _name;
        private readonly Imports _imports;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private readonly Exports _exports;
        private readonly List<IConstruct> _tokens = new List<IConstruct>();
        private const string Pattern = "-V[0-9]+$";
        
        internal MibModule(string name, IEnumerable<string> dependents)
        {
            _name = name;
            _imports = new Imports(dependents);
        }
        
        /// <summary>
        /// Creates a <see cref="MibModule"/> with a specific <see cref="Lexer"/>.
        /// </summary>
        /// <param name="name">Module name</param>
        /// <param name="lexer">Lexer</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "lexer")]
        public MibModule(string name, Lexer lexer)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            
            if (lexer == null)
            {
                throw new ArgumentNullException("lexer");
            }
            
            _name = name.ToUpperInvariant(); // all module name are uppercase.
            Symbol temp = lexer.GetNextNonEOLSymbol();
            temp.Expect(Symbol.Definitions);
            temp = lexer.GetNextNonEOLSymbol();
            temp.Expect(Symbol.Assign);
            temp = lexer.GetNextSymbol();
            temp.Expect(Symbol.Begin);
            temp = lexer.GetNextNonEOLSymbol();
            if (temp == Symbol.Imports)
            {
                _imports = ParseDependents(lexer);
            }
            else if (temp == Symbol.Exports)
            {
                _exports = ParseExports(lexer);
            }
            else if (temp == Symbol.End)
            {
                return;
            }

            ParseEntities(_tokens, temp, _name, lexer);
        }

        /// <summary>
        /// Exports data.
        /// </summary>
        public Exports Exports
        {
            get { return _exports; }
        }

        private static Exports ParseExports(Lexer lexer)
        {
            return new Exports(lexer);
        }
        
        private static Imports ParseDependents(Lexer lexer)
        {
            return new Imports(lexer);
        }
        
        private static void ParseEntities(ICollection<IConstruct> tokens, Symbol last, string module, Lexer lexer)
        {
            Symbol temp = last; 
            IList<Symbol> buffer = new List<Symbol>();
            do
            {
                if (temp == Symbol.Imports || temp == Symbol.Exports || temp == Symbol.EOL)
                {
                    continue;
                }
                
                buffer.Add(temp);
                if (temp != Symbol.Assign)
                {
                    continue;
                }

                ParseEntity(tokens, module, buffer, lexer);
                buffer.Clear();
            }
            while ((temp = lexer.GetNextSymbol()) != Symbol.End);
        }
        
        private static void ParseEntity(ICollection<IConstruct> tokens, string module, IList<Symbol> buffer, Lexer lexer)
        {
            buffer[0].Assert(buffer.Count > 1, "unexpected symbol");
            buffer[0].ValidateIdentifier();
            if (buffer.Count == 2)
            {
                // others
                tokens.Add(ParseOthers(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.Object)
            {
                // object identifier
                tokens.Add(ParseObjectIdentifier(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.ModuleIdentity)
            {
                // module identity
                tokens.Add(new ModuleIdentity(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.ObjectType)
            {
                tokens.Add(new ObjectType(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.ObjectGroup)
            {
                tokens.Add(new ObjectGroup(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.NotificationGroup)
            {
                tokens.Add(new NotificationGroup(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.ModuleCompliance)
            {
                tokens.Add(new ModuleCompliance(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.NotificationType)
            {
                tokens.Add(new NotificationType(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.ObjectIdentity)
            {
                tokens.Add(new ObjectIdentity(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.Macro)
            {
                tokens.Add(new Macro(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.TrapType)
            {
                tokens.Add(new TrapType(module, buffer, lexer));
            }
            else if (buffer[1] == Symbol.AgentCapabilities)
            {
                tokens.Add(new AgentCapabilities(module, buffer, lexer));
            }
        }
        
        private static IEntity ParseObjectIdentifier(string module, IList<Symbol> header, Lexer lexer)
        {
            header[0].Assert(header.Count == 4, "invalid OID value assignment");
            header[2].Expect(Symbol.Identifier);
            return new OidValueAssignment(module, header[0].ToString(), lexer);
        }

        private static IConstruct ParseOthers(string module, IList<Symbol> header, Lexer lexer)
        {
            Symbol current = lexer.GetNextNonEOLSymbol();
            
            if (current == Symbol.Sequence)
            {
                return new Sequence(module, header[0].ToString(), lexer);
            }

            if (current == Symbol.Choice)
            {
                return new Choice(module, header[0].ToString(), lexer);
            }

            if (current == Symbol.Integer)
            {
                return new IntegerType(module, header[0].ToString(), lexer);
            }

            if (current == Symbol.TextualConvention)
            {
                return new TextualConvention(module, header[0].ToString(), lexer);
            }

            return new TypeAssignment(module, header[0].ToString(), current, lexer);
        }

        /// <summary>
        /// Module name.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        
        /// <summary>
        /// OID nodes.
        /// </summary>
        public IList<IEntity> Entities
        {
            get
            {
                return _tokens.OfType<IEntity>().ToList();
            }
        }
        
        /// <summary>
        /// OID nodes.
        /// </summary>
        public IList<IEntity> Objects
        {
            get
            {
                return _tokens.OfType<ObjectType>().Cast<IEntity>().ToList();
            }
        }

        public IDictionary<string, ITypeAssignment> Types
        {
            get
            {
                return _tokens.Where(token => token is ITypeAssignment)
                              .Cast<ITypeAssignment>()
                              .Where(type => !string.IsNullOrEmpty(type.Name))
                              .ToDictionary(type => type.Name);
            }
        }

        
        public IList<string> Dependents
        {
            get
            {
                return (_imports == null) ? new List<string>() : _imports.Dependents;
            }
        }
        
        internal static bool AllDependentsAvailable(MibModule module, IDictionary<string, MibModule> modules)
        {
            return module.Dependents.All(dependent => DependentFound(dependent, modules));
        }

        private static bool DependentFound(string dependent, IDictionary<string, MibModule> modules)
        {
            if (!Regex.IsMatch(dependent, Pattern))
            {
                return modules.ContainsKey(dependent);
            }

            if (modules.ContainsKey(dependent))
            {
                return true;
            }
            
            string dependentNonVersion = Regex.Replace(dependent, Pattern, string.Empty);
            return modules.ContainsKey(dependentNonVersion);
        }
    }
}
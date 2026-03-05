// Copyright (c) 2011-2012, Lex Li
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lextm.SharpSnmpLib.Mib
{
    public class MibModule : IModule
    {
        private readonly IList<IConstruct> _constructs = new List<IConstruct>();
        public string Name { get; set; }
        public bool AllExported { get; set; }
        public string FileName { private get; set; }

        private Exports _exports;
        public Exports Exports
        {
            get { return _exports ?? (_exports = new Exports()) ; }
            set { _exports = value; }
        }

        private Imports _imports;
        private IList<IEntity> _entities;
        private List<IEntity> _objects;
        private IList<string> _dependents;
        private readonly Dictionary<string, ITypeAssignment> _typeAssignments = new Dictionary<string, ITypeAssignment>();

        public MibModule(string moduleName, List<string> dependents)
        {
            Name = moduleName;
            _dependents = dependents;
        }

        public MibModule()
        {
        }

        public Imports Imports
        {
            get { return _imports ?? (_imports = new Imports()); }
            set { _imports = value; }
        }

        public IList<IConstruct> Constructs
        {
            get {
                return _constructs;
            }
        }

        public IList<IEntity> Entities  
        {
            get
            {
                if (_entities != null)
                {
                    return _entities;
                }

                var entities = new List<IEntity>();
                foreach (var construct in Constructs)
                {
                    var assignment = construct as ValueAssignment;
                    if (assignment == null)
                    {
                        continue;
                    }

                    var item = assignment.SmiType as IEntity;
                    if (item == null)
                    {
                        continue;
                    }

                    item.Name = assignment.Name;
                    item.ModuleName = Name;
                    var seq = assignment.SmiValue as SequenceValue;
                    if (seq != null && seq.Values.Count == 1)
                    {
                        var number = (NumberLiteralValue) seq.Values[0].Value;
                        item.Value = (uint) number.Value.Value;
                        item.Parent = seq.Values[0].Name;
                        entities.Add(item);
                        continue;
                    }

                    var v = assignment.SmiValue as IdComponentList;
                    if (v != null)
                    {
                        if (v.IdComponents.Count >= 2)
                        {
                            int start = 0;
                            var parent = new StringBuilder();
                            if (v.DefinedValue != null)
                            {
                                parent.AppendFormat("{0}.", v.DefinedValue.Value);
                            }
                            while (start < v.IdComponents.Count - 1)
                            {
                                if (string.IsNullOrEmpty(v.IdComponents[start].Name))
                                {
                                    parent.AppendFormat("{0}.", v.IdComponents[start].Number);
                                }
                                else
                                {
                                    parent.AppendFormat("{0}({1}).", v.IdComponents[start].Name, v.IdComponents[start].Number);
                                }

                                start++;
                            }

                            parent.Length--;
                            item.Parent = parent.ToString();
                            if (item.Parent == "0")
                            {
                                // IMPORTANT: fix for 0.0
                                item.Parent = "ccitt";
                            }

                            item.Value = (uint)v.IdComponents[v.IdComponents.Count - 1].Number;
                            entities.Add(item);
                        }
                    }
                }

                return (_entities = entities);
            }
        }

        public IDictionary<string, ITypeAssignment> Types
        {
            get { return _typeAssignments; }
        }

        public IList<string> Dependents
        {
            get {
                return _dependents ?? (_dependents = Imports.Clauses.Select(import => import.Module).ToList());
            }
        }

        public IList<IEntity> Objects
        {
            get { return (_objects ?? (_objects = Entities.OfType<ObjectTypeMacro>().OfType<IEntity>().ToList())); }
        }

        public void Add(IConstruct construct)
        {
            _constructs.Add(construct);
        }

        internal bool Validate(IDictionary<string, MibModule> modules)
        {
            var knownConstructs = new List<IConstruct>();
            knownConstructs.AddRange(Constructs);
            foreach (var import in Imports.Clauses)
            {
                var dependencyModule = FoundDependent(import.Module, modules);
                if (dependencyModule == null)
                {
                    return false; // dependency missing.
                }

                foreach (var symbol in import.Symbols)
                {
                    var construct = dependencyModule.Find(symbol);
                    if (construct == null)
                    {
                        // TODO: make it strict later.
                        continue; //return false; // imported type missing
                    }

                    knownConstructs.Add(construct);
                }
            }

            return Entities.All(entity => entity.Validate(knownConstructs, FileName));
        }

        private IConstruct Find(string symbol)
        {
            foreach (var construct in Constructs)
            {
                if (string.CompareOrdinal(construct.Name, symbol) == 0)
                {
                    return construct;
                }
            }

            ReportMissingType(symbol);
            return null;
        }

        const string Pattern = "-V[0-9]+$";

        private static MibModule FoundDependent(string dependent, IDictionary<string, MibModule> modules)
        {
            if (Regex.IsMatch(dependent, Pattern))
            {
                string dependentNonVersion = Regex.Replace(dependent, Pattern, string.Empty);
                return modules.ContainsKey(dependentNonVersion) ? modules[dependentNonVersion] : null;
            }

            return modules.ContainsKey(dependent) ? modules[dependent] : null;
        }

        internal string ReportMissingDependencies(ICollection<string> existing)
        {
            var builder = new StringBuilder();
            if (String.IsNullOrEmpty(FileName))
            {
                builder.Append("warning N0001 : ");
            }
            else
            {
                builder.AppendFormat("{0} : warning N0001 : ", FileName);
            }

            builder.AppendFormat("{0} is pending. Missing dependencies: ", Name);
            foreach (string depend in Dependents.Where(depend => !existing.Contains(depend)))
            {
                builder.Append(depend).Append(", ");
            }

            return builder.ToString();
        }

        internal string ReportMissingType(string symbol)
        {
            var builder = new StringBuilder();
            if (String.IsNullOrEmpty(FileName))
            {
                builder.Append("error S0002 : ");
            }
            else
            {
                builder.AppendFormat("{0} : error S0002 : ", FileName);
            }

            builder.AppendFormat("failed to import type {0} from module {1}.", symbol, Name);
            return builder.ToString();
        }

        public string ReportDuplicate(MibModule existing)
        {
            return string.IsNullOrEmpty(FileName)
                       ? string.Format("warning N0002 : {0} is duplicate of {1}", Name, existing.FileName)
                       : string.Format("{0} : warning N0002 : {1} is duplicate of {2}", FileName, Name, existing.FileName);
        }

        public string ReportImplicitObjectIdentifier(ObjectIdentifierType extra)
        {
            return string.IsNullOrEmpty(FileName)
                       ? string.Format("warning N0003 : implicit object identifier {0} ({1}) created under {2} in module {3}", extra.Name, extra.Value, extra.Parent, Name)
                       : string.Format("{0} : warning N0003 : implicit object identifier {1} ({2}) created under {3} in module {4}", FileName, extra.Name, extra.Value, extra.Parent, Name);
        }

        public string ReportIgnoredImplicitEntity(string longParent)
        {
            return string.IsNullOrEmpty(FileName)
                       ? string.Format("warning N0004 : ignore implicit entity created {0} in module {1}", longParent, Name)
                       : string.Format("{0} : warning N0004 : ignore implicit entity created {1} in module {2}", FileName, longParent, Name);
        }
    }
}

//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections.Generic;
using System.Collections;
namespace Alachisoft.NCache.Common.AssemblyBrowser
{
    public class AssemblyReflector
    {
        List<string> _classes = null;
        AssemblyDef[] _assembly;
        [CLSCompliant(false)]
        public ClassTypeFilter _readThruTypefilter;
        public ClassTypeFilter _writeThruTypefilter;
        public AssemblyReflector(string[] filenames, ClassTypeFilter readThruFilter, ClassTypeFilter writeThruFilter)
        {
            _assembly = new AssemblyDef[filenames.Length];
            LoadAssemblies(filenames);
            _readThruTypefilter = readThruFilter;
            _writeThruTypefilter = writeThruFilter;
        }

        private void LoadAssemblies(string[] filenames)
        {
            int index = 0;

            foreach (string filename in filenames)
            {
                ProxyDomain pd = new ProxyDomain();
                _assembly[index] = pd.GetAssembly(filename);
                index++;
            }
        }

        public List<string> GetClasses()
        {
            try
            {
                if (_assembly != null)
                {
                    foreach (AssemblyDef asm in _assembly)
                    {

                        TypeDef[] type = asm.GetTypes();
                        _classes = new List<string>();
                        IEnumerator enu = type.GetEnumerator();
                        while (enu.MoveNext())
                        {
                            if (_readThruTypefilter != null)
                                if (!_readThruTypefilter.FilterType((TypeDef)enu.Current))
                                    continue;
                            if (_writeThruTypefilter != null)
                                if (!_writeThruTypefilter.FilterType((TypeDef)enu.Current))
                                    continue;
                            string innerClass = enu.Current.ToString();
                            if (innerClass.Contains("+"))
                                innerClass = innerClass.Replace("+", ".");
                            _classes.Add(innerClass);

                        }
                    }

                    return _classes;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return null;

        }

        public string[] FullName
        {
            get
            {
                string[] names = new string[_assembly.Length];
                int index = 0;
                foreach (AssemblyDef asm in _assembly)
                {
                    names[index] = asm.FullName;
                    index++;
                }
                return names;
            }
        }
        public string GetfirstItem
        {
            get
            {
                try
                {
                    return _classes[0];
                }
                catch (Exception)
                {
                    throw new Exception("Provider class not found.");
                }
            }
        }
    }
}

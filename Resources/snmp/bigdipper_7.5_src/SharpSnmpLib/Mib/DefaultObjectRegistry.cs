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
using System.IO;
using System.Text;
using Lextm.SharpSnmpLib.Properties;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Default object registry.
    /// </summary>
    [Obsolete("Use SimpleObjectRegistry instead.")]
    public sealed class DefaultObjectRegistry : ObjectRegistryBase
    {
        private static volatile DefaultObjectRegistry _instance;
        private static readonly object Locker = new object();
        
        private DefaultObjectRegistry()
        {
            Tree = new ObjectTree(LoadDefaultModules());
        }

        /// <summary>
        /// Default instance.
        /// </summary>
        [CLSCompliant(false)]
        public static DefaultObjectRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Locker)
                    {
                        if (_instance == null)
                        {
                            _instance = new DefaultObjectRegistry();
                        }
                    }
                }
                
                return _instance;
            }
        }

        private static IList<ModuleLoader> LoadDefaultModules()
        {
            IList<ModuleLoader> result = new List<ModuleLoader>(5)
                                             {
                                                 LoadSingle(Encoding.ASCII.GetString(Resources.SNMPV2_SMI), "SNMPv2-SMI"),
                                                 LoadSingle(Encoding.ASCII.GetString(Resources.SNMPV2_CONF), "SNMPv2-CONF"),
                                                 LoadSingle(Encoding.ASCII.GetString(Resources.SNMPV2_TC), "SNMPv2-TC"),
                                                 LoadSingle(Encoding.ASCII.GetString(Resources.SNMPV2_MIB), "SNMPv2-MIB"),
                                                 LoadSingle(Encoding.ASCII.GetString(Resources.SNMPV2_TM), "SNMPv2-TM")
                                             };
            return result;
        }

        private static ModuleLoader LoadSingle(string mibFileContent, string name)
        {
            ModuleLoader result;
            using (TextReader reader = new StringReader(mibFileContent))
            {
                result = new ModuleLoader(reader, name);
            }
            
            return result;
        }
    }
}
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
 * Date: 2008/5/16
 * Time: 20:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Object registry.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Reloadable", Justification = "By design")]
    public class ReloadableObjectRegistry : ObjectRegistryBase
    {
        private readonly string _path;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadableObjectRegistry"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public ReloadableObjectRegistry(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                throw new ArgumentException(@"Path cannot be empty.", "path");
            }

            if (!Directory.Exists(path))
            {
                throw new ArgumentException(@"Path is invalid", "path");
            }

            _path = System.IO.Path.GetFullPath(path);
            LoadModuleFolder(_path);
        }

        /// <summary>
        /// Reloads the registry.
        /// </summary>
        public void Reload()
        {
            // FIXME: only used in Browser. Low efficiency.
            LoadModuleFolder(_path);
            Refresh();
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path
        {
            get { return _path; }
        }

        private void LoadModuleFolder(string path)
        {
            if (Directory.Exists(path))
            {
                string index = System.IO.Path.Combine(path, "index");
                if (File.Exists(index))
                {
                    List<string> list = new List<string>();
                    using (StreamReader reader = new StreamReader(index))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            list.Add(System.IO.Path.Combine(path, line + ".module"));
                        }
                    }
                    
                    Tree = new ObjectTree(list.ToArray());
                }
                else 
                {
                    Tree = new ObjectTree(Directory.GetFiles(path, "*.module"));
                }
            }
            else 
            {
                Tree = new ObjectTree();
            }
        }
    }
}

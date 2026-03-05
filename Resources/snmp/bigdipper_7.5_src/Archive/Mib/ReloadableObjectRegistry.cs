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

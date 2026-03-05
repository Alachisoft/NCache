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
using System.Globalization;
using System.IO;

namespace Lextm.SharpSnmpLib.Mib
{
    /// <summary>
    /// Object registry.
    /// </summary>
    public class ObjectRegistry : IObjectRegistry
    {
        private ObjectTree _tree;
        private static volatile IObjectRegistry _default;
        private static readonly object locker = new object();
        private readonly string _path;
        private readonly static string DefaultPath = System.IO.Path.GetFullPath("modules");

        private ObjectRegistry()
        {
            _path = DefaultPath;
            _tree = new ObjectTree(LoadDefaultModules());
        }
        
        private static IList<ModuleLoader> LoadDefaultModules()
        {
            IList<ModuleLoader> result = new List<ModuleLoader>(5);
            result.Add(LoadSingle(Resource.SNMPv2_SMI, "SNMPV2-SMI"));
            result.Add(LoadSingle(Resource.SNMPv2_CONF, "SNMPV2-CONF"));
            result.Add(LoadSingle(Resource.SNMPv2_TC, "SNMPV2-TC"));
            result.Add(LoadSingle(Resource.SNMPv2_MIB, "SNMPV2-MIB"));
            result.Add(LoadSingle(Resource.SNMPv2_TM, "SNMPV2-TM"));
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
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRegistry"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public ObjectRegistry(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                _path = DefaultPath;
            }
            else
            {
                _path = System.IO.Path.GetFullPath(path);
            }

            LoadModuleFolder(_path);
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
                    
                    _tree = new ObjectTree(list.ToArray());
                }
                else 
                {
                    _tree = new ObjectTree(Directory.GetFiles(path, "*.module"));
                }
            }
            else 
            {
                _tree = new ObjectTree();
            }
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
        /// This event occurs when new documents are loaded.
        /// </summary>
        public event EventHandler<EventArgs> OnChanged;
        
        /// <summary>
        /// Object tree.
        /// </summary>
        [CLSCompliant(false)]
        public IObjectTree Tree
        {
            get
            {
                return _tree;
            }
        }
        
        /// <summary>
        /// Registry
        /// </summary>
        [CLSCompliant(false)]
        public static IObjectRegistry Default
        {
            get
            {
                if (_default == null)
                {
                    lock (locker)
                    {
                        if (_default == null)
                        {
                            _default = new ObjectRegistry();
                        }
                    }
                }
                
                return _default;
            }
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path
        {
            get { return _path; }
        }
        
        /// <summary>
        /// Indicates that if the specific OID is a table.
        /// </summary>
        /// <param name="id">OID</param>
        /// <returns></returns>
        internal bool IsTableId(uint[] id)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            
            // TODO: enhance checking here.
            string name = Translate(id);
            return name.EndsWith("Table", StringComparison.Ordinal);
        }

        /// <summary>
        /// Validates if an <see cref="ObjectIdentifier"/> is a table.
        /// </summary>
        /// <param name="identifier">The object identifier.</param>
        /// <returns></returns>
        public bool ValidateTable(ObjectIdentifier identifier)
        {
            if (identifier == null)
            {
                throw new ArgumentNullException("identifier");
            }
            
            try
            {
                return IsTableId(identifier.ToNumerical());
            }
            catch (ArgumentOutOfRangeException)
            {
                // if no matching definition found, refuse to continue.
                return false;
            }
        }

        /// <summary>
        /// Gets numercial form from textual form.
        /// </summary>
        /// <param name="textual">Textual</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public uint[] Translate(string textual)
        {
            if (textual == null)
            {
                throw new ArgumentNullException("textual");
            }
            
            if (textual.Length == 0)
            {
                throw new ArgumentException("textual cannot be empty");
            }
            
            string[] content = textual.Split(new string[] { "::" }, StringSplitOptions.None);
            if (content.Length != 2)
            {
                throw new ArgumentException("textual format must be '<module>::<name>'");
            }
            
            return Translate(content[0], content[1]);
        }
        
        /// <summary>
        /// Gets numerical form from textual form.
        /// </summary>
        /// <param name="moduleName">Module name</param>
        /// <param name="name">Object name</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public uint[] Translate(string moduleName, string name)
        {
            if (moduleName == null)
            {
                throw new ArgumentNullException("moduleName");
            }
            
            if (moduleName.Length == 0)
            {
                throw new ArgumentException("module cannot be empty");
            }
            
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            
            if (name.Length == 0)
            {
                throw new ArgumentException("name cannot be empty");
            }
            
            if (!name.Contains("."))
            {
                return _tree.Find(moduleName, name).GetNumericalForm();
            }
            
            string[] content = name.Split('.');
            if (content.Length != 2)
            {
                throw new ArgumentException("name can only contain one dot");
            }
            
            int value;
            bool succeeded = int.TryParse(content[1], out value);
            if (!succeeded)
            {
                throw new ArgumentException("not a decimal after dot");
            }
            
            uint[] oid = _tree.Find(moduleName, content[0]).GetNumericalForm();
            return Definition.AppendTo(oid, (uint)value);
        }
        
        /// <summary>
        /// Gets textual form from numerical form.
        /// </summary>
        /// <param name="numerical">Numerical form</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public string Translate(uint[] numerical)
        {
            if (numerical == null)
            {
                throw new ArgumentNullException("numerical");
            }
            
            try
            {
                return _tree.Find(numerical).TextualForm;
            }
            catch (ArgumentOutOfRangeException)
            {
                // no definition matches numerical.
            }
            
            return _tree.Find(GetParent(numerical)).TextualForm + "." + numerical[numerical.Length - 1].ToString(CultureInfo.InvariantCulture);
        }
        
        private static uint[] GetParent(uint[] id)
        {
            uint[] result = new uint[id.Length - 1];
            Array.Copy(id, result, id.Length - 1);
            return result;
        }
        
        /// <summary>
        /// Loads a folder of MIB files.
        /// </summary>
        /// <param name="folder">Folder</param>
        /// <param name="pattern">MIB file pattern</param>
        public void CompileFolder(string folder, string pattern)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }
            
            if (folder.Length == 0)
            {
                throw new ArgumentException("folder cannot be empty");
            }
            
            string path = System.IO.Path.GetFullPath(folder);
            
            if (!Directory.Exists(path))
            {
                throw new ArgumentException("folder does not exist: " + path);
            }
            
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            
            if (pattern.Length == 0)
            {
                throw new ArgumentException("pattern cannot be empty");
            }
            
            CompileFiles(Directory.GetFiles(path, pattern));
        }

        /// <summary>
        /// Loads MIB files.
        /// </summary>
        /// <param name="fileNames">File names.</param>
        public void CompileFiles(IEnumerable<string> fileNames)
        {
            if (fileNames == null)
            {
                throw new ArgumentNullException("fileNames");
            }

            foreach (string fileName in fileNames)
            {
                Import(Parser.Compile(fileName));
            }
            
            Refresh();
        }
        
        /// <summary>
        /// Loads a MIB file.
        /// </summary>
        /// <param name="fileName">File name</param>
        public void Compile(string fileName)
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
            
            Import(Parser.Compile(fileName));
            Refresh();
        }
        
        /// <summary>
        /// Imports instances of <see cref="MibModule"/>.
        /// </summary>
        /// <param name="modules">Modules.</param>
        public void Import(IEnumerable<MibModule> modules)
        {
            _tree.Import(modules);
        }

        /// <summary>
        /// Refreshes.
        /// </summary>
        /// <remarks>This method raises an <see cref="OnChanged"/> event. </remarks>
        public void Refresh()
        {
            _tree.Refresh();
            EventHandler<EventArgs> handler = OnChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }        
                
        /// <summary>
        /// Gets the textual format.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <param name="id">Object ID.</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static string GetTextual(ObjectIdentifier id, IObjectRegistry registry)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            
            IObjectRegistry objects = registry ?? ObjectRegistry.Default;
            return objects.Translate(id.ToNumerical());
        }

        #region IObjectRegistry Members

        /// <summary>
        /// Creates a variable.
        /// </summary>
        /// <param name="textual">The textual.</param>
        /// <returns></returns>
        public Variable CreateVariable(string textual)
        {
            return CreateVariable(textual, null);
        }

        /// <summary>
        /// Creates a variable.
        /// </summary>
        /// <param name="textual">The textual ID.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public Variable CreateVariable(string textual, ISnmpData data)
        {
            return new Variable(Translate(textual), data);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;

namespace Lextm.SharpSnmpLib.Compiler
{
    internal class FileAddedEventArgs : EventArgs
    {
        private readonly IList<string> _files;

        public FileAddedEventArgs(IList<string> files)
        {
            _files = files;
        }

        public IList<string> Files
        {
            get { return _files; }
        }
    }
}
/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/6/28
 * Time: 12:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using Lextm.SharpSnmpLib.Mib;

namespace Lextm.SharpSnmpLib.Compiler
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
    internal class CompilerCore : IDisposable
    {
        private bool _disposed;
        private readonly IList<string> _files = new List<string>();
        private BackgroundWorker _worker = new BackgroundWorker();
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("Lextm.SharpSnmpLib.Compiler");

        public CompilerCore()
        {
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += BackgroundWorker1DoWork;
            _worker.RunWorkerCompleted += BackgroundWorker1RunWorkerCompleted;
        }

        public bool IsBusy
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                
                return _worker.IsBusy;
            }
        }

        public Parser Parser { get; set; }

        public Assembler Assembler { get; set; }

        public event EventHandler<EventArgs> RunCompilerCompleted;

        public event EventHandler<FileAddedEventArgs> FileAdded;

        public void Compile(IEnumerable<string> files)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }            
            
            _worker.RunWorkerAsync(files);
        }
        
        private void BackgroundWorker1DoWork(object sender, DoWorkEventArgs e)
        {
            var docs = (IEnumerable<string>)e.Argument;
            IEnumerable<CompilerError> errors;
            IEnumerable<CompilerWarning> warnings;
            CompileInternal(docs, out errors, out warnings);
            e.Result = new Tuple<IEnumerable<CompilerError>, IEnumerable<CompilerWarning>>(errors, warnings);
        }

        private void CompileInternal(IEnumerable<string> docs, out IEnumerable<CompilerError> errors, out IEnumerable<CompilerWarning> warnings)
        {
            var modules = Parser.ParseToModules(docs, out errors, out warnings);
            Assembler.Assemble(modules);
        }

        private void BackgroundWorker1RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                var results = (Tuple<IEnumerable<CompilerError>, IEnumerable<CompilerWarning>>)e.Result;
                var errors = results.Item1;
                foreach (CompilerError error in errors)
                {
                    Logger.Info(error.Details);
                }

                var warnings = results.Item2;
                foreach (var warning in warnings)
                {
                    Logger.Info(warning.Details);
                }
            }

            if (e.Error != null)
            {
                Logger.Info(e.Error);
            }

            if (RunCompilerCompleted != null)
            {
                RunCompilerCompleted(this, EventArgs.Empty);
            }

            SystemSounds.Beep.Play();
        }

        public void Add(IEnumerable<string> files)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            } 
            
            IList<string> filered = new List<string>();
            foreach (string file in files.Where(file => !_files.Contains(file)))
            {
                _files.Add(file);
                filered.Add(file);
            }

            if (FileAdded != null)
            {
                FileAdded(this, new FileAddedEventArgs(filered));
            }
        }

        public void CompileAll()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }   
            
            Compile(_files);
        }

        public void Remove(string name)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }   
            
            _files.Remove(name);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        ~CompilerCore()
        {
            Dispose(false);
        }
        
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.ComponentModel.Component"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            
            if (disposing)
            {
                if (_worker != null)
                {
                    _worker.Dispose();
                    _worker = null;
                }
            }
            
            _disposed = true;
        }
    }
}

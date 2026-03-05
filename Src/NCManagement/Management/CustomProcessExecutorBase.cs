using Alachisoft.NCache.Common;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Management
{
    public class CustomProcessExecutorBase
    {
        private string _command = default(string);
        Process process = null;
        protected String _toolName = null;
        private bool _waitForExit = false;

        public bool HasExited
        {
            get { return process.HasExited; }
        }

        public Process Process
        {
            get { return process; }
        }

        protected virtual string GetApplicationPath()
        {
            string part1 = Path.Combine(AppUtil.InstallDir, "bin");
            string part2 = Path.Combine(part1, "tools");
            string path = Path.Combine(part2, _toolName);
            return path;
        }

        public CustomProcessExecutorBase(string command,String toolName,bool waitForCompletion=false)
        {
            _toolName = toolName;
            _command = command;
            _waitForExit = waitForCompletion;
            process = new Process()
            {
                StartInfo = new ProcessStartInfo()
            };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.StartInfo.Arguments = _command;
            process.StartInfo.FileName = GetApplicationPath();
            process.EnableRaisingEvents = true;
        }

        public void InvokeOnExit(EventHandler eventHandler)
        {
            process.Exited += eventHandler;
        }
        /// <summary>
        /// Executes the process for NActivate command line tool. 
        /// Returns true if auto-renewal is successful else returns false.
        /// </summary>
        /// <returns>bool</returns>
        public void Execute()
        {            
            try
            {
                process.Start();
                if(_waitForExit)
                    process.WaitForExit();
                AppUtil.LogEvent("NCache", process.StandardOutput.ReadToEnd(), EventLogEntryType.Information, EventCategories.Information, EventID.GeneralInformation);
            }
            catch (Exception ex)
            {
                if (process != null && !process.HasExited && !(ex is Runtime.Exceptions.TimeoutException))
                    process.Kill();
                throw ex;
            }
        }


    }
}
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Management.Management
{
    public class MemoryDumpGenerator
    {
        static IDictionary<int, Process> _procDumpProcesses = new Dictionary<int, Process>();
        private static CustomProcessExecutorBase _dumpProcessExecutor;
        MemoryDumpMetainfo _memoryDumpMetainfo = default(MemoryDumpMetainfo);
        int _processId = 0;
        String _cacheName = default(String);
        bool _waitForCompletion = false;
        Hashtable _runningCaches; //refernce for the s_caches
        CacheServer _cacheServer;//reference for the CacheServer
        Process process = null;

        public MemoryDumpGenerator(int processId, String cacheName, bool waitForCompletion, Hashtable runningCaches, CacheServer cacheServer)
        {
            _processId = processId;
            _cacheName = cacheName;
            _waitForCompletion = waitForCompletion;
            _runningCaches = runningCaches;
            _cacheServer = cacheServer;
        }

        #region Private Methods
        private void OnProcessExit(object sender, EventArgs e)
        {
            lock (_procDumpProcesses)
            {
                int processId = ((Process)sender).Id;
                if (_procDumpProcesses.ContainsKey(processId))
                    _procDumpProcesses.Remove(processId);
            }
        }

        private string GetCompleteFileName(string dumpsPath, string dumpName)
        {
            int i = 0;
            string newDumpName = dumpName;
            while (true)
            {
                string filename = Path.Combine(dumpsPath, newDumpName + ".dmp");
                if (!File.Exists(filename))
                {
                    return Path.Combine(dumpsPath, newDumpName);
                }
                i++;
                newDumpName = dumpName + "_" + i;
            }
            return null;
        }
        #endregion
        public void StartProcess()
        {
            string dumpsPath = AppUtil.DumpsDir;
            string dumpName = default(string);
            if (!Directory.Exists(dumpsPath))
                Directory.CreateDirectory(dumpsPath);

            //if CacheName is specifed
            if (!string.IsNullOrEmpty(_cacheName))
            {
                if (!_runningCaches.ContainsKey(_cacheName))
                {
                    throw new CacheException(_cacheName + " is not running on this server");
                }
                _processId = _cacheServer.GetCacheHostProcessID(_cacheName);
            }
            //if CacheName is not specifed and processId is specified
            //then first check if Cache exist against that process id
            else
            {
                foreach (DictionaryEntry entry in _runningCaches)
                {
                    //if Cache exists the get cachename
                    if (((CacheInfo)entry.Value).CacheProcessId == _processId)
                    {
                        _cacheName = (string)entry.Key;
                    }
                }
            }

            //Append cache name in dump file
            if (!string.IsNullOrEmpty(_cacheName))
                dumpName = _cacheName;
            else
                dumpName = Process.GetProcessById(_processId).ProcessName;

            dumpsPath = GetCompleteFileName(dumpsPath, dumpName);
            //if process is not running
            if (!Process.GetProcesses().Any(x => x.Id == _processId))
            {
                throw new CacheException("No process with the given process id is running on this server");
            }


            string serverIP = _cacheServer.ClusterIP;
            string dateTimeString = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");

            dumpsPath += $"_{dateTimeString}_{serverIP}";

            if (_cacheServer.GetOSPlatform() == OSInfo.Linux)
            {
                string bashCommand = $"-u {_processId} -f \"{dumpsPath}.dmp\"";
                _dumpProcessExecutor = new CreateDumpProcessExecutor(bashCommand);
            }
            else
            {
                string doubleQuotes = "\"";
                StringBuilder _params = new StringBuilder();
                _params.Append(" -ma").Append(" ");
                _params.Append(_processId).Append(" ");
                _params.Append("-accepteula").Append(" ");
                _params.Append(doubleQuotes).Append(dumpsPath).Append(doubleQuotes);
                _dumpProcessExecutor = new ProcDumpProcessExecutor(_params.ToString());
            }

            _dumpProcessExecutor.Execute();
            int pId = _dumpProcessExecutor.Process.Id;
            if (_waitForCompletion)
                _procDumpProcesses.Add(pId, _dumpProcessExecutor.Process);

            _memoryDumpMetainfo = new MemoryDumpMetainfo()
            {
                DumpProcessId = pId,
                FileName = dumpsPath + ".dmp"
            };
        }

        public static bool DumpProcessExist(int processId)
        {
            lock (_procDumpProcesses)
            {
                if (_procDumpProcesses.ContainsKey(processId))
                {
                    var process = _procDumpProcesses[processId];
                    if (process.HasExited)
                    {
                        _procDumpProcesses.Remove(processId);

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        if (output.Contains("There is not enough space on the disk") || error.Contains("No space left on device"))
                        {
                            string errorMessage = "Insufficient disk space to create the dump file. Please ensure that there is enough space on the target machine.";
                            AppUtil.LogEvent("NCache", errorMessage, EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                            throw new Exception(errorMessage);
                        }

                        if (error.Contains("Permission denied"))
                        {
                            string errorMessage = "Permission Denied: Failed to take memory dump. This is due to restricted memory access. Possible Fix: Allow access by setting `ptrace_scope` to 0: echo 0 | sudo tee /proc/sys/kernel/yama/ptrace_scope";
                            throw new Exception(errorMessage);
                        }

                        AppUtil.LogEvent("NCache", output, EventLogEntryType.Information, EventCategories.Information, EventID.GeneralInformation);

                        if (!string.IsNullOrEmpty(error))
                        {
                            AppUtil.LogEvent("NCache", error, EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                            throw new Exception(error);

                        }
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }

        public MemoryDumpMetainfo GetMemoryDumpMetaInfo()
        {
            return _memoryDumpMetainfo;
        }
    }
}

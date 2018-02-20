// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;

namespace Alachisoft.NGroups.Stack
{
    /// <summary>
    /// Create logs
    /// </summary>
    public class Logs
    {
        private TextWriter _writer;
        private bool _logsEnabled;
        private bool _errorLogsEnabled;

        private object _sync_mutex = new object();

        /// <summary>
        /// Gets/Sets the flag whether logs are enabled or not.
        /// </summary>
        public bool LogsEnabled
        {
            get { return _logsEnabled; }
            set { lock (_sync_mutex) { _logsEnabled = value; } }
        }

        /// <summary>
        /// Gets/Sets the flag whether Error logs are enabled or not.
        /// </summary>
        public bool IsErrorLogEnabled
        {
            get { return _errorLogsEnabled; }
            set { lock (_sync_mutex) { _errorLogsEnabled = value; } }
        }

        /// <summary>
        /// True if writer is not instantiated, false otherwise
        /// </summary>
        public bool IsWriterNull
        {
            get
            {
                if (_writer == null) return true;
                else return false;
            }
        }

        /// <summary>
        /// Creates logs in installation folder
        /// </summary>
        /// <param name="fileName">name of file</param>
        /// <param name="directory">directory in which the logs are to be made</param>
        public void Initialize(string fileName, string directory)
        {
            Initialize(fileName, null, directory);
        }

        /// <summary>
        /// Creates logs in provided folder
        /// </summary>
        /// <param name="fileName">name of file</param>
        /// <param name="filepath">path where logs are to be created</param>
        /// <param name="directory">directory in which the logs are to be made</param>
        public void Initialize(string fileName, string filepath, string directory)
        {
            lock (_sync_mutex)
            {
                string filename = fileName + "." +
                                  Environment.MachineName.ToLower() + "." +
                                  DateTime.Now.ToString("dd-MM-yy HH-mm-ss") + @".logs.txt";

                if (filepath == null || filepath == string.Empty)
                {

#if JAVA
                        filepath = Path.Combine(filepath, "log");
#else
                    filepath = Path.Combine(filepath, "log-files");
#endif
                    if (!Directory.Exists(filepath)) Directory.CreateDirectory(filepath);
                }

                try
                {
                    filepath = Path.Combine(filepath, directory);
                    if (!Directory.Exists(filepath)) Directory.CreateDirectory(filepath);

                    filepath = Path.Combine(filepath, filename);

                    _writer = TextWriter.Synchronized(new StreamWriter(filepath, false));
                    _errorLogsEnabled = true;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Write to log file
        /// </summary>
        /// <param name="module">module</param>
        /// <param name="logText">text</param>
        public void WriteLogEntry(string module, string logText)
        {
            if (_writer != null)
            {
                int space2 = 40;
                string line = null;
                line = System.DateTime.Now.ToString("HH:mm:ss:ffff") + ":  " + module.PadRight(space2, ' ') + logText;
                //string line = DateTime.Now.ToString() + "          " + logText;
                lock (_sync_mutex)
                {
                    _writer.WriteLine(line);
                    _writer.Flush();
                }
            }
        }

        /// <summary>
        /// Close writer
        /// </summary>
        public void Close()
        {
            lock (_sync_mutex)
            {
                if (_writer != null)
                {
                    lock (_writer)
                    {
                        _writer.Close();
                        _writer = null;
                        _logsEnabled = false;
                        _errorLogsEnabled = false;
                    }
                }
            }
        }
    }
}

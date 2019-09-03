using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NGroups.Stack
{
    /// <summary>
    /// Summary description for PerfStatsCollector.
    /// </summary>
    public class PerfStatsCollector : IDisposable
    {

        #region /                           --- Logger ----                 /

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


                        filepath = Path.Combine(filepath, "log-files");

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

        #endregion
        /// <summary> Instance name. </summary>
        private string _instanceName;
        /// <summary> Port number. </summary>
        private string _port;


        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcTcpDownQueueCount = null;

        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcTcpUpQueueCount = null;

        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcBcastQueueCount = null;

        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcMcastQueueCount = null;

        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcBytesSentPerSec = null;

        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcBytesReceivedPerSec = null;

        /// <summary> performance counter for Clustered Operations. </summary>
        private PerformanceCounter _pcClusteredOperationsPerSec = null;

        /// <summary> performance counter for Socket send time(mili seconds). </summary>
        private PerformanceCounter _pcSocketSendTime = null;

        /// <summary> performance counter for Socket send size (bytes). </summary>
        private PerformanceCounter _pcSocketSendSize = null;

        /// <summary> performance counter for Socket recv time (mili seconds). </summary>
        private PerformanceCounter _pcSocketReceiveTime = null;

        /// <summary> performance counter for Socker recv size(bytes). </summary>
        private PerformanceCounter _pcSocketReceiveSize = null;

        ///
        /// <summary> performance counter for Socket recv time (mili seconds). </summary>
        private PerformanceCounter _pcTcpDownEnter = null;
        private PerformanceCounter _pcClusterOpsSent= null;
        private PerformanceCounter _pcClusterOpsReceived = null;
        private PerformanceCounter _pcResponseSent = null;
        ////

        /// <summary> performance counter for Cache hits per second. </summary>
        //public NewTrace nTrace;
        //public string _cacheName;
        private ILogger _ncacheLog;
        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
            set { _ncacheLog = value; }
        }

        /// <summary> Category name of counter performance data.</summary>

        private const string PC_CATEGORY = "NCache";

        private Thread _printThread;
        private TimeSpan _printInterval = new TimeSpan(10,0,0);
        private string _logFilePath;
        private Logs _logger;

        private float _clusterOps = 0;
        private float _bytesSent = 0;
        private float _bytesRecieved = 0;
        private float _requestsSent = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        public PerfStatsCollector(string instanceName)
        {
            _instanceName = instanceName + " - " + Process.GetCurrentProcess().Id.ToString();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="port"></param>
        public PerfStatsCollector(string instanceName, int port)
        {

            _port = ":" + port.ToString();
            _instanceName = instanceName + " - " + Process.GetCurrentProcess().Id.ToString() + _port;
        }

        /// <summary>
        /// Returns true if the current user has the rights to read/write to performance counters
        /// under the category of object cache.
        /// </summary>
        public string InstanceName
        {
            get { return _instanceName; }
            set { _instanceName = value; }
        }


        /// <summary>
        /// Returns true if the current user has the rights to read/write to performance counters
        /// under the category of object cache.
        /// </summary>
        public bool UserHasAccessRights
        {
            get
            {
                try
                {
                    PerformanceCounterPermission permissions = new
                        PerformanceCounterPermission(PerformanceCounterPermissionAccess.Instrument,
                            ".", PC_CATEGORY);
                    permissions.Demand();

                    if (!PerformanceCounterCategory.Exists(PC_CATEGORY, "."))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PerfStatsCollector.UserHasAccessRights",  e.Message);
                    return false;
                }
                return true;
            }
        }



        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_printThread != null)
            {
                NCacheLog.Flush();
#if !NETCORE
                _printThread.Abort();
#elif NETCORE
                _printThread.Interrupt();
#endif
            }
            _printThread = null;
            if (_logger != null) _logger.Close();

            lock (this)
            {
                if (_pcBcastQueueCount != null)
                {
                    _pcBcastQueueCount.RemoveInstance();
                    _pcBcastQueueCount.Dispose();
                    _pcBcastQueueCount = null;
                }
                if (_pcMcastQueueCount != null)
                {
                    _pcMcastQueueCount.RemoveInstance();
                    _pcMcastQueueCount.Dispose();
                    _pcMcastQueueCount = null;
                }
                if (_pcTcpDownQueueCount != null)
                {
                    _pcTcpDownQueueCount.RemoveInstance();
                    _pcTcpDownQueueCount.Dispose();
                    _pcTcpDownQueueCount = null;
                }
                if (_pcTcpUpQueueCount != null)
                {
                    _pcTcpUpQueueCount.RemoveInstance();
                    _pcTcpUpQueueCount.Dispose();
                    _pcTcpUpQueueCount = null;
                }
                if (_pcClusteredOperationsPerSec != null)
                {
                    _pcClusteredOperationsPerSec.RemoveInstance();
                    _pcClusteredOperationsPerSec.Dispose();
                    _pcClusteredOperationsPerSec = null;
                }
                if (_pcBytesSentPerSec != null)
                {
                    _pcBytesSentPerSec.RemoveInstance();
                    _pcBytesSentPerSec.Dispose();
                    _pcBytesSentPerSec = null;
                }
                if (_pcBytesReceivedPerSec != null)
                {
                    _pcBytesReceivedPerSec.RemoveInstance();
                    _pcBytesReceivedPerSec.Dispose();
                    _pcBytesReceivedPerSec = null;
                }
                if (_pcSocketSendSize != null)
                {
                    _pcSocketSendSize.RemoveInstance();
                    _pcSocketSendSize.Dispose();
                    _pcSocketSendSize = null;
                }
                if (_pcSocketSendTime != null)
                {
                    _pcSocketSendTime.RemoveInstance();
                    _pcSocketSendTime.Dispose();
                    _pcSocketSendTime = null;
                }
                if (_pcSocketReceiveSize != null)
                {
                    _pcSocketReceiveSize.RemoveInstance();
                    _pcSocketReceiveSize.Dispose();
                    _pcSocketReceiveSize = null;
                }
                if (_pcSocketReceiveTime != null)
                {
                    _pcSocketReceiveTime.RemoveInstance();
                    _pcSocketReceiveTime.Dispose();
                    _pcSocketReceiveTime = null;
                }


                ///
                if (_pcTcpDownEnter != null)
                {
                    _pcTcpDownEnter.RemoveInstance();
                    _pcTcpDownEnter.Dispose();
                    _pcTcpDownEnter = null;
                }

                if (_pcClusterOpsSent != null)
                {
                    _pcClusterOpsSent.RemoveInstance();
                    _pcClusterOpsSent.Dispose();
                    _pcClusterOpsSent = null;
                }

                if (_pcClusterOpsReceived != null)
                {
                    _pcClusterOpsReceived.RemoveInstance();
                    _pcClusterOpsReceived.Dispose();
                    _pcClusterOpsReceived = null;
                }

                if (_pcResponseSent != null)
                {
                    _pcResponseSent.RemoveInstance();
                    _pcResponseSent.Dispose();
                    _pcResponseSent = null;
                }


                ////
            }
        }


        #endregion

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Initializes the counter instances and category.
        /// </summary>
        public void InitializePerfCounters(bool enableDebuggingCounters)
        {
#if NETCORE
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                return;
#endif
            try
            {
                if (!UserHasAccessRights)
                    return;

                lock (this)
                {
                    string instname = _instanceName;

                    _instanceName = instname;
                    if (enableDebuggingCounters)
                    {
                        _pcTcpDownQueueCount = new PerformanceCounter(PC_CATEGORY, "TcpDownQueueCount", _instanceName, false);
                        _pcTcpUpQueueCount = new PerformanceCounter(PC_CATEGORY, "TcpUpQueueCount", _instanceName, false);
                        _pcBcastQueueCount = new PerformanceCounter(PC_CATEGORY, "BcastQueueCount", _instanceName, false);
                        _pcMcastQueueCount = new PerformanceCounter(PC_CATEGORY, "McastQueueCount", _instanceName, false);
                        _pcSocketSendTime = new PerformanceCounter(PC_CATEGORY, "Socket send time (msec)", _instanceName, false);
                        _pcSocketSendSize = new PerformanceCounter(PC_CATEGORY, "Socket send size (bytes)", _instanceName, false);
                        _pcSocketReceiveTime = new PerformanceCounter(PC_CATEGORY, "Socket recv time (msec)", _instanceName, false);
                        _pcSocketReceiveSize = new PerformanceCounter(PC_CATEGORY, "Socket recv size (bytes)", _instanceName, false);
                        _pcBytesSentPerSec = new PerformanceCounter(PC_CATEGORY, "Bytes sent/sec", _instanceName, false);
                        _pcBytesReceivedPerSec = new PerformanceCounter(PC_CATEGORY, "Bytes received/sec", _instanceName, false);
                    }

                    _pcClusteredOperationsPerSec = new PerformanceCounter(PC_CATEGORY, "Cluster ops/sec", _instanceName, false);

                    _printInterval = ServiceConfiguration.StatsPrintInterval;

                    _logger = new Logs();
                    _logger.Initialize(instname + ".clstats", "ClusterPerfMonStats");

                    _printThread = new Thread(new ThreadStart(PrintStats));
                    _printThread.Start();
                }
            }
            catch (Exception e)
            {
                NCacheLog.Warn("PerfStatsCollector.PerfStatsCollector()    "+ e.Message);
            }
        }

        #endregion

        private void PrintStats()
        {
            while (_printThread != null)
            {
                try
                {
                    if (_logger != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("[ print_interval :" + _printInterval);

                        if (_pcBcastQueueCount != null)
                            sb.Append(" ; BCastQueueCount : " + _pcBcastQueueCount.RawValue);

                        if (_pcBcastQueueCount != null)
                            sb.Append(" ; TcpDownQueueCount : " + _pcTcpDownQueueCount.RawValue);

                        if (_pcBcastQueueCount != null)
                            sb.Append(" ; TcpUpQueueCount : " + _pcTcpUpQueueCount.RawValue);

                        if (_pcBcastQueueCount != null)
                            sb.Append(" ; MCastQueueCount : " + _pcMcastQueueCount.RawValue);

                        sb.Append(" ]");

                        _logger.WriteLogEntry("ClStatsCollector.PrintStats", sb.ToString());
                        System.Threading.Thread.Sleep(_printInterval);

                    }
                }
                catch (ThreadInterruptedException ti)
                {
                    break;
                }
                catch (ThreadAbortException te)
                {
                    break;
                }
                catch (Exception e)
                {
                    NCacheLog.Error("ClusterStatsCollector.PrintStats",  e.ToString());
                }
            }
        }

        public void IncrementTcpDownQueueCountStats(long count)
        {
            if (_pcTcpDownQueueCount != null)
            {
                lock (_pcTcpDownQueueCount)
                {
                    _pcTcpDownQueueCount.RawValue = count;
                }
            }
        }

        public void IncrementTcpUpQueueCountStats(long count)
        {
            if (_pcTcpUpQueueCount != null)
            {
                lock (_pcTcpUpQueueCount)
                {
                    _pcTcpUpQueueCount.RawValue = count;
                }
            }
        }

        public void IncrementBcastQueueCountStats(long count)
        {
            if (_pcBcastQueueCount != null)
            {
                lock (_pcBcastQueueCount)
                {
                    _pcBcastQueueCount.RawValue = count;
                }
            }
        }

        public void IncrementMcastQueueCountStats(long count)
        {
            if (_pcMcastQueueCount != null)
            {
                lock (_pcMcastQueueCount)
                {
                    _pcMcastQueueCount.RawValue = count;
                }
            }
        }

        public void IncrementClusteredOperationsPerSecStats()
        {
            Interlocked.Exchange(ref this._clusterOps, (this._clusterOps + 1));
            if (_pcClusteredOperationsPerSec != null)
            {
                lock (_pcClusteredOperationsPerSec)
                {
                    _pcClusteredOperationsPerSec.Increment();
                }
            }
        }
        public void IncrementBytesSentPerSecStats(long byteSent)
        {
            Interlocked.Exchange(ref this._bytesSent, (this._bytesSent + byteSent));
            if (_pcBytesSentPerSec != null)
            {
                lock (_pcBytesSentPerSec)
                {
                    _pcBytesSentPerSec.IncrementBy(byteSent);
                }
            }
        }

        public void IncrementBytesReceivedPerSecStats(long byteReceived)
        {
            Interlocked.Exchange(ref this._bytesRecieved, (this._bytesRecieved + byteReceived));
            if (_pcBytesReceivedPerSec != null)
            {
                lock (_pcBytesReceivedPerSec)
                {
                    _pcBytesReceivedPerSec.IncrementBy(byteReceived);
                }
            }
        }

        public void IncrementSocketSendTimeStats(long milisecs)
        {
            if (_pcSocketSendTime != null)
            {
                lock (_pcSocketSendTime)
                {
                    _pcSocketSendTime.IncrementBy(milisecs);
                }
            }
        }

        public void IncrementSocketSendSizeStats(long byteSent)
        {
            if (_pcSocketSendSize != null)
            {
                lock (_pcSocketSendSize)
                {
                    _pcSocketSendSize.IncrementBy(byteSent);
                }
            }
        }

        public void IncrementSocketReceiveTimeStats(long milisecs)
        {
            if (_pcSocketReceiveTime != null)
            {
                lock (_pcSocketReceiveTime)
                {
                    _pcSocketReceiveTime.IncrementBy(milisecs);
                }
            }
        }

        public void IncrementSocketReceiveSizeStats(long byteSent)
        {
            if (_pcSocketReceiveSize != null)
            {
                lock (_pcSocketReceiveSize)
                {
                    _pcSocketReceiveSize.IncrementBy(byteSent);
                }
            }
        }

        ///
        public void IncrementTcpDownEnter()
        {
            if (_pcTcpDownEnter != null)
            {
                lock (_pcTcpDownEnter)
                {
                    _pcTcpDownEnter.Increment();
                }
            }
        }

        public void IncrementClusterOpsReceived()
        {
            if (_pcClusterOpsReceived != null)
            {
                lock (_pcClusterOpsReceived)
                {
                    _pcClusterOpsReceived.Increment();
                }
            }
        }
        public void IncrementClusterOpsSent(long sentCount)
        {
            if (_pcClusterOpsSent != null)
            {
                lock (_pcClusterOpsSent)
                {
                    _pcClusterOpsSent.IncrementBy(sentCount);
                }
            }
        }
        public void IncrementResponseSent()
        {
            if (_pcResponseSent != null)
            {
                lock (_pcResponseSent)
                {
                    _pcResponseSent.Increment();
                }
            }
        }


        ////

        /// <summary>
        /// Get cluster operations done during this sample
        /// </summary>
        public float ClusterOps
        {
            get { return Interlocked.Exchange(ref this._clusterOps, 0); }
        }

        /// <summary>
        /// Get bytes recieved during this sample
        /// </summary>
        public float BytesRecieved
        {
            get { return Interlocked.Exchange(ref this._bytesRecieved, 0); }
        }

        /// <summary>
        /// Get bytes sent during this sample
        /// </summary>
        public float BytesSent
        {
            get { return Interlocked.Exchange(ref this._bytesSent, 0); }
        }
    }
}
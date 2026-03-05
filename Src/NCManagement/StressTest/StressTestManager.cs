using Alachisoft.NCache.Common.RuntimeEnvironment;
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Management
{
    public class StressTestManager
    {
        private IList<DateTime> _stressTestList;
        static object _lockobject = new object();

        private static StressTestManager _instance;

        public static StressTestManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new StressTestManager();
                return _instance;
            }
        }
        public bool IsLimitReached()
        {
            lock (_lockobject)
            {
                foreach (DateTime task in _stressTestList.ToList())
                {
                    if (task <= DateTime.Now)
                    {
                        _stressTestList.Remove(task);
                    }
                }
                return _stressTestList.Count >= ServiceConfiguration.MaxStressTestTasks ? false : true;
            }
        }

        private StressTestManager()
        {
            _stressTestList = new List<DateTime>();
        }

        private string CreateCommandLineArgument(string cacheName, int executionTime)
        {
            StringBuilder _params = new StringBuilder();

            if (!string.IsNullOrEmpty(cacheName))
                _params.Append(cacheName).Append(" ").Append(executionTime);

            return _params.ToString();
        }

        /// <summary>
        /// Executes the process for Test-Stress command line tool. 
        /// Returns process.
        /// </summary>
        /// <returns>Process</returns>
        public void Execute(int executionTime, string cacheName)
        {
            string command = CreateCommandLineArgument(cacheName, executionTime);

            Process process = null;
            try
            {
                RuntimeOption runtimeOption = new RuntimeOption()
                {
                    Command = command
                };
                process = NCacheRuntimeEnvironment.GetEnvironment.GetStressTestProcess(runtimeOption);
                lock (_lockobject)
                {
                    _stressTestList.Add(DateTime.Now.AddMilliseconds(executionTime));
                }
            }
            catch (Exception)
            {
                process?.Kill();
                throw;
            }
        }

    }
}

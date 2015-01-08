// Copyright (c) 2015 Alachisoft
// 
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
using System.Text;
using System.Diagnostics;
using System.Globalization;
using Alachisoft.NCache.Tools.Common;
using Alachisoft.NCache.Management.ServiceControl;

namespace Alachisoft.NCache.Tools.StressTestTool
{
    /// <summary>
    /// Main application class
    /// </summary>
    class Application
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
          
                StressTestTool.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    
    /// <summary>
    /// Summary description for StressTool.
    /// </summary>
    /// 
    public class StressTestToolParam: CommandLineParamsBase
    {
        private string _cacheId = "";
        private int _totalLoopCount = 0;
        private int _testCaseIterations = 20;
        private int _testCaseIterationDelay = 0;
        private int _getsPerIteration = 1;
        private int _updatesPerIteration = 1;
        private int _dataSize = 1024;
        private int _expiration = 60;
        private int _threadCount = 1;
        private int _reportingInterval = 5000;

        public StressTestToolParam()
        {}

        [ArgumentAttribute("", "")]
        public  string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [ArgumentAttribute(@"/n", @"/item-count")]
        public  int TotalLoopCount
        {
            get { return _totalLoopCount; }
            set { _totalLoopCount = value; }
        }

        [ArgumentAttribute(@"/i", @"/test-case-iterations")]
        public  int TestCaseIterations
        {
            get { return _testCaseIterations; }
            set { _testCaseIterations = value; }
        }

        [ArgumentAttribute(@"/d", @"/test-case-iteration-delay")]
        public  int TestCaseIterationDelay
        {
            get { return _testCaseIterationDelay; }
            set { _testCaseIterationDelay = value; }
        }

        [ArgumentAttribute(@"/g", @"/gets-per-iteration")]
        public  int GetsPerIteration
        {
            get { return _getsPerIteration; }
            set { _getsPerIteration = value; }
        }

        [ArgumentAttribute(@"/u", @"/updates-per-iteration")]
        public  int UpdatesPerIteration
        {
            get { return _updatesPerIteration; }
            set { _updatesPerIteration = value; }
        }

        [ArgumentAttribute(@"/m", @"/item-size")]
        public  int DataSize
        {
            get { return _dataSize; }
            set { _dataSize = value; }
        }

        [ArgumentAttribute(@"/e", @"/sliding-expiration")]
        public  int Expiration
        {
            get { return _expiration; }
            set { _expiration = value; }
        }

        [ArgumentAttribute(@"/t", @"/thread-count")]
        public  int ThreadCount
        {
            get { return _threadCount; }
            set { _threadCount = value; }
        }

        [ArgumentAttribute(@"/r", @"/reporting-interval")]
        public  int ReportingInterval
        {
            get { return _reportingInterval; }
            set { _reportingInterval = value; }
        }
    }

    sealed class StressTestTool
    {
        static private StressTestToolParam cParam = new StressTestToolParam();
        		/// <summary>
		/// Sets the application level parameters to those specified at the command line.
		/// </summary>
		/// <param name="args">array of command line parameters</param>
        static private bool ApplyParameters(string[] args)
        {    
            try
            {
                if (cParam.CacheId == String.Empty ||cParam.CacheId == null)
                {
                    Console.Error.WriteLine("Error: cache name not specified");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured while parsing input parameters. Please verify all given parameters are in correct format.");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                return false;
            }
            AssemblyUsage.PrintLogo(cParam.IsLogo);
            return true;
        }

        /// <summary>
		/// The main entry point for the tool.
		/// </summary>
		static public void Run(string[] args)
		{
			try
			{
                object param = new StressTestToolParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (StressTestToolParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }

                if (!ApplyParameters(args)) return;
                //if (!ValidateParameters()) return;



                Console.WriteLine("cacheId = {0}, total-loop-count = {1}, test-case-iterations = {2}, testCaseIterationDelay = {3}, gets-per-iteration = {4}, updates-per-iteration = {5}, data-size = {6}, expiration = {7}, thread-count = {8}, reporting-interval = {9}.", cParam.CacheId,cParam.TotalLoopCount,cParam.TestCaseIterations, cParam.TestCaseIterationDelay,cParam.GetsPerIteration,cParam.UpdatesPerIteration,cParam.DataSize,cParam.Expiration,cParam.ThreadCount,cParam.ReportingInterval);
                Console.WriteLine("-------------------------------------------------------------------\n");
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;

                ThreadTest threadTest = new ThreadTest(cParam.CacheId, cParam.TotalLoopCount,cParam.TestCaseIterations,cParam.TestCaseIterationDelay,cParam.GetsPerIteration,cParam.UpdatesPerIteration,cParam.DataSize,cParam.Expiration,cParam.ThreadCount,cParam.ReportingInterval,cParam.IsLogo);
                threadTest.Test();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.ToString());

            }
        }       
    }
}

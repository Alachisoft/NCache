using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.IO;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsDiagnostic.Test,"Stress")]
    public class StresstestBase : StressTestParameters, IConfiguration
    {
        private TestStressManager _taskManger;
        private  static TestStressManager _taskInstance;
        PowerShellAdapter adapter;
        private string TOOLNAME = "StressTest Tool";
        public PowerShellAdapter Adapter
        {
            set { adapter = value; }
        }

        private static TestStressManager  Instance
        {
            set { _taskInstance = value; }
            get
            {
                return _taskInstance;
            }
        }
  

        public bool ValidateParameters()
        {
            try
            {
                if (CacheName == String.Empty || CacheName == null)
                {
                    OutputProvider.WriteErrorLine("Error: Cache name not specified.");
                    return false;
                }

                if (!string.IsNullOrEmpty(Server))
                {
                    string[] servers = Server.Split(new char[] { ',' });

                    for (int i = 0; i < servers.Length; i++)
                    {
                        if (!ToolsUtil.IsValidIP(servers[i]))
                        {
                            OutputProvider.WriteErrorLine("Error: Invalid Server IP. {0}", servers[i]);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OutputProvider.WriteLine("Exception occured while parsing input parameters. Please verify all given parameters are in correct format.");
                OutputProvider.WriteLine(ex.Message);
                return false;
            }
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }
        protected static void ClosePowershell(object sender, ConsoleCancelEventArgs args)
        {           
            Instance.StopTasks(true);
        }

        public void TestStress()
        {
            try
            {
                               
                if (!ValidateParameters()) return;

                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error: " + e.Message);
            }
        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);

        }

        public void StartTesting ()
        {
            TestStress();
            StartStress();
        }

        protected void StartProcess()
        {
            try
            {
              

            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex.ToString());
            }
               
        }


        public void StopProcess()
        {
            StopProcessing();
            OutputProvider = null;
        }

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Automation.Util.AssemblyResolver.GetAssembly);
#endif
                Console.CancelKeyPress += new ConsoleCancelEventHandler(ClosePowershell); 
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Test-Stress Cmdlet";
                TestStress();
                StartStress();
                
            }
            catch (System.Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

        protected  void StartStress ()
        {
            try
            {
                adapter = new PowerShellAdapter(this);
                _taskManger = new TestStressManager(CacheName, ItemsCount, TestCaseIterations, TestCaseIterationDelay, GetsPerIteration, UpdatesPerIteration, DataSize, SlidingExpiration, ThreadCount, ReportingInterval, Server, printLogo, OutputProvider, adapter);
                _taskInstance = _taskManger;
                _taskManger.StartTasks();

            }
            catch (Exception e)
            {
                if (OutputProvider != null)
                {
                    OutputProvider.WriteLine(e.ToString());
                    OutputProvider.WriteLine(Environment.NewLine);
                }

            }

        }

        protected override void StopProcessing()
        {
            try
            {
                _taskManger.StopTasks(true);
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex.ToString());
            }
        }


    }
}

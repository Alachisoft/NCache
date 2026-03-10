using Client = Alachisoft.NCache.Client.Caching.Statistics;
using Cache = Alachisoft.NCache.Caching.Statistics;
using System;

namespace InstallPerfmon
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                if (args[0].Equals("install-server"))
                {
                    try
                    {
                        Cache.PerfInstaller perfInstallerCache = new Cache.PerfInstaller();
                        Console.WriteLine("Perfmon Initialized successfully for Caching");

                        Client.PerfInstaller perfInstallerClient = new Client.PerfInstaller();
                        Console.WriteLine("Perfmon Initialized successfully for Client");

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to initialize perfmon counters");
                        Console.WriteLine(e);
                    }
                }
                else if (args[0].Equals("install-client"))
                {
                    try
                    {
                        Cache.PerfInstaller perfInstallerCache = new Cache.PerfInstaller();
                        Console.WriteLine("Perfmon Initialized successfully for Caching");

                        Client.PerfInstaller perfInstallerWeb = new Client.PerfInstaller();
                        Console.WriteLine("Perfmon Initialized successfully for WebCaching");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to initialize perfmon counters");
                        Console.WriteLine(e);
                    }
                }
                else if (args[0].Equals("uninstall"))
                {
                    try
                    {
                        var categoryName = "NCache Bridge";

                        if (System.Diagnostics.PerformanceCounterCategory.Exists(categoryName))
                            System.Diagnostics.PerformanceCounterCategory.Delete(categoryName);

                        categoryName = "NCache Client";
                        if (System.Diagnostics.PerformanceCounterCategory.Exists(categoryName))
                            System.Diagnostics.PerformanceCounterCategory.Delete(categoryName);

                        categoryName = "NCache";
                        if (System.Diagnostics.PerformanceCounterCategory.Exists(categoryName))
                            System.Diagnostics.PerformanceCounterCategory.Delete(categoryName);

                        Console.WriteLine("Successfully uninstalled perfmon counters");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to uninstall perfmon counters");
                        Console.WriteLine(e);
                    }
                }
            }
            else { Console.WriteLine("Please provide a valid argument"); }
        }
    }
}

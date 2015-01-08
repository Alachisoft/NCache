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
using System.Globalization;
using Alachisoft.NCache.Tools.Common;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime;


namespace Alachisoft.NCache.Tools.AddTestData
{
    class Application
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                AddTestDataTool.Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }

    public class AddtestDataToolParam : Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string s_cacheId = "";
        private long s_itemCount = 10;
        private long s_dataSize = 1024;

        public AddtestDataToolParam()
        {

        }
        [ArgumentAttribute("", "")]
        public string S_cacheId
        {
            get { return s_cacheId; }
            set { s_cacheId = value; }
        }

        [ArgumentAttribute(@"/c", @"/count")]
        public long S_itemCount
        {
            get { return s_itemCount; }
            set { s_itemCount = value; }
        }

        [ArgumentAttribute(@"/S", @"/size")]
        public long S_dataSize
        {
            get { return s_dataSize; }
            set { s_dataSize = value; }
        }
    }

    class AddTestDataTool
    {
        static private AddtestDataToolParam cParam = new AddtestDataToolParam();

        public static void Run(string[] args)
        {
            try
            {
                object param = new AddtestDataToolParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (AddtestDataToolParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }

                if (!ValidateParameters()) return;

                AddTestData(cParam.S_cacheId,cParam.S_itemCount,cParam.S_dataSize);

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.ToString());
            }


        }

        private static bool ValidateParameters()
        {
           
            if (string.IsNullOrEmpty(cParam.S_cacheId))
            {
                Console.Error.WriteLine("\nError: Cache name not specified.");
                return false;
            }
            AssemblyUsage.PrintLogo(cParam.IsLogo);

            return true;
            
        }

        static void AddTestData(string cacheId, long itemCount, long dataSize)
        {
            Cache cache;

            try
            {
                cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheId);

                long startCount = cache.Count;

                Console.WriteLine("");
                Console.WriteLine("Adding " + itemCount + " items. Size " + dataSize + " bytes. Expiration 5 minutes...");

                DateTime startDTime = DateTime.Now;
                byte[] data = new byte[dataSize];
                for (long index = 0; index < itemCount; index++)
                {
                    try
                    {
                        cache.Insert(index.ToString(),
                                        data,
                                        System.DateTime.Now.AddMinutes(5),
                                        Cache.NoSlidingExpiration,
                                        CacheItemPriority.Default);
                    }

                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error: " + ex.Message);
                        Console.Error.WriteLine();
                        Console.Error.WriteLine(ex.ToString());
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                DateTime endDTime = DateTime.Now;
                long endCount = cache.Count;

                cache.Dispose();

                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("AddTestData started at:  {0}", startDTime.ToString());
                Console.WriteLine("AddTestData ended at:    {0}", endDTime.ToString());
                Console.WriteLine("");
                Console.WriteLine("Old cache count :      {0}", startCount);
                Console.WriteLine("New cache count :      {0}", endCount);
                Console.WriteLine("");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("ERROR: \"" + cacheId + "\" cache: " + e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.ToString());
            }

        }
    }
}

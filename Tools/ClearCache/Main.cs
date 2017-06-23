// Copyright (c) 2017 Alachisoft
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

using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Tools.Common;
using Alachisoft.NCache.Management.ServiceControl;



namespace Alachisoft.NCache.Tools.ClearCache
{
    class Application
    {
        [STAThread]
        static void Main(string[] args) 
        {
            try
            {
                ClearCacheTool.Run(args);
              
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }


    class ClearCacheToolParam:Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string s_cacheId="";
        private bool s_clearJsCss = false;
        private bool s_forceClear = false;

        public ClearCacheToolParam()
        {

        }

        [ArgumentAttribute(@"", @"")]
        public  string S_cacheId
        {
            get { return s_cacheId; }
            set { s_cacheId = value; }
        }

        [ArgumentAttribute(@"/F", @"/forceclear",false)]
        public  bool S_forceClear
        {
            get { return s_forceClear; }
            set { s_forceClear = value; }
        }

        [ArgumentAttribute(@"/w", @"/webcontent", false)]
        public  bool S_clearJsCss
        {
            get { return s_clearJsCss; }
            set { s_clearJsCss = value; }
        }
    }

    
    class ClearCacheTool
    {
        static private ClearCacheToolParam cParam = new ClearCacheToolParam();

        public static void Run(string[] args)
        {
            try
            {
                object param = new ClearCacheToolParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (ClearCacheToolParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintUsage();
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    return;
                }

                if (!ValidateParameters()) return;
                ClearCache(cParam.S_cacheId,cParam.S_forceClear,cParam.S_clearJsCss);

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(ex.ToString());
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

        static void ClearCache(string cacheId, bool forceClear, bool webOnly)
        {
            Cache cache = null;
            try
            {
                cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheId);
                if (!cParam.S_forceClear)
                {
                    long count = cache.Count;
                    Console.WriteLine("");
                    Console.WriteLine("\"" + cacheId + "\" cache currently has " + count + " items. ");
                    Console.WriteLine("Do you really want to clear it (Y or N)? ");
                    string response = Console.ReadLine();

                    if (response != "Y" && response != "y")
                    {
                        Console.WriteLine("");
                        Console.WriteLine("Cache not cleared.");
                        return;
                    }
                }
                cache.Clear();
                Console.WriteLine("");
                Console.WriteLine("Cache cleared.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.ToString());
            }
            finally
            {
                if(cache != null)
                    cache.Dispose();
            }

        }
    }
}

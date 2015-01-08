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
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Tools.Common;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Web.Caching;
using Factory = Alachisoft.NCache.Web.Caching.NCache;


namespace Alachisoft.NCache.Tools.DumpCache
{
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
                DumpCacheTool.Run(args);
              
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        public class DumpCacheParam: Alachisoft.NCache.Tools.Common.CommandLineParamsBase
        {
            private string _cacheId = "";
            private string _keyFilter = "";
            private long _keyCount = 1000;

            public DumpCacheParam()
            {

            }

            [ArgumentAttribute("", "")]
            public  string CacheId
            {
              get { return _cacheId; }
              set { _cacheId = value; }
            }
            
            [ArgumentAttribute(@"/k", @"/key-count")]
            public  long KeyCount
            {
              get { return _keyCount; }
              set { _keyCount = value; }
            }

            [ArgumentAttribute(@"/F", @"/key-filter")]
            public  string KeyFilter
            {
              get { return _keyFilter; }
              set { _keyFilter = value; }
            }

           
        }
        
        /// <summary>
        /// Summary description for GetCacheCountTool.
        /// </summary>
        sealed class DumpCacheTool
        {
            static private DumpCacheParam cParam = new DumpCacheParam();

            public static void Run(string[] args)
            {
                try
                {
                    object param = new DumpCacheParam();
                    CommandLineArgumentParser.CommandLineParser(ref param, args);
                    cParam = (DumpCacheParam)param;
                                       
                    if (cParam.IsUsage)
                    {
                        AssemblyUsage.PrintLogo(cParam.IsLogo);
                        AssemblyUsage.PrintUsage();
                        return;
                    }
                    if (!ValidateParameters()) return;

                    Cache cache = Factory.InitializeCache(cParam.CacheId);
                    cache.ExceptionsEnabled = true;

                    System.Console.WriteLine("Cache count: {0}.", cache.Count);
                    IDictionaryEnumerator keys = (IDictionaryEnumerator)cache.GetEnumerator();

                    if (keys != null)
                    {
                        long index = 0;
                        bool checkFilter = (cParam.KeyFilter != "");
                        cParam.KeyFilter = cParam.KeyFilter.Trim();
                        while (keys.MoveNext())
                        {
                            if ((cParam.KeyCount > 0) && (index >= cParam.KeyCount))
                                break;

                            if (checkFilter == true)
                            {
                                string tmpKey = (string)keys.Key;

                                if (tmpKey.Contains(cParam.KeyFilter) == true)
                                {
                                    System.Console.WriteLine(tmpKey);
                                }
                            }
                            else
                            {
                                System.Console.WriteLine(keys.Key);
                            }
                            index++;
                        }//end while 
                    }//end if 
                    cache.Dispose();
                }//end try block                
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine();
                    Console.Error.WriteLine(e.ToString());
                }
            }

             private static bool ValidateParameters()
             {
                 if (string.IsNullOrEmpty(cParam.CacheId))
                 {
                     Console.Error.WriteLine("\nError: Cache name not specified");
                     return false;
                 }
                 AssemblyUsage.PrintLogo(cParam.IsLogo);
                 return true;
             }
        } //end class
    }//end App class
}

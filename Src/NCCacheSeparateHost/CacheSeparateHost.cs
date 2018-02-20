// Copyright (c) 2018 Alachisoft
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
using System.Reflection;
using System.IO;
using System.Diagnostics;


namespace Alachisoft.NCache.CacheHost
{
    public class CacheSeparateHost
    {
        public static int Main(string[] args)
        {
#if NETCORE
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly); 
#endif
            return SeperateHost(args);
        }

        private static Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                string location = Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location);
                string installDir = directoryInfo.Parent.Parent.FullName;
                return Assembly.LoadFrom(Path.Combine(Path.Combine(installDir, "lib"), new AssemblyName(args.Name).Name + ".dll"));
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        static int SeperateHost(string[] args)
        {
            try
            {
                if (CacheSeperateHostUtil.populateValues(args))
                {

                    CacheSeperateHostUtil.StartCacheHost();
                    System.Console.WriteLine("Started");
                    Alachisoft.NCache.Management.CacheServer.SetWaitOnServiceObject();
                    return CacheSeperateHostUtil.ErrorCode;
                }
                return CacheSeperateHostUtil.ErrorCode;

            }
            catch (Exception ex)
            {
                CacheSeperateHostUtil.close();
                Alachisoft.NCache.Common.AppUtil.LogEvent(CacheSeperateHostUtil.ApplicationName, "Cache [ " + CacheSeperateHostUtil.CacheName + " ] Error:" + ex.ToString(), EventLogEntryType.Error, Alachisoft.NCache.Common.EventCategories.Error, Alachisoft.NCache.Common.EventID.GeneralError);
                System.Console.Error.WriteLine(ex.ToString());
                return CacheSeperateHostUtil.ErrorCode;
            }
        }

    }
}

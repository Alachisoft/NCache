//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Diagnostics;

#if NETCORE
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
#endif


namespace Alachisoft.NCache.CacheHost
{
    public class CacheSeparateHost
    {
        public static int Main(string[] args)
        {
#if NETCORE
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
            }
#endif
            return SeperateHost(args);
        }
#if NETCORE
        private static Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                
                string final = "";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (args.RequestingAssembly.Location.Contains("NCache" + Path.DirectorySeparatorChar + "deploy"))
                    {
                        final = Path.GetDirectoryName(args.RequestingAssembly.Location);
                    }
                    else
                    {
                        string location = Assembly.GetExecutingAssembly().Location;
                        DirectoryInfo directoryInfo = Directory.GetParent(location);
                        string bin = directoryInfo.Parent.FullName; /// in bin folder
                        string assembly = Path.Combine(bin, "assembly"); /// in assembly folder 
                        final = Path.Combine(assembly, "netcore20"); /// from where you neeed the assemblies
                    }
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (args.RequestingAssembly.Location.Contains("ncache" + Path.DirectorySeparatorChar + "deploy"))
                    {
                        final = Path.GetDirectoryName(args.RequestingAssembly.Location);
                    }
                    else
                    {
                        string location = Assembly.GetExecutingAssembly().Location;
                        DirectoryInfo directoryInfo = Directory.GetParent(location);
                        string installDir = directoryInfo.Parent.Parent.FullName; /// in installdir of linux
                        final = Path.Combine(installDir, "lib");
                        string assemblyPath = Path.Combine(final, new AssemblyName(args.Name).Name + ".dll");
                        if(!File.Exists(assemblyPath))
                        {
                            final = Path.Combine(installDir, "deploy"+Path.DirectorySeparatorChar+ CacheSeperateHostUtil.CacheName);
                        }
                    }
                    
                }
                return Assembly.LoadFrom(Path.Combine(final, new AssemblyName(args.Name).Name + ".dll"));
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
#endif

        static int SeperateHost(string[] args)
        {
            try
            {
              

                if (CacheSeperateHostUtil.populateValues(args))
                {
                    CacheSeperateHostUtil.StartCacheHost();
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

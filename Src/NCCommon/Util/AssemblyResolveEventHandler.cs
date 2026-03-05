//  Copyright (c) 2026 Alachisoft
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
using System.Reflection;
using System.IO;

namespace Alachisoft.NCache.Common.Util
{
    public class AssemblyResolveEventHandler
    {
        public static string CacheName = "";

        public static System.Reflection.Assembly DeployAssemblyHandler(object sender,ResolveEventArgs args)
        {            
            Assembly asm = null;
            string asmName=new AssemblyName(args.Name).Name;
            if (!Path.GetExtension(asmName).Equals(".dll"))
            {
                asmName += ".dll";
            }

            if (asmName.StartsWith("System.") || asmName.StartsWith("Microsoft"))
                return null;

            string deployAssemblyDirPath = string.Empty;
            if (!(string.IsNullOrEmpty(CacheName)))
            {
                try
                {
                    deployAssemblyDirPath = Path.Combine(AppUtil.DeployedAssemblyDir, CacheName);
                    asm = Assembly.LoadFrom(deployAssemblyDirPath + Path.DirectorySeparatorChar + asmName);
                    return asm;
                }
                catch (Exception ex) { }
            }
            else
            {
                if (args != null && args.RequestingAssembly != null)
                {
                    if (File.Exists(Path.Combine(Path.GetDirectoryName(args.RequestingAssembly.Location), asmName)))
                    {
                        var path = Path.Combine(Path.GetDirectoryName(args.RequestingAssembly.Location), asmName);
                        asm = Assembly.LoadFrom(path);
                        if (asm != null)
                            return asm;
                    }
                }

                string fromServiceDir = Path.Combine(AppUtil.InstallDir, "bin", "service", asmName);

                if (File.Exists(fromServiceDir))
                {
                    asm = Assembly.LoadFrom(fromServiceDir);
                    if (asm != null)
                        return asm;
                }

                deployAssemblyDirPath = AppUtil.DeployedAssemblyDir;
                if (Directory.Exists(deployAssemblyDirPath))
                {
                    string[] deployDirectories = Directory.GetDirectories(deployAssemblyDirPath);

                    foreach (string deploy in deployDirectories)
                    {
                        try
                        {
                            asm = Assembly.LoadFrom(deploy + Path.DirectorySeparatorChar + asmName);
                            break;
                        }
                        catch (Exception ex) { }
                    }
                }
            }
            return asm;
        }
    }
}

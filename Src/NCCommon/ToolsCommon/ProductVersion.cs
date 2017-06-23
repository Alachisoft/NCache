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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Tools.Common
{
    public class ProductVersion
    {
        public static string GetVersion()
        {
            string version;
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            version = fvi.FileVersion;
            try
            {
                string[] components = version.Split('.');
                int.TryParse(components[0], out ProductVersionType.Major);
                int.TryParse(components[1], out ProductVersionType.Minor);
                int.TryParse(components[2], out ProductVersionType.Build);
                int.TryParse(components[3], out ProductVersionType.Revision);

                //Example: AssemblyFileVersion is 4.6.2.3
                // productVersion = 4.6 (Major and Minor together form a product version)
                // servicePack = SP-2 (Build tells the service pack number)
                // privatePatch = PP-2 (Revision tells the private patch number)

                string productVersion = string.Format("{0}.{1}", ProductVersionType.Major, ProductVersionType.Minor);
                string servicePack = string.Format("{0}{1}", ProductVersionType.Build > 0 ? "SP" : string.Empty, ProductVersionType.Build > 0 ? ProductVersionType.Build.ToString() : string.Empty);
                string privatePatch = string.Format("{0}{1}", ProductVersionType.Revision > 0 ? "PP" : string.Empty, ProductVersionType.Revision > 0 ? ProductVersionType.Revision.ToString() : string.Empty);

                version = (string.Format("{0} {1} {2}", productVersion, servicePack, privatePatch)).TrimEnd();
                return version;

            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Error occured while reading product info: "+ex.ToString(), EventLogEntryType.Error);
                try
                {
                    string spVersion = (string)Alachisoft.NCache.Common.RegHelper.GetRegValue(Alachisoft.NCache.Common.RegHelper.ROOT_KEY, "SPVersion", 0);
                    return (string.Format("{0} {1} ", version, spVersion));

                }
                catch
                {
                    return null;
                }

            }
        }
    }

    struct ProductVersionType
    {
        public static int Major, Minor, Build, Revision;
    }
    
}


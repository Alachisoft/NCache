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
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace Alachisoft.NCache.Common.Util
{
    public class SecurityUtil
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool LogonUser(
          string principal,
          string authority,
          string password,
          int logonType,
          int logonProvider,
          out IntPtr token);

        public static bool VerifyWindowsUserForRole(string nodeName, string userName, string password, WindowsBuiltInRole role)
        {
            bool isAdministrator = false;
            IntPtr token;
            try
            {
                LogonUser(userName, nodeName, password, 3, 0, out token);
                WindowsIdentity identity = new WindowsIdentity(token);
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                if (principal.IsInRole(role))
                {
                    isAdministrator = true;
                }
            }
            catch (Exception ex)
            {

            }
            return isAdministrator;
        }
    }
}

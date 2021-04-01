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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Management.ServiceControl
{
    static class CredentialsCache
    {
        internal static readonly string BASE_KEY = RegHelper.ROOT_KEY + RegHelper.APPBASE_KEY;
        internal static readonly string CREDENTIALS_KEY = BASE_KEY + @"\SSHPorts";

        static Dictionary<string, Credentials> credentialsCache = new Dictionary<string, Credentials>();
        public static void AddCredentials(string host, Credentials credentials)
        {
            Credentials existingCredentials;
            bool found = credentialsCache.TryGetValue(host, out existingCredentials);
            if (found && existingCredentials.ConnectionFailed)
                credentialsCache.Remove(host);

            credentialsCache[host] = credentials;

            if (!(RegHelper.IsRegKeyExist(CREDENTIALS_KEY)))
            {
                RegHelper.NewKey(CREDENTIALS_KEY);
            }

            RegHelper.SetRegValue(CREDENTIALS_KEY, credentials.Host, credentials.SSHPort, 0);
        }

        public static void RemoveCredentials(string host)
        {
            if (credentialsCache.ContainsKey(host))
                credentialsCache.Remove(host);

            if (!(String.IsNullOrEmpty(Convert.ToString(RegHelper.GetRegValue(CREDENTIALS_KEY, host, 0)))))
            {
                RegHelper.DeleteRegValue(CREDENTIALS_KEY, host);
            }
        }

        public static bool TryGetCredentials(string host, out Credentials credentials)
        {
            bool found = credentialsCache.TryGetValue(host, out credentials);
            if (!found)
            {
                //If credentials are not found in cache, we should try to at least get the port from registry if there.
                object port = RegHelper.GetRegValue(CREDENTIALS_KEY, host, 0);
                if (port == null)
                    return false;

                int sshPort;
                bool parsed = int.TryParse(port.ToString(), out sshPort);
                if (parsed)
                    credentials = new Credentials(host, sshPort, string.Empty, string.Empty);
            }

            return found;
        }
    }
}

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
using System.Net;
using UnixServiceController;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Net.Sockets;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Management.ServiceControl
{
    public class JvCacheRPCService : RPCService
    {
        private bool isRunningOnUnix = false;

        public JvCacheRPCService(string server)
            : this(server, CacheConfigManager.JvCacheTcpPort)
        {
        }

        public JvCacheRPCService(string server, int port)
            : base("TayzGridSvc", server, port)
        {
        }

        protected override void Start(TimeSpan timeout, string service)
        {
            string host = ParseHostName(ServerName);

            NodeOSInfo osInfo = HostInfo.GetHostInfo(host);
            if (osInfo == null) return;

            if (osInfo.HostOS == OSInfo.Linux)
                ExecuteUnixServiceCommand(timeout, ServiceOptions.START, "");
            else 
                base.Start(timeout, ServiceNames.JVCACHE);
        }


        protected override void Stop(TimeSpan timeout, string service)
        {
            string host = ParseHostName(ServerName);

            NodeOSInfo osInfo = HostInfo.GetHostInfo(host);
            if (osInfo == null) return;

            if (osInfo.HostOS == OSInfo.Linux)
                ExecuteUnixServiceCommand(timeout, ServiceOptions.STOP, "");
            else 
                base.Stop(timeout, ServiceNames.JVCACHE);
        }

        protected override void Restart(TimeSpan timeout, string service)
        {
            string host = ParseHostName(ServerName);

            NodeOSInfo osInfo = HostInfo.GetHostInfo(host);
            if (osInfo == null) return;

            if (osInfo.HostOS == OSInfo.Linux)
                ExecuteUnixServiceCommand(timeout, ServiceOptions.RESTART, "");
            else 
                base.Restart(timeout, ServiceNames.JVCACHE);
        }

        public override void RestartSvcAfterNICChanged(TimeSpan timeout, string previousServerNode)
        {
            string host = ParseHostName(previousServerNode);

            NodeOSInfo osInfo = HostInfo.GetHostInfo(host);
            if (osInfo == null) return;

            if (osInfo.HostOS == OSInfo.Linux)
                ExecuteUnixServiceCommand(timeout, ServiceOptions.RESTARTNICCHANGE, previousServerNode);
            else 
                base.Restart(timeout, ServiceNames.JVCACHE);

        }

        protected override bool isServiceRunning(TimeSpan timeout, string service)
        {
            bool isRunning = false;

            string host = ParseHostName(ServerName);

            NodeOSInfo osInfo = HostInfo.GetHostInfo(host);
            if (osInfo == null) return isRunning;

            if (osInfo.HostOS == OSInfo.Linux)
            {
                ExecuteUnixServiceCommand(timeout, ServiceOptions.ISRUNNING, "");
                isRunning = isRunningOnUnix;
            }
            else 
                isRunning = base.isServiceRunning(timeout, ServiceNames.JVCACHE);

            return isRunning;
        }


        private static string ParseHostName(string host)
        {
            try
            {
                IPAddress.Parse(host);
            }
            catch
            {
                try
                {
                    IPAddress[] hosts = System.Net.Dns.GetHostAddresses(host);
                    foreach (IPAddress ip in hosts)
                    {
                        if (ip.AddressFamily.Equals(System.Net.Sockets.AddressFamily.InterNetwork))
                        {
                            host = ip.ToString();
                            break;
                        }
                    }
                }
                catch
                {
                }
            }
            return host;
        }

        private void ExecuteUnixServiceCommand(TimeSpan timeout, int option, string previousServerNode)
        {
            string serverName = option == ServiceOptions.RESTARTNICCHANGE ? previousServerNode : ServerName;
            CredentialsEventArgs credentialsEvent = new CredentialsEventArgs();
            credentialsEvent.NodeName = serverName;
            Credentials credentials;

            bool found = CredentialsCache.TryGetCredentials(serverName, out credentials);

            credentialsEvent.UserName = found ? credentials.Username : "ncache";
            credentialsEvent.Password = found ? credentials.Password : "ncache";

            //We might not get the credentials from cache but we can possibly get a valid port. 
            // If 'found' is false and 'credentials' is not null, it is telling us to use 'credentials.SSHPort'
            credentialsEvent.SSHPort = credentials != null ? credentials.SSHPort : 22;

            SshClient client = null;
            while (true)
            {
                try
                {
                    client = new SshClient(credentialsEvent.NodeName, credentialsEvent.SSHPort, credentialsEvent.UserName, credentialsEvent.Password);

                    client.Connect();
                    break;
                }
                catch (Exception ex)
                {
                    if (ex is SshAuthenticationException || ex is SocketException)
                    {

                        credentialsEvent.UserName = "ncache"; 
                        
                        credentialsEvent.FailureReason = ex is SshAuthenticationException ? FailureReason.AuthorizationFailure
                                                                                            : FailureReason.ConnectionFailure;
                        if (credentials != null)
                            credentials.ConnectionFailed = true;
                        GetSecurityCredentials(this, credentialsEvent);
                        if (credentialsEvent.Cancel)
                            throw new OperationCanceledException("Operation cancelled by user.");
                    }
                    else
                        throw;
                }
            }

            CredentialsCache.AddCredentials(credentialsEvent.NodeName, new Credentials(credentialsEvent.NodeName, credentialsEvent.SSHPort, credentialsEvent.UserName, credentialsEvent.Password));

            using (JvCacheServiceOnUnix service = new JvCacheServiceOnUnix(client))
            {
                if (option == ServiceOptions.START)
                    service.Start(timeout.Seconds);
                else if (option == ServiceOptions.STOP)
                    service.Stop(timeout.Seconds);
                else if (option == ServiceOptions.RESTART || option == ServiceOptions.RESTARTNICCHANGE)
                    service.Restart();
            }
        }
    }
}
    

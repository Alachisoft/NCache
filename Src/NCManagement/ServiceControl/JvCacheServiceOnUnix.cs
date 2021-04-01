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
using System.Text;
using Renci.SshNet;
using System.Threading;

namespace UnixServiceController
{
    public class JvCacheServiceOnUnix : IDisposable
    {
        string _serviceName = "ncached";
        const string UNIX_COMMAND_FORMAT_NIC = "/etc/init.d/{0} {1} {2} {3}";
        const string UNIX_COMMAND_FORMAT = "/etc/init.d/{0} {1}";
        ServiceStatus status;
        SshClient client;

        public ServiceStatus Status 
        {
            get { return status; }
            private set { status = value; }
        }

        public String serviceName
        {
            set { _serviceName = value; }
        }

        string username;
        string password;
        string host;

        public JvCacheServiceOnUnix(SshClient sshClient)
        {
            this.client = sshClient;
        }

        public void Start(int timeout)
        {
            if (!client.IsConnected)
                throw new Exception("Could not contact TayzGrid services on target machine.");

            Status = GetServiceStatus(client);

            if (Status == ServiceStatus.Running)
                return;

            if (Status == ServiceStatus.DoesNotExist)
                throw new Exception("Either TayzGrid services does not exist on target machine or permission denied to start service. Contact your administrator to control this behavior.");

            AsyncCallback callback = new AsyncCallback(ICommandResult);
            SshCommand command = client.CreateCommand("");
            ExecuteAsyncCommand(client, ServiceCommands.START, callback, command);
            System.Threading.Thread.Sleep(timeout * 1000);

            int retry = 0;
            while (retry < 2)
            {
                Status = GetServiceStatus(client);
                if (Status == ServiceStatus.Running)
                    return;

                retry++;
                Thread.Sleep(timeout * 1000);
            }
            command.Dispose();
            throw new Exception("Unable to start service in the given timeout period.");
        }

        public void Stop(int timeout)
        {
            if (!client.IsConnected)
                throw new Exception("Could not contact TayzGrid services on target machine.");

            Status = GetServiceStatus(client);

            if (Status == ServiceStatus.Stopped)
                return;

            if (Status == ServiceStatus.DoesNotExist)
                throw new Exception("Could not contact TayzGrid services on target machine");

            AsyncCallback callback = new AsyncCallback(ICommandResult);
            SshCommand command = client.CreateCommand("");
            ExecuteAsyncCommand(client, ServiceCommands.STOP, callback, command);
            System.Threading.Thread.Sleep(timeout * 500);

            int retry = 0;
            while (retry < 2)
            {
                Status = GetServiceStatus(client);
                if (Status == ServiceStatus.Stopped)
                    return;

                retry++;
                Thread.Sleep(timeout * 500);
            }
            command.Dispose();
            throw new Exception("Unable to stop services in the given timeout period.");
        }


        public bool isRunning()
        {
            bool isRunnig = false;
            if (client != null && client.IsConnected)
                isRunnig = false;
            using (client)
            {
                Status = GetServiceStatus(client);

                if (Status == ServiceStatus.Running)
                    isRunnig = true;
                else
                {
                    isRunnig = false;
                }
            }
            return isRunnig;           
        }

        ServiceStatus GetServiceStatus(SshClient client)
        {
            string status = ExecuteSyncCommand(client, ServiceCommands.STATUS);
            if (status.ToLower().Contains("in-active"))
                return ServiceStatus.Stopped;
            else if (status.ToLower().Contains("active"))
                return ServiceStatus.Running;

            return ServiceStatus.DoesNotExist;
        }
        public void Restart()
        {
            Restart(4, 3);
        }

        public void Restart(int timeoutStart, int timeoutStop)
        {
            Stop(6);//3 seconds as timeout
            Start(6); // 4 seconds as timeout
        }

        public void ICommandResult(IAsyncResult result)
        {
        }  

        string ExecuteSyncCommand(SshClient client, string action)
        {
            SshCommand command = client.CreateCommand(String.Format(UNIX_COMMAND_FORMAT, _serviceName, action, username, password));
            return command.Execute();
        }

        void ExecuteAsyncCommand(SshClient client, string action, AsyncCallback callBack, SshCommand command)
        {
            command = client.CreateCommand(String.Format(UNIX_COMMAND_FORMAT, _serviceName, action), UTF8Encoding.UTF8);
            command.BeginExecute(String.Format(UNIX_COMMAND_FORMAT, _serviceName, action), callBack, null);
        }

        public void Dispose()
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }
        }
    }
}

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
namespace Alachisoft.NCache.Management.ServiceControl
{
    public class Credentials
    {
        string host;
        string userName;
        string password;
        int sshPort;
        bool connectionFailed;

        public string Host 
        {
            get { return host; }
            set { host = value; }
        }
        public string Username
        {
            get { return userName; }
            set { userName = value; }
        }
        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        public int SSHPort
        {
            get { return sshPort; }
            set { sshPort = value; }
        }
        public bool ConnectionFailed
        {
            get { return connectionFailed; }
            set { connectionFailed = value; }
        }

        public Credentials(string host, int port, string username, string password)
        {
            this.Host = host;
            this.Username = username;
            this.Password = password;
            this.SSHPort = port;
        }
    }
}
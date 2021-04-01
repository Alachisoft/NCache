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

namespace Alachisoft.NCache.Management.ServiceControl
{
    public class CredentialsEventArgs : EventArgs
    {
        string nodeName;
        string userName;
        string password;
        int sshPort;
        bool cancel;
        FailureReason failureReason;

        public string NodeName 
        {
            get { return nodeName; }
            set { nodeName = value; }
        }

        public string UserName
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
        public bool Cancel
        {
            get { return cancel; }
            set { cancel = value; }
        }
        public FailureReason FailureReason
        {
            get { return failureReason; }
            set { failureReason = value; }
        }
    }
}
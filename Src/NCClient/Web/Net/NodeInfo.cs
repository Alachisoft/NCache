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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Holds the information about the cluster member nodes. It identifies 
    /// each member of the cluster uniquely with a combination of the IP Address
    /// and port.
    /// An instance of this class can not be instantiated. When client applications
    /// register the events <see cref="Alachisoft.NCache.Client.MemberJoinedCallback"/>
    /// or <see cref="Alachisoft.NCache.Client.MemberLeftCallback"/>, an instance of 
    /// NodeInfo is passed in the notification.
    /// </summary>
    public class NodeInfo
    {
        private System.Net.IPAddress _ip;
        private int _port;

        internal NodeInfo(System.Net.IPAddress ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        /// <summary>
        /// IPAddress of the node joining / leaving the cluster.
        /// </summary>
        public System.Net.IPAddress IpAddress
        {
            get { return _ip; }
        }

        /// <summary>
        /// Port, the member uses for the cluster-wide communication.
        /// </summary>
        public int Port
        {
            get { return _port; }
        }

        /// <summary>
        /// provides the string representation of NodeInfo.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (_ip == null)
                sb.Append("<null>");
            else
            {
                string host_name = _ip.ToString();
                appendShortName(host_name, sb);
            }

            sb.Append(":" + _port);

            return sb.ToString();
        }

        /// <summary> Input: "daddy.nms.fnc.fujitsu.com", output: "daddy". Appends result to string buffer 'sb'.</summary>
        /// <param name="hostname">The hostname in long form. Guaranteed not to be null
        /// </param>
        /// <param name="sb">The string buffer to which the result is to be appended
        /// </param>
        private void appendShortName(string hostname, System.Text.StringBuilder sb)
        {
            int index = hostname.IndexOf((System.Char)'.');

            if (hostname == null)
                return;
            if (index > 0 && !System.Char.IsDigit(hostname[0]))
                sb.Append(hostname.Substring(0, (index) - (0)));
            else
                sb.Append(hostname);
        }
    }
}

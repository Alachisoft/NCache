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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Provide connection information for the client to the server node in cache.
    /// </summary>
    public class ServerInfo : IComparable
    {
        int _port = 9800;
        string _name;
        IPAddress _ipAddress;
        short _portRange = 1;
        short _priority = 0;
        bool _userProvided;

        /// <summary>
        /// Initializes a new instance of ServerInfo.
        /// </summary>
        /// <param name="name">Specifies name of the server node where cache is running.</param>
        /// <param name="port">Specifies port for client to connect to the server node.</param>
        public ServerInfo(string name, int port = 9800)
        {
            Name = name;
            _port = port;
        }

        /// <summary>
        /// Initializes new instance of ServerInfo.
        /// </summary>
        /// <param name="ip">Specifies <see cref="IPAddress"/> of the server node where cache is running.</param>
        /// <param name="port">Specifies port for client to connect to the server node.</param>
        public ServerInfo(IPAddress ip, int port = 9800)
        {
            IP = ip;
            _port = port;
        }

        internal ServerInfo() { }

        /// <summary>
        /// Port for client to connect to the server node.
        /// </summary>
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        internal short PortRange
        {
            get { return _portRange; }
            set { if (_portRange > 0) _portRange = value; }
        }

        /// <summary>
        /// Priority for client connection to the server node.
        /// </summary>
        public short Priority
        {
            get { return _priority; }
            set { if (value > 0) _priority = value; }
        }

        internal bool IsUserProvided
        {
            get { return _userProvided; }
            set { _userProvided = value; }
        }

        /// <summary>
        /// Name of the server node where cache is running.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == string.Empty) return;
                if (value != null && value.ToLower() == "localhost")
                    _name = System.Environment.MachineName;
                else
                    _name = value;
            }
        }
        // added this property to return a string version of IP address

        internal string IpString
        {
            get
            {
                if (IP == null)
                {
                    return string.Empty;
                }
                else
                    return IP.ToString();
            }
        }

        /// <summary>
        /// IPAddress of the server node where cache is running.
        /// </summary>
        public IPAddress IP
        {
            get
            {
                if (_ipAddress == null)
                {
                    try
                    {
                        if (_name != null)
                        {
                            try
                            {
                                _ipAddress = IPAddress.Parse(_name);
                                return _ipAddress;
                            }
                            catch (Exception)
                            {
                            }

                            IPHostEntry entry = Dns.Resolve(_name);
                            if (entry != null) _ipAddress = entry.AddressList[0];
                        }
                    }
                    catch (Exception) { }
                }
                return _ipAddress;
            }
            set
            {
                _ipAddress = value;
            }
        }

        /// <summary>
        /// Compares two ServerInfo instances.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return CompareReal(obj) == 0 ? true : false;
        }

        /// <summary>
        /// Converts the value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string ip = IP != null ? IP.ToString() : "";
            return ip + ":" + _port;
        }

        internal int CompareReal(object o)
        {
            int h1 = 0, h2 = 0, rc;

            if ((o == null))
                return 1;
            ServerInfo other = o as ServerInfo;
            if (other == null) return 1;
            if (IP == null)
                if (other.IP == null)
                    return _port < other.Port ? -1 : (_port > other.Port ? 1 : 0);
                else
                    return -1;

            h1 = IP.GetHashCode();
            if (other.IP != null)
                h2 = other.IP.GetHashCode();
            rc = h1 < h2 ? -1 : (h1 > h2 ? 1 : 0);
            return rc != 0 ? rc : (_port < other.Port ? -1 : (_port > other.Port ? 1 : 0));
        }

        #region IComparable Members

        /// <summary>
        /// Compares the ServerInfo on the basis of priority
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>0 if equals, -1 if lesser and 1 if greater than the comparing serverInfo</returns>
        public int CompareTo(object obj)
        {
            int result = 0;
            if (obj != null && obj is ServerInfo)
            {
                ServerInfo other = (ServerInfo)obj;
                if (other.Priority > _priority)
                    result = -1;
                else if (other.Priority < _priority)
                    result = 1;
            }
            return result;
        }

        #endregion
    }
}
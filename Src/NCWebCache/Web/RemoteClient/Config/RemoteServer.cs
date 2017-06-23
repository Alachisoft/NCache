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
using System.Net;

namespace Alachisoft.NCache.Web.RemoteClient.Config
{
    class RemoteServer : IComparable
    {
        int _port;
        string _name;
        IPAddress _ipAddress;
        short _portRange = 1;
        short _priority = 0;
        bool _userProvided;

        public RemoteServer(string name, int port)
        {
            Name = name;
            _port = port;
        }

        public RemoteServer(IPAddress ip, int port)
        {
            IP = ip;
            _port = port;
        }
        public RemoteServer() { }

        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public short PortRange
        {
            get { return _portRange; }
            set { if (_portRange > 0) _portRange = value; }
        }

        public short Priority
        {
            get { return _priority; }
            set { if (value > 0) _priority = value; }
        }

        public bool IsUserProvided
        {
            get { return _userProvided; }
            set { _userProvided = value; }
        }

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

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return CompareReal(obj) == 0 ? true : false;
        }
        public override string ToString()
        {
            string ip = IP != null ? IP.ToString() : "";
            return ip + ":" + _port;
        }

        public int CompareReal(object o)
        {
            int h1 = 0, h2 = 0, rc;

            if ((o == null))
                return 1;
            RemoteServer other = o as RemoteServer;
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

        public int CompareTo(object obj)
        {
            int result = 0;
            if (obj != null && obj is RemoteServer)
            {
                RemoteServer other = (RemoteServer)obj;
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

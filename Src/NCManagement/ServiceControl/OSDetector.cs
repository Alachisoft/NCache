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
using System.Net.NetworkInformation;
using System.Net;
using Alachisoft.NCache.Common;

namespace UnixServiceController
{
    public class OSDetector
    {
        public static string hostIP; 
               
        public static OSInfo DetectOS(string host)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply;
                IPAddress[] hostAddresses;

                if (CheckIfIP(host))
                {
                    reply = ping.Send(host);
                    hostIP = host;
                }
                else
                {
                    hostAddresses = Dns.GetHostAddresses(host);
                    hostIP = GetAppropriateAddress(hostAddresses).ToString(); 
                    reply = ping.Send(GetAppropriateAddress(hostAddresses));
                }

                if (reply.Status == IPStatus.Success)
                {
                    if (reply.Options.Ttl > 64 && reply.Options.Ttl <= 128) //for windows, default TTL value is 128 or (128 - N) where N is the number of hops (routers in between)
                        return OSInfo.Windows;

                    if (reply.Options.Ttl <= 64)// for unix, default TTL is 64 or (64 - N)
                        return OSInfo.Linux;
                }

                return OSInfo.Unknown;
            }
            catch (Exception ex)
            {
                return OSInfo.Unknown;
            }            
        }

        private static bool CheckIfIP(string host)
        {
            
            string[] parts = host.Split('.');
            if (parts.Length < 4)
            {
                return false;
            }
            else
            {
                foreach (string part in parts)
                {
                    byte checkPart = 0;
                    if (!byte.TryParse(part, out checkPart))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private static IPAddress GetAppropriateAddress(IPAddress[] ipaddresses)
        {
            foreach (IPAddress ipAddress in ipaddresses)
            {
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ipAddress;
                }
            }

            return null;
        }        
    }
}

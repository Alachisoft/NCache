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
using System.Net.Sockets;

namespace Alachisoft.NCache.Common.Net
{
    public class Helper
    {
        public static bool isTCPPortFree(int port, IPAddress addr)
        {
            TcpListener ss = null;
            try
            {
                ss = new TcpListener(IPAddress.Any, port);
                ss.Start();
                return true;
            }
            catch (Exception io)
            {
                return false;

            }
            finally
            {
                if (ss != null)
                {
                    try
                    {
                        ss.Stop();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        public static bool isPortFree(int port, IPAddress addr)
        {
            return isTCPPortFree(port, addr);
        }

      
    }
}

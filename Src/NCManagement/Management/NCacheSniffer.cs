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
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Alachisoft.NCache.Management
{
    public  class NCacheSniffer
    {
        private const int DEFAULT_LISTENING_PORT = 8252;
        private UdpClient listener;

        public NCacheSniffer()
        {
            listener = new UdpClient(DEFAULT_LISTENING_PORT);
        }

        public void Start()
        {
            bool done = false;

            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, DEFAULT_LISTENING_PORT);

            try
            {
             
                while (System.Threading.Thread.CurrentThread.IsAlive )
                {
                    byte[] bytes = listener.Receive(ref groupEP);
                    String strMsg = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                   
                    if (strMsg.CompareTo("R_U_NCACHE_SERVER") == 0)
                    {
                        strMsg = "I_M_NCACHE_SERVER";
                        bytes = Encoding.ASCII.GetBytes(strMsg);
                        listener.Send(bytes, bytes.Length, new IPEndPoint(groupEP.Address, groupEP.Port));
                    }
                    
                }

            }
            catch (Exception e)
            {
                if (e.Message.Equals("Thread was being aborted."))
                { }
                else
                { }// Console.WriteLine(e.ToString());
            }
            finally
            {
                listener.Close();
            }
        }

        public void Stop()
        {
            if (listener != null)
                listener.Close();
        }

    }
}

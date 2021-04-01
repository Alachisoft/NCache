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

using System.Net;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// 
    /// </summary>
    internal class AddressUtil
    {
        bool _isLocalAddress = false;

        internal bool IsLocaclAddres
        {
            get { return _isLocalAddress; }
        }

        internal bool VerifyLocaclAddress(IPAddress address)
        {
            try
            {
                string strHostName = Dns.GetHostName().ToString();
                IPHostEntry ipHostEntry = Dns.GetHostByName(strHostName);
                foreach (IPAddress ipAddress in ipHostEntry.AddressList)
                {
                    if (address.ToString() == ipAddress.ToString() || IPAddress.IsLoopback(address))
                    {
                        _isLocalAddress = true;
                        break;
                    }

                }

                return _isLocalAddress;
            }
            catch
            {
                return true;
            }
        }
    }
}

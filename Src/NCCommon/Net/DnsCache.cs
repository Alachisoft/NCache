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
using System.Collections;
using System.Net;
using System.Net.Sockets;

namespace Alachisoft.NCache.Common.Net
{
	/// <summary>
	/// Summary description for DnsCache.
	/// </summary>
	public class DnsCache
	{
		/// <summary> map for forward lookups </summary>
		private static Hashtable	_fwdMap;
		/// <summary> map for reverse lookups </summary>
		private static Hashtable	_bckMap;

		static DnsCache()
		{
			_fwdMap = Hashtable.Synchronized(new Hashtable(11));
			_bckMap = Hashtable.Synchronized(new Hashtable(11));
		}

		/// <summary>
		/// Does a DNS lookup on the hostname. updates the reverse cache to optimize reverse lookups.
		/// </summary>
		/// <param name="hostname"></param>
		/// <returns></returns>
		public static IPAddress ResolveName(string hostname)
		{
			hostname = hostname.ToLower();
			if(!_fwdMap.ContainsKey(hostname))
			{
#if !NETCORE
                IPHostEntry ie = Dns.GetHostByName(hostname);
				if(ie != null && ie.AddressList.Length > 0)
				{
					lock(_fwdMap.SyncRoot)
					{
						_fwdMap[hostname] = ie.AddressList[0];
						_bckMap[ie.AddressList[0]] = hostname;
					}
				}
#elif NETCORE
                if (!String.IsNullOrEmpty(hostname))
                {
                    IPAddress[] ipv4Addresses = Array.FindAll(Dns.GetHostByName(hostname).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                    if (ipv4Addresses != null && ipv4Addresses.Length > 0)
                    {
                        lock (_fwdMap.SyncRoot)
                        {
                            _fwdMap[hostname] = ipv4Addresses[0];
                            _bckMap[ipv4Addresses[0]] = hostname;
                        }
                    }
                }
                else
                {

                    IPAddress[] ipv4Addresses = Array.FindAll(Dns.GetHostEntry(hostname).AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
                    if (ipv4Addresses != null && ipv4Addresses.Length > 0)
                    {
                        lock (_fwdMap.SyncRoot)
                        {
                            _fwdMap[hostname] = ipv4Addresses[0];
                            _bckMap[ipv4Addresses[0]] = hostname;
                        }
                    }
                }

#endif
            }

            return _fwdMap[hostname] as IPAddress;
		}


		/// <summary>
		/// Does a reverse DNS lookup on the address. updates the forward cache to optimize lookups.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static string ResolveAddress(string address)
		{
			try
			{
				return ResolveAddress(IPAddress.Parse(address));
			}
			catch (Exception ex)
			{
			}
			return null;
		}


		/// <summary>
		/// Does a reverse DNS lookup on the address. updates the forward cache to optimize lookups.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static string ResolveAddress(IPAddress address)
		{
			if(!_bckMap.ContainsKey(address))
			{
				IPHostEntry ie = Dns.GetHostByAddress(address);
				if(ie != null && ie.AddressList.Length > 0)
				{
					string hostname = ie.HostName.ToLower();
					hostname = hostname.Replace("is~","");
					if(hostname.IndexOf('.') > 0)
						hostname = hostname.Substring(0, hostname.IndexOf('.'));

					lock(_fwdMap.SyncRoot)
					{
						_bckMap[address] = hostname;//ie.HostName.ToLower();
						_fwdMap[ie.HostName.ToLower()] = address;
					}
				}
			}

			return _bckMap[address] as string;
		}

        public static IPAddress Resolve(string addr)
        {
            IPAddress ip = null;
            try
            {
                ip = IPAddress.Parse(addr);
            }
            catch (Exception)
            {
                ip = ResolveName(addr);
            }
            return ip;
        }
		/// <summary>
		/// Clears the caches 
		/// </summary>
		public static void FlushCache()
		{
			lock(_fwdMap.SyncRoot)
			{
				_fwdMap.Clear();
				_bckMap.Clear();
			}
		}
	}
}

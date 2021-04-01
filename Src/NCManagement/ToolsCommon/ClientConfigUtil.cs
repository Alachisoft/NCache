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
using System.Net;
using System.Collections;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Config.NewDom;

namespace Alachisoft.NCache.ToolServerOperations
{
    public class ClientConfigUtil
    {
        static private NCacheRPCService NCache = new NCacheRPCService("");

        public Dictionary<int, Management.ClientConfiguration.Dom.CacheServer> GetPrioritizedServerListForClient(string clientNode, string clusterId, ArrayList _nodeList)
        {
            int priority = 0;
            string ClientServerName = "";
            Dictionary<int, Management.ClientConfiguration.Dom.CacheServer> serversPriorityList = new Dictionary<int, Management.ClientConfiguration.Dom.CacheServer>();
            foreach (ServerNode serverNode in _nodeList)
            {
                Management.ClientConfiguration.Dom.CacheServer server = new Management.ClientConfiguration.Dom.CacheServer();
                try
                {
                    NCache.ServerName = serverNode.IP;
                    ICacheServer _cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    Hashtable bindedIps = _cacheServer.BindedIp().Map;
                    if (bindedIps.Contains(CacheServer.Channel.SocketServer))
                        ClientServerName = bindedIps[CacheServer.Channel.SocketServer].ToString();

                    if (!string.IsNullOrEmpty(ClientServerName))
                        server.ServerName = ClientServerName;
                    else
                        server.ServerName = serverNode.IP;
                }
                catch (Exception ex)
                {
                    ClientServerName = serverNode.IP;
                    server.ServerName = serverNode.IP;
                }

                server.Priority = priority;
                serversPriorityList[priority++] = server;
            }

            serversPriorityList = BringLocalServerToFirstPriority(clientNode, serversPriorityList);
            return serversPriorityList;
        }
       
        public Dictionary<int, Management.ClientConfiguration.Dom.CacheServer> BringLocalServerToFirstPriority(string localNode, Dictionary<int, Management.ClientConfiguration.Dom.CacheServer> serversPriorityList)
        {
            Dictionary<int, Management.ClientConfiguration.Dom.CacheServer> tempList = new Dictionary<int, Management.ClientConfiguration.Dom.CacheServer>();
            int localServerPriority = 0;
            bool localServerFound = false;
            NCache.ServerName = localNode;
            ICacheServer sw = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
            string nodeName = localNode;
            Hashtable temp = sw.GetNodeInfo().Map;
            string server = temp[Alachisoft.NCache.Management.CacheServer.Channel.SocketServer] as string;
            IPAddress serverAddress = null;
            if (IPAddress.TryParse(server, out serverAddress))
                nodeName = server;

            foreach (KeyValuePair<int, Management.ClientConfiguration.Dom.CacheServer> pair in serversPriorityList)
            {
                string serverName = pair.Value.ServerName.ToLower();
                if (serverName.CompareTo(nodeName.ToLower()) == 0)
                {
                    localServerFound = true;
                    localServerPriority = pair.Key;
                    break;
                }
            }

            if (localServerFound)
            {
                tempList.Add(0, serversPriorityList[localServerPriority]);

                int priority = 1;
                foreach (KeyValuePair<int, Management.ClientConfiguration.Dom.CacheServer> pair in serversPriorityList)
                {
                    if (pair.Key != localServerPriority)
                        tempList.Add(priority++, pair.Value);
                }

                serversPriorityList = tempList;
            }

            return serversPriorityList;
        }
	
    }
}

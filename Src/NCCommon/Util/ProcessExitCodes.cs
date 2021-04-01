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
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.Util
{
    public static class ProcessExitCodes
    {
        const string CONFIG_NOT_VALID = "Provided config.conf is not valid cache configurations.";
        const string MULTIPLE_CONFIGS = "Multiple cache configurations provided in config.conf.";
        const string PATH_INVALID = "Cache configuration [config.conf] path is not provided or File does not exist.";
        const string CACHE_NOT_EXIST = "Provided cache is not specified in cache.conf.";
        const string CLUSTER_IP = "Invalid BindToIP address specified.";
        const string CLIENT_SERVER_IP = "Invalid BindToClientServerIP address specified";
        const string SOCKET_CONNECTION = "Client listener can not be started. The port might already be in use";
        const string MANAGEMENT_CONNECTION = "Management listener can not be started. The port might already be in use";
        const string SOCKET_PORT_RANGE = "The port is outside the range of valid values for Socket.";
        const string MANAGEMENT_PORT_RANGE = "The port is outside the range of valid values for Management.";
        const string GENERAL_EXCEPTION = "An Exception has occurred while starting Separate Process. Please refer to Windows Event Logs for details.";
        
        public static Dictionary<int, string> list = new Dictionary<int, string>();
           
        static ProcessExitCodes()
        {
            list.Add(500, GENERAL_EXCEPTION);
            list.Add(1, CONFIG_NOT_VALID);
            list.Add(2, MULTIPLE_CONFIGS);
            list.Add(3, PATH_INVALID);
            list.Add(4, CACHE_NOT_EXIST);
            list.Add(5, CLUSTER_IP);
            list.Add(6, CLIENT_SERVER_IP);
            list.Add(7, SOCKET_CONNECTION);
            list.Add(8, MANAGEMENT_CONNECTION);
            list.Add(9, SOCKET_PORT_RANGE);
            list.Add(10, MANAGEMENT_PORT_RANGE);
        }
        
    }
}

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
    /// Enumeration that defines the runtime status of connection.
    /// </summary>
    internal class ConnectionStatus
    {
        /// <summary> The connection is in initialization phase.</summary>
        public const byte Connecting = 1;
        /// <summary> The connection is fully functional. </summary>
        public const byte Connected = 2;
        /// <summary> The connection is disconnected. </summary>
        public const byte Disconnected = 4;
        /// <summary> This is in load balance state so dont wont queue up new request </summary>
        public const byte LoadBalance = 8;
    }
}

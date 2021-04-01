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
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.Maintenance
{
    /// <summary>
    /// ClusterState specifies whether state transfer can be triggered on cluster or needs to wait
    /// Used for maintenance only
    /// </summary>
    internal class MaintenanceStatus
    {
        //specifies that the cluster is active and state transfer can be triggered
        public const byte PerformStateTransfer = 1;

        //specifies that the cluster is under maintenace and state transfer cannot be triggered
        public const byte WaitForMaintenance = 2;

        //specifies that the cluster is under maintenace and state transfer cannot be triggered
        public const byte PerformReplication = 4;
    }
}

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
namespace Alachisoft.NCache.Common.DataStructures
{
    /// <summary>
    /// This enum represents the status of the state transfer.
    /// </summary>
    public enum StateTransferStatus : byte
    {
        /// <summary>State transfer has not started yet.</summary>
        NEEED_STATE_TRANSFER = 1,

        /// <summary>State transfer is in progress.</summary>
        UNDER_STATE_TRANSFER,

        /// <summary>State transfer has completed.</summary>
        STATE_TRANSFER_COMPLETED,

        /// <summary>State Transfer is not required as source cache is not bridge coordinator cache.</summary>
        NO_NEED_FOR_STATE_TRANSFER
    }
}
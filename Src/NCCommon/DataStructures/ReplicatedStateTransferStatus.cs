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
    public class ReplicatedStateTransferStatus
    {
        /// <summary>State transfer is in progress.</summary>
        public const int UNDER_STATE_TRANSFER = 1;

        /// <summary>State transfer has completed.</summary>
        public const int STATE_TRANSFER_COMPLETED = 2;
      
    }
}
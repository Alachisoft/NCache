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
namespace Alachisoft.NCache.Storage
{
    /// <summary>
    /// Staus of the Store.
    /// </summary>

    public enum StoreStatus
    {
        /// <summary> Store has space.</summary>
        HasSpace,

        /// <summary> Store is almost full,but can accomadate some data. </summary>
        NearEviction,

        /// <summary> Store has no space to accomodate new data.</summary>
        HasNotEnoughSpace
    }
}
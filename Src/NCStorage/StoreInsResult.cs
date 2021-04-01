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
    /// Enumeration that defines the result of a Put operation.
    /// </summary>
    public enum StoreInsResult
    {
        /// <summary> The item was inserted. </summary>
        Success,

        /// <summary> The item was updated. </summary>
        SuccessOverwrite,


        /// <summary> The operation is successfull but storage is almost full.</summary>
        SuccessNearEviction,


        /// <summary> The operation is successfull but storage is almost full.</summary>
        SuccessOverwriteNearEviction,


        /// <summary> The operation failed, since there is not enough space. </summary>
        NotEnoughSpace,

        /// <summary> The operation failed. </summary>
        Failure
    }
}
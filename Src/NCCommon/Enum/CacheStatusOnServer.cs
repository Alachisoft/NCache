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
namespace Alachisoft.NCache.Common.Enum
{
    /// <summary>
    /// removenode and addnode utilites work with the clusteredcaches
    /// only. we need to find the status of the given cache id on the 
    /// server. this enumeration is used for the purpose.
    /// </summary>
    public enum CacheStatusOnServer
    {
        Registered,
        Unregistered,
        ClusteredCache,
        LocalCache,
        MirrorCache
    }
}
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
    /// An enum demonstrating the manner of containment of data by the Json class.
    /// </summary>
    public enum JsonValueType : byte
    {
        /// <summary>
        /// Contained value is in raw value form. 
        /// This is usually for InProc caches.
        /// </summary>
        Object,
        /// <summary>
        /// Contained value is in binary form. 
        /// This is usually for OutProc caches.
        /// </summary>
        Binary,
        /// <summary>
        /// Contained value is possibly in partially 
        /// binary form because there was an attempt 
        /// made to deserialize it on the cache server.
        /// </summary>
        Json
    }
}

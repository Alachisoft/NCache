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
    /// Defines the level of 
    /// logging you want to use.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Info level describes some useful information about any operation performed on cache.
        /// </summary>
        Info,
        /// <summary>
        /// This log flag gives the cause of errors that are raised during operation execution.
        /// </summary>
        Error,
        /// <summary>
        /// This log option prints detailed information about any operations in cache.
        /// </summary>
        Debug
    }
}

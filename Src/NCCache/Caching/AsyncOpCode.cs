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

namespace Alachisoft.NCache.Caching
{
    /// <summary>
    /// A class to contain cache creation parameters.
    /// </summary>

    /// <summary>
    /// opcodes to identify the async operations.
    /// </summary>
    [Serializable]
    public enum AsyncOpCode
    {
        Add,
        Update,
        Remove,
        Clear
    }
}
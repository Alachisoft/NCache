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
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// A delegate that is responsible for reading an object from the stream reader. A fresh instance 
    /// of the object is passed as parameter. The surrogate should populate fields in the object from 
    /// data on the stream
    /// </summary>
    /// <remarks>
    /// A <see cref="ReadObjectDelegate"/> is used with an DynamicValueTypeSurrogate or
    /// DynamicRefTypeSurrogate
    /// </remarks>
    /// <param name="reader">stream reader</param>
    /// <param name="graph">a fresh instance of the object that the surrogate must deserialize</param>
    /// <returns>object read from the stream reader</returns>
    public delegate object ReadObjectDelegate(CompactBinaryReader reader, object graph);
}
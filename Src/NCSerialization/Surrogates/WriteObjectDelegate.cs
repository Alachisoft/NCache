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
    /// A delegate that is responsible for writing an object to the stream
    /// </summary>
    /// <remarks>
    /// A <see cref="WriteObjectDelegate"/> is used with an DynamicValueTypeSurrogate or
    /// DynamicRefTypeSurrogate
    /// </remarks>
    /// <param name="writer">stream writer</param>
    /// <param name="graph">object to be written to the stream reader</param>
    public delegate void WriteObjectDelegate(CompactBinaryWriter writer, object graph);
}
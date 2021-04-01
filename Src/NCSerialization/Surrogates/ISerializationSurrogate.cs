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
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// Interface that defines methods to be implemented by a serialization surrogate.
    /// </summary>
    public interface ISerializationSurrogate
    {
        /// <summary> 
        /// Return the type of object for which this object is a surrogate. 
        /// </summary>
        Type ActualType { get; }

        /// <summary> 
        /// Type handle associated with the type provided by the <see cref="TypeSurrogateSelector"/> 
        /// </summary>
        short TypeHandle { get; set; }

        /// <summary>
        /// Sub Type handle associated with the type provided by the <see cref="TypeSurrogateSelector"/> 
        /// </summary>
        short SubTypeHandle { get; set;}

        /// <summary>
        /// Object pool for item to fetch new instances from instead of creating new.
        /// </summary>
        IObjectPool ObjectPool { get; set; }

        /// <summary>
        /// Read an object of type <see cref="ActualType"/> from the stream reader
        /// </summary>
        /// <param name="reader">stream reader</param>
        /// <returns>object read from the stream reader</returns>
        object Read(CompactBinaryReader reader);

        /// <summary>
        /// Write an object of type <see cref="ActualType"/> to the stream writer
        /// </summary>
        /// <param name="writer">stream writer</param>
        /// <param name="graph">object to be written to the stream reader</param>
        void Write(CompactBinaryWriter writer, object graph);

        /// <summary>
        /// Skips an object of type <see cref="ActualType"/> from the stream reader
        /// </summary>
        /// <param name="reader">stream reader</param>
        void Skip(CompactBinaryReader reader);
    }
}
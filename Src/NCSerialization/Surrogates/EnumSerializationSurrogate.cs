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
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    /// <summary>
    /// Surrogate for <see cref="System.Enum"/> derived types.
    /// </summary>
    sealed class EnumSerializationSurrogate : SerializationSurrogate
    {
        public EnumSerializationSurrogate(Type enm) : base(enm, null) { }
        public override object Read(CompactBinaryReader reader)
        {
            // Find an appropriate surrogate by handle
            short handle = reader.ReadInt16();
            ISerializationSurrogate typeSurr = TypeSurrogateSelector.GetSurrogateForTypeHandle(handle,reader.Context.CacheContext);

            if (typeSurr == null)
            {
                typeSurr = TypeSurrogateSelector.GetSurrogateForSubTypeHandle(handle, reader.ReadInt16(), reader.Context.CacheContext);
            }

            return Enum.ToObject(ActualType, typeSurr.Read(reader));
        }
        public override void Write(CompactBinaryWriter writer, object graph)
        {
            Type enumType = Enum.GetUnderlyingType(ActualType);
            ISerializationSurrogate typeSurr = TypeSurrogateSelector.GetSurrogateForType(enumType,writer.Context.CacheContext);
            writer.Write(typeSurr.TypeHandle);
            typeSurr.Write(writer, graph);
        }

        public override void Skip(CompactBinaryReader reader)
        {
            // Find an appropriate surrogate by handle
            short handle = reader.ReadInt16();
            ISerializationSurrogate typeSurr = TypeSurrogateSelector.GetSurrogateForTypeHandle(handle, reader.Context.CacheContext);
            if (typeSurr == null)
            {
                typeSurr = TypeSurrogateSelector.GetSurrogateForSubTypeHandle(handle, reader.ReadInt16(), reader.Context.CacheContext);
            }
            typeSurr.Skip(reader);
        }
    }
}
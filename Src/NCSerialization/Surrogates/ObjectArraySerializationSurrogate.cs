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
    /// Surrogate for <see cref="System.object[]"/> type.
    /// </summary>
    sealed class ObjectArraySerializationSurrogate : ContextSensitiveSerializationSurrogate
    {
        public ObjectArraySerializationSurrogate() : base(typeof(object[]), null) { }

        public override object Instantiate(CompactBinaryReader reader)
        {
            int length = reader.ReadInt32();
            return new object[length];
        }

        public override object ReadDirect(CompactBinaryReader reader, object graph)
        {
            object[] array = (object[])graph;

            short handle = reader.ReadInt16();
            ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForTypeHandle(handle, reader.CacheContext);

            if (surrogate == null)
            {
                surrogate = TypeSurrogateSelector.GetSurrogateForSubTypeHandle(handle, reader.ReadInt16(), reader.Context.CacheContext);
            }

            Object obj = Array.CreateInstance(surrogate.ActualType, array.Length);

            for (int i = 0; i < array.Length; i++)
                ((Array)obj).SetValue(reader.ReadObject(), i);
            //array[i] = reader.ReadObject();
            
            return obj;
        }

        public override void WriteDirect(CompactBinaryWriter writer, object graph)
        {
            object[] array = (object[])graph;
            writer.Write(array.Length);

            if (!typeof(object[]).Equals(graph.GetType()))
            {
                object obj = null;
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] != null)
                    {
                        obj = array[i];
                        break;
                    }
                }
                ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForObject(obj, writer.CacheContext);
                writer.Write(surrogate.TypeHandle);
                if(surrogate.SubTypeHandle > 0)
                    writer.Write(surrogate.SubTypeHandle);
            }
            else
            {
                ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForObject(new object(), writer.CacheContext);
                writer.Write(surrogate.TypeHandle);
            }

            for (int i = 0; i < array.Length; i++)
                writer.WriteObject(array[i]);
        }

        public override void SkipDirect(CompactBinaryReader reader, object graph)
        {
            object[] array = (object[])graph;
            short handle = reader.ReadInt16();
            ISerializationSurrogate surrogate = TypeSurrogateSelector.GetSurrogateForTypeHandle(handle, reader.CacheContext);

            if (surrogate == null)
            {
                surrogate = TypeSurrogateSelector.GetSurrogateForSubTypeHandle(handle, reader.ReadInt16(), reader.Context.CacheContext);
            }

            for (int i = 0; i < array.Length; i++)
                reader.SkipObject();
        }
    }
}
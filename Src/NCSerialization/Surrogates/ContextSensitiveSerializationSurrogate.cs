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
    /// Surrogate for types that inherit from <see cref="ICompactSerializable"/>.
    /// </summary>
    public abstract class ContextSensitiveSerializationSurrogate : SerializationSurrogate
    {
        public ContextSensitiveSerializationSurrogate(Type t, IObjectPool pool) : base(t, pool) { }

        abstract public object ReadDirect(CompactBinaryReader reader, object graph);
        abstract public void WriteDirect(CompactBinaryWriter writer, object graph);
        abstract public void SkipDirect(CompactBinaryReader reader, object graph);

        virtual public object Instantiate(CompactBinaryReader reader)
        {
            return base.CreateInstance();
        }

        
        sealed public override object Read(CompactBinaryReader reader)
        {
            int cookie = reader.ReadInt32();
            object graph = reader.Context.GetObject(cookie);
            if (graph == null)
            {
                bool bKnown = false;
                graph = Instantiate(reader);
                if (graph != null)
                {
                    reader.Context.RememberObject(graph, false);
                    bKnown = true;
                }

                if (VersionCompatible)
                {
                    long startPosition = 0;
                    int dataLength = 0;
                    long endPosition = 0;
                    startPosition = reader.BaseReader.BaseStream.Position;
                    
                    dataLength = reader.ReadInt32();
                    reader.BeginCurrentObjectDeserialization(startPosition + dataLength);

                    graph = ReadDirect(reader, graph);

                    reader.EndCurrentObjectDeserialization();
                    if (dataLength != -1 && (endPosition - startPosition) < dataLength)
                    {
                        endPosition = reader.BaseReader.BaseStream.Position;
                        reader.SkipBytes((int)(dataLength - (endPosition - startPosition)));
                    }
                }
                else
                {
                    reader.BeginCurrentObjectDeserialization(reader.BaseReader.BaseStream.Length);
                    graph = ReadDirect(reader, graph);
                    reader.EndCurrentObjectDeserialization();
                }

                if (!bKnown)
                {
                    reader.Context.RememberObject(graph, false);
                }
            }
            return graph;
        }

        public override void Write(CompactBinaryWriter writer, object graph)
        {
            int cookie = writer.Context.GetCookie(graph);
            if (cookie != SerializationContext.INVALID_COOKIE)
            {
                writer.Write(cookie);
                return;
            }

            cookie = writer.Context.RememberObject(graph, true);
            writer.Write(cookie);

            if (VersionCompatible)
            {
                long startPosition = 0;
                long endPosition = 0;
                startPosition = writer.BaseWriter.BaseStream.Position;
                
                writer.Write((int)0);
                WriteDirect(writer, graph);

                endPosition = writer.BaseWriter.BaseStream.Position;
                writer.BaseWriter.BaseStream.Seek(startPosition, System.IO.SeekOrigin.Begin);
                writer.Write((int)(endPosition - startPosition));
                writer.BaseWriter.BaseStream.Seek(endPosition, System.IO.SeekOrigin.Begin);
            }
            else
            {
                WriteDirect(writer, graph);
            }
        }

        sealed public override void Skip(CompactBinaryReader reader)
        {
            int cookie = reader.ReadInt32();
            object graph = reader.Context.GetObject(cookie);
            if (graph == null)
            {
                graph = Instantiate(reader);
                SkipDirect(reader, graph);
            }
        }
    }
}
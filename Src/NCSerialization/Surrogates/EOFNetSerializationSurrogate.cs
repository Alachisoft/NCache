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
    sealed public class EOFNetSerializationSurrogate : SerializationSurrogate
    {
        public EOFNetSerializationSurrogate() : base(typeof(EOFNetSerializationSurrogate), null) { }
        public override object Read(CompactBinaryReader reader) { return reader.ReadInt16(); }
        public override void Write(CompactBinaryWriter writer, object graph) { writer.Write((short)graph); }
        public override void Skip(CompactBinaryReader reader) { reader.SkipInt16(); }
    }
}
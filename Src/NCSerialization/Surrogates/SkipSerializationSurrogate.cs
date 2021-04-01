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
    sealed public class SkipSerializationSurrogate : SerializationSurrogate
    {
        public SkipSerializationSurrogate() : base(typeof(SkipSerializationSurrogate), null) { }
        public override object Read(CompactBinaryReader reader) { /*returns default value*/return this; }
        public override void Write(CompactBinaryWriter writer, object graph) { return; /*Do nothing, write handle only */ }
        public override void Skip(CompactBinaryReader reader) { /*Do Nothing*/ }
    }
}
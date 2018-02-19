// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Serialization.Surrogates
{
    sealed public class AverageResultSerializationSurrogate : SerializationSurrogate
    {
        public AverageResultSerializationSurrogate() : base(typeof(AverageResult)) { }
        public override object Read(CompactBinaryReader reader) 
        {
            ISerializationSurrogate decimalSurrogate = TypeSurrogateSelector.GetSurrogateForType(typeof(decimal), null);

            AverageResult result = new AverageResult();
            result.Sum = (decimal)decimalSurrogate.Read(reader);
            result.Count = (decimal)decimalSurrogate.Read(reader);
            return result;
        }
        public override void Write(CompactBinaryWriter writer, object graph) 
        {
            ISerializationSurrogate decimalSurrogate = TypeSurrogateSelector.GetSurrogateForType(typeof(decimal), null);

            AverageResult result = (AverageResult)graph;
            decimalSurrogate.Write(writer, result.Sum);
            decimalSurrogate.Write(writer, result.Count);
        }
        public override void Skip(CompactBinaryReader reader) {
            ISerializationSurrogate decimalSurrogate = TypeSurrogateSelector.GetSurrogateForType(typeof(decimal), null);
            decimalSurrogate.Skip(reader);
            decimalSurrogate.Skip(reader);
        }
    }
}

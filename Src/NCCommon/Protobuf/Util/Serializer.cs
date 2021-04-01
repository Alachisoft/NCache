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
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Alachisoft.NCache.Common.Protobuf.Util
{
    public sealed class Serializer
    {
        public static void Serialize<T>(Stream stream, T obj)
        {
            ProtoBuf.Serializer.Serialize<T>(stream, obj);
        }

        public static void Serialize(Stream stream, object instance)
        {
            ProtoBuf.Serializer.NonGeneric.Serialize(stream, instance);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return ProtoBuf.Serializer.Deserialize<T>(stream);            
        }
    }
}

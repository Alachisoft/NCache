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

namespace Alachisoft.NCache.Common.Util
{
    public static class EnumerationPointerConversionUtil
    {
        public static Alachisoft.NCache.Common.DataStructures.EnumerationPointer GetFromProtobufEnumerationPointer(Alachisoft.NCache.Common.Protobuf.EnumerationPointer pointer)
        {
            Alachisoft.NCache.Common.DataStructures.EnumerationPointer enumerationPointer = new Alachisoft.NCache.Common.DataStructures.EnumerationPointer(pointer.id, pointer.chunkId);
            enumerationPointer.isDisposable = pointer.isDisposed;
            return enumerationPointer;
        }

        public static Alachisoft.NCache.Common.DataStructures.GroupEnumerationPointer GetFromProtobufGroupEnumerationPointer(Alachisoft.NCache.Common.Protobuf.GroupEnumerationPointer pointer)
        {
            Alachisoft.NCache.Common.DataStructures.GroupEnumerationPointer enumerationPointer = new Alachisoft.NCache.Common.DataStructures.GroupEnumerationPointer(pointer.id, pointer.chunkId, pointer.group, pointer.subGroup);
            return enumerationPointer;
        }

        public static Alachisoft.NCache.Common.Protobuf.EnumerationPointer ConvertToProtobufEnumerationPointer(Alachisoft.NCache.Common.DataStructures.EnumerationPointer pointer)
        {
            Alachisoft.NCache.Common.Protobuf.EnumerationPointer enumerationPointer = new Alachisoft.NCache.Common.Protobuf.EnumerationPointer();
            enumerationPointer.chunkId = pointer.ChunkId;
            enumerationPointer.id = pointer.Id;
            enumerationPointer.isDisposed = pointer.isDisposable;
            return enumerationPointer;
        }

        public static Alachisoft.NCache.Common.Protobuf.GroupEnumerationPointer ConvertToProtobufGroupEnumerationPointer(Alachisoft.NCache.Common.DataStructures.GroupEnumerationPointer pointer)
        {
            Alachisoft.NCache.Common.Protobuf.GroupEnumerationPointer enumerationPointer = new Alachisoft.NCache.Common.Protobuf.GroupEnumerationPointer();
            enumerationPointer.id = pointer.Id;
            enumerationPointer.chunkId = pointer.ChunkId;
            enumerationPointer.group = pointer.Group;
            enumerationPointer.subGroup = pointer.SubGroup;

            return enumerationPointer;
        }
    }
}

//  Copyright (c) 2019 Alachisoft
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
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.StateTransfer.LoggedOperations
{
    public class InsertLoggedOperation : LoggedOperationBase
    {
        public string Key { get; set; }
        public CacheEntry Entry { get; set; }

        public override LoggedOperationType OperationType
        {
            get { return LoggedOperationType.InsertOperation; }
        }

        public InsertLoggedOperation(string key, CacheEntry entry)
        {
            Key = key;
            Entry = entry;
        }

        public override void Deserialize(CompactReader reader)
        {
            Key = reader.ReadObject() as string;
            Entry = reader.ReadObject() as CacheEntry;
        }

        public override void Serialize(CompactWriter writer)
        {
            writer.WriteObject(Key);
            writer.WriteObject(Entry);
        }
    }
}

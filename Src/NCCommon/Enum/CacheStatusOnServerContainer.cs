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

using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;


namespace Alachisoft.NCache.Common.Enum
{
    public class CacheStatusOnServerContainer : ICompactSerializable
    {
        public CacheStatusOnServer cacheStatus;

        public void Deserialize(CompactReader reader)
        {
            this.cacheStatus = (CacheStatusOnServer)reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write((int)this.cacheStatus);               
        }
    }
}

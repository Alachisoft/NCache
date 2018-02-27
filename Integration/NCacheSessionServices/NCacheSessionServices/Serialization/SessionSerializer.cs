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
// limitations under the License

using System.IO;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.IO;

namespace Alachisoft.NCache.Web.SessionState.Serialization
{
    public static class SessionSerializer
    {
        public static byte[] Serialize(NCacheSessionData sessionData)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new CompactBinaryWriter(stream))
                {
                    SerializationUtility.SerializeDictionary(sessionData.Items, writer);
                }
                return stream.GetBuffer();
            }
        }

        public static NCacheSessionData Deserialize(byte[] data)
        {
            NCacheSessionData sessionData = new NCacheSessionData();
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new CompactBinaryReader(stream))
                {
                    sessionData.Items = SerializationUtility.DeserializeDictionary<string, byte[]>(reader);
                }
            }
            return sessionData;
        }
    }
}

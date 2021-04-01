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
using System.IO;
using System.Web;
using System.Web.SessionState;

namespace Alachisoft.NCache.Web.SessionState
{
    internal class SessionSerializationUtil
    {
        const byte SESSION_ITEMS = 1;
        const byte SESSION_STATIC_ITEMS = 2;

        public static byte[] Serialize(SessionStateStoreData sessionData)
        {
            byte sessionFlag = 0;
            MemoryStream stream = null;
            byte[] buffer = null;
            try
            {
                stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);

                if (sessionData.Items != null /*&& sessionData.Items.Count != 0*/)
                {
                    sessionFlag = (byte)(sessionFlag | SESSION_ITEMS);
                }
                if (sessionData.StaticObjects != null && !sessionData.StaticObjects.NeverAccessed)
                {
                    sessionFlag = (byte)(sessionFlag | SESSION_STATIC_ITEMS);
                }
                writer.Write(sessionFlag);

                if ((byte)(sessionFlag & SESSION_ITEMS) == SESSION_ITEMS)
                {
                    ((SessionStateItemCollection)sessionData.Items).Serialize(writer);
                }
                if ((byte)(sessionFlag & SESSION_STATIC_ITEMS) == SESSION_STATIC_ITEMS)
                {
                    sessionData.StaticObjects.Serialize(writer);
                }
                writer.Write(sessionData.Timeout);
            }
            finally
            {
                if (stream != null)
                {
                    buffer = stream.ToArray();
                    stream.Close();
                }
            }
            return buffer;

        }

        public static SessionStateStoreData Deserialize(byte[] buffer)
        {
            MemoryStream stream = new MemoryStream(buffer);

            SessionStateItemCollection itemCollection = null;
            HttpStaticObjectsCollection staticItemCollection = null;
            int timeout = 0;
            try
            {
                BinaryReader reader = new BinaryReader(stream);


                byte sessionFlag = reader.ReadByte();

                if ((byte)(sessionFlag & SESSION_ITEMS) == SESSION_ITEMS)
                {
                    itemCollection = SessionStateItemCollection.Deserialize(reader);
                }
                if ((byte)(sessionFlag & SESSION_STATIC_ITEMS) == SESSION_STATIC_ITEMS)
                {
                    staticItemCollection = HttpStaticObjectsCollection.Deserialize(reader);
                }
                timeout = reader.ReadInt32();

            }
            finally
            {
                if (stream != null) stream.Close();
            }
            return new SessionStateStoreData(itemCollection, staticItemCollection, timeout);
        }
    }
}
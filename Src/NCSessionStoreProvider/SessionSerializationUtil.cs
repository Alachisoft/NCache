//  Copyright (c) 2018 Alachisoft
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
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Web.SessionStoreProvider;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace Alachisoft.NCache.Web.SessionState
{
    [Serializable]
    internal class JSONObject
    {
        public Hashtable KeyValuePairs
        {
            get; set;
        }
    }

    internal class SessionSerializationUtil
    {
        const byte SESSION_ITEMS = 1;
        const byte SESSION_STATIC_ITEMS = 2;

        public static byte[] Serialize(SessionStateStoreData sessionData, string cacheId = "", bool isCompact = false, bool isJson = false)
        {

            byte sessionFlag = 0;
            MemoryStream stream = null;
            byte[] buffer = null;
            byte[] itemsBuffer = null;

            try
            {
                stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);

                sessionFlag = SetSessionFlag(sessionData);

                writer.Write(sessionFlag);

                if ((byte)(sessionFlag & SESSION_ITEMS) == SESSION_ITEMS)
                {
                    if (!isCompact && !isJson)
                    {
                        ((SessionStateItemCollection)sessionData.Items).Serialize(writer);
                    }
                    else
                    {
                        Hashtable sessionItems = new Hashtable();
                        var enumerator = (sessionData.Items).GetEnumerator();
                        string key = "";
                        while (enumerator.MoveNext())
                        {
                            key = enumerator.Current.ToString();
                            sessionItems.Add(key, sessionData.Items[key]);
                        }

                        if (isCompact)
                        {
                            itemsBuffer = CompactBinaryFormatter.ToByteBuffer(sessionItems, cacheId);

                        }
                        else
                        {
                            JSONObject jsonObject = new JSONObject();
                            jsonObject.KeyValuePairs = sessionItems;


                            var serializableObject = JsonConvert.SerializeObject(jsonObject, JsonSessionSerializationSettings.Instance);
                            itemsBuffer = Encoding.UTF8.GetBytes(serializableObject);
                        }

                        var length = itemsBuffer is byte[]? ((byte[])itemsBuffer).Length : 0;
                        writer.Write(length);
                        writer.Write(itemsBuffer);

                    }

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

        public static SessionStateStoreData Deserialize(byte[] buffer, string cacheId = "", bool isCompact = false, bool isJson = false)
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
                    itemCollection = new SessionStateItemCollection();

                    if (!isJson && !isCompact)
                        itemCollection = SessionStateItemCollection.Deserialize(reader);
                    else
                    {
                        var contentLength = reader.ReadInt32();
                        var itemBuffer = new byte[contentLength];
                        stream.Read(itemBuffer, 0, contentLength);
                        var sessionItems = new Hashtable();
                        if (isCompact)
                        {
                            sessionItems = (Hashtable)CompactBinaryFormatter.FromByteBuffer(itemBuffer, cacheId);
                        }
                        else if (isJson)
                        {
                            JSONObject jsonObject = JsonConvert.DeserializeObject<JSONObject>(Encoding.UTF8.GetString(itemBuffer), JsonSessionSerializationSettings.Instance);
                            sessionItems = jsonObject.KeyValuePairs;
                        }
                        foreach (string key in sessionItems.Keys)
                        {
                            itemCollection[key] = sessionItems[key];
                        }
                    }
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

        private static byte SetSessionFlag(SessionStateStoreData sessionData)
        {
            byte sessionFlag = 0;

            if (sessionData.Items != null)
            {
                sessionFlag = (byte)(sessionFlag | SESSION_ITEMS);
            }
            if (sessionData.StaticObjects != null && !sessionData.StaticObjects.NeverAccessed)
            {
                sessionFlag = (byte)(sessionFlag | SESSION_STATIC_ITEMS);
            }

            return sessionFlag;
        }
    }
}
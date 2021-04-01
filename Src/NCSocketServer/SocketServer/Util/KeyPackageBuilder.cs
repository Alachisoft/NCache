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
using System.IO;
using System.Text;
using System.Collections;

using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Util;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;


namespace Alachisoft.NCache.SocketServer.Util
{
    internal sealed class KeyPackageBuilder
    {


        private static Caching.Cache _cache = null;
        private static NCache _ncache = null;

        /// <summary>
        /// Cache Object used for deciding which Data Format mode current cache have.  
        /// </summary>
        internal static Caching.Cache Cache
        {
            get { return KeyPackageBuilder._cache; }
            set { KeyPackageBuilder._cache = value; }
        }

        /// <summary>
        /// Make a package containing quote separated keys from list
        /// </summary>
        /// <param name="keysList">list of keys to be packaged</param>
        /// <returns>key package being constructed</returns>
        internal static string PackageKeys(ArrayList keyList)
        {
            StringBuilder keyPackage = new StringBuilder(keyList.Count * 256);

            for (int i = 0; i < keyList.Count; i++)
                keyPackage.Append((string)keyList[i] + "\"");

            return keyPackage.ToString();
        }
        /// <summary>
        /// Make a package containing quote separated keys from list
        /// </summary>
        /// <param name="keysList">list of keys to be packaged</param>
        /// <returns>key package being constructed</returns>
        internal static string PackageKeys(ICollection keyList)
        {
            string packagedKeys = "";
            if (keyList != null && keyList.Count > 0) 
            {
                StringBuilder keyPackage = new StringBuilder(keyList.Count * 256);

                IEnumerator ie = keyList.GetEnumerator();
                while(ie.MoveNext())
                    keyPackage.Append((string)ie.Current + "\"");
                packagedKeys = keyPackage.ToString();
            }
            return packagedKeys;
        }

        internal static void PackageKeys(IDictionaryEnumerator dicEnu, out string keyPackage, out int keyCount)
        {
            StringBuilder keys = new StringBuilder(1024);
            keyCount = 0;

            while (dicEnu.MoveNext())
            {
                keys.Append(dicEnu.Key + "\"");
                keyCount++;
            }

            keyPackage = keys.ToString();
        }

		internal static void PackageKeys(IEnumerator enumerator, System.Collections.Generic.List<string> keys)
		{
            if (enumerator is IDictionaryEnumerator)
            {
                IDictionaryEnumerator ide = enumerator as IDictionaryEnumerator;
                while (ide.MoveNext())
                {
                    keys.Add((ide.Key).ToString());
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    keys.Add((enumerator.Current).ToString());
                }
            }
		}



        internal static IList PackageKeys(IEnumerator enumerator)
        {
            int estimatedSize = 0;
            IList ListOfKeyPackage = new ClusteredArrayList();
            IList<string> keysChunkList = new ClusteredList<string>();
            if (enumerator is IDictionaryEnumerator)
            {
                IDictionaryEnumerator ide = enumerator as IDictionaryEnumerator;
                while (ide.MoveNext())
                {
                    keysChunkList.Add((string)ide.Key);
                    estimatedSize = estimatedSize + (((string)ide.Key).Length * sizeof(Char));
                    if (estimatedSize >= ServiceConfiguration.ResponseDataSize) //If size is greater than specified size then add it and create new chunck
                    {
                        ListOfKeyPackage.Add(keysChunkList);
                        keysChunkList = new ClusteredList<string>();
                        estimatedSize = 0;
                    }
                }
                if (estimatedSize != 0)
                {
                    ListOfKeyPackage.Add(keysChunkList);
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    keysChunkList.Add((string)enumerator.Current);

                    estimatedSize = estimatedSize + (((string)enumerator.Current).Length * sizeof(Char));
                    if (estimatedSize >= ServiceConfiguration.ResponseDataSize) //If size is greater than specified size then add it and create new chunck
                    {
                        ListOfKeyPackage.Add(keysChunkList);
                        keysChunkList = new ClusteredList<string>();
                        estimatedSize = 0;
                    }
                }

                if (estimatedSize != 0)
                {
                    ListOfKeyPackage.Add(keysChunkList);
                }
            }
            if (ListOfKeyPackage.Count <= 0)
            {
                ListOfKeyPackage.Add(keysChunkList);
            }
            return ListOfKeyPackage;
        }

        /// <summary>
        /// Makes a key and data package form the keys and values of hashtable
        /// </summary>
        /// <param name="dic">Hashtable containing the keys and values to be packaged</param>
        /// <param name="keys">Contains packaged keys after execution</param>
        /// <param name="data">Contains packaged data after execution</param>
        /// <param name="currentContext">Current cache</param>
        internal static IList PackageKeysValues(IDictionary dic)
        {
            int estimatedSize = 0;
            IList  ListOfKeyPackageResponse = new ClusteredArrayList();
            if (dic != null && dic.Count > 0)
            {

                Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse keyPackageResponse = new Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse();

                IDictionaryEnumerator enu = dic.GetEnumerator();
                while (enu.MoveNext())
                {
                    Alachisoft.NCache.Common.Protobuf.Value value = new Alachisoft.NCache.Common.Protobuf.Value();
                    CompressedValueEntry cmpEntry= (CompressedValueEntry)enu.Value;

                    UserBinaryObject ubObject = null;
                    if (cmpEntry != null)
                    {
                        if (cmpEntry.Value is UserBinaryObject)
                            ubObject = (UserBinaryObject)cmpEntry.Value;
                        else
                        {
                            var flag = cmpEntry.Flag;
                            ubObject = (UserBinaryObject)Cache.SocketServerDataService.GetClientData(cmpEntry.Value, ref flag, LanguageContext.DOTNET);
                        }
                    }

                    //UserBinaryObject ubObject = Cache.SocketServerDataService.GetClientData(cmpEntry.Value, ref cmpEntry.Flag, LanguageContext.DOTNET) as UserBinaryObject;
                    value.data.AddRange(ubObject.DataList);
                    keyPackageResponse.keys.Add((string)enu.Key);
                    keyPackageResponse.flag.Add(cmpEntry.Flag.Data);
                    keyPackageResponse.values.Add(value);
                    keyPackageResponse.itemType.Add(MiscUtil.EntryTypeToProtoItemType(cmpEntry.Type) );// (Alachisoft.NCache.Common.Protobuf.CacheItemType.ItemType));

                    estimatedSize = estimatedSize + ubObject.Size + (((string)enu.Key).Length * sizeof(Char));

                    if (estimatedSize >= ServiceConfiguration.ResponseDataSize) //If size is greater than specified size then add it and create new chunck
                    {
                        ListOfKeyPackageResponse.Add(keyPackageResponse);
                        keyPackageResponse = new Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse();
                        estimatedSize = 0;
                    }
                }

                if (estimatedSize != 0)
                {
                    ListOfKeyPackageResponse.Add(keyPackageResponse);
                }
            }
            else
            {
                 ListOfKeyPackageResponse.Add(new Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse());
            }

            return ListOfKeyPackageResponse;
        }


        /// <summary>
        /// Makes a key and data package form the keys and values of hashtable
        /// </summary>
        /// <param name="dic">Hashtable containing the keys and values to be packaged</param>
        /// <param name="keys">Contains packaged keys after execution</param>
        /// <param name="data">Contains packaged data after execution</param>
        /// <param name="currentContext">Current cache</param>
        internal static Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse PackageKeysValues(IDictionary dic, Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse keyPackageResponse)
        {
            if (dic != null && dic.Count > 0) 
            {
                if (keyPackageResponse == null)
                    keyPackageResponse = new Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse(); ;

                IDictionaryEnumerator enu = dic.GetEnumerator();
                while (enu.MoveNext())
                {
                    keyPackageResponse.keys.Add((string)enu.Key);
                    CompressedValueEntry cmpEntry= (CompressedValueEntry)enu.Value;
                    BitSet flag = cmpEntry.Flag;
                    UserBinaryObject ubObject = Cache.SocketServerDataService.GetClientData(cmpEntry.Value, ref flag, LanguageContext.DOTNET) as UserBinaryObject;
                    Alachisoft.NCache.Common.Protobuf.Value value = new Alachisoft.NCache.Common.Protobuf.Value();
                    value.data.AddRange(ubObject.DataList);
                    keyPackageResponse.flag.Add(cmpEntry.Flag.Data);
                    
                    keyPackageResponse.values.Add(value);
                    keyPackageResponse.itemType.Add(MiscUtil.EntryTypeToProtoItemType(cmpEntry.Type));

                }
            }

            return keyPackageResponse;
        }


	

        /// <summary>
        /// Makes a key and data package form the keys and values of hashtable, for bulk operations
        /// </summary>
        /// <param name="dic">Hashtable containing the keys and values to be packaged</param>
        /// <param name="keys">Contains packaged keys after execution</param>
        /// <param name="data">Contains packaged data after execution</param>
        internal static void PackageKeysExceptions(Hashtable dic, Alachisoft.NCache.Common.Protobuf.KeyExceptionPackageResponse keyExceptionPackage)
        {
            int errorCode = -1;

            if (dic != null && dic.Count > 0)
            {
                IDictionaryEnumerator enu = dic.GetEnumerator();
                while (enu.MoveNext())
                {
                    CacheException cacheException = enu.Value as CacheException;
                    if (cacheException != null)
                    {
                        errorCode = cacheException.ErrorCode;
                    }
                    Exception ex = enu.Value as Exception;
                    
                    if (ex != null)
                    {
                        keyExceptionPackage.keys.Add((string)enu.Key);

                        Alachisoft.NCache.Common.Protobuf.Exception exc = new Alachisoft.NCache.Common.Protobuf.Exception();
                        exc.message = ex.Message;
                        exc.exception = ex.ToString();
                        exc.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;
                        exc.errorCode = errorCode;
                        keyExceptionPackage.exceptions.Add(exc);
                    }
                    
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="keyVersionPackage"></param>
        internal static void PackageKeysVersion(IDictionary dic, Alachisoft.NCache.Common.Protobuf.KeyVersionPackageResponse keyVersionPackage)
        {
            if (dic != null && dic.Count > 0)
            {
                IDictionaryEnumerator enu = dic.GetEnumerator();
                while (enu.MoveNext())
                {
                    ulong ver = Convert.ToUInt64(enu.Value.ToString());
                    if (ver != null)
                    {
                        keyVersionPackage.keys.Add((string)enu.Key);

                        keyVersionPackage.versions.Add(ver);
                    }
                }
            }
        }

    

        internal static ClusteredList<List<Common.Protobuf.Message>> GetMessages(IList<object> result, NCache nCache, string clientId)
        {
            int estimatedSize = 0;
            var listOfMessages = new ClusteredList<List<Common.Protobuf.Message>>();

            if (result != null && result.Count > 0)
            {
                var messagesList = new List<Common.Protobuf.Message>();
                var enu = result.GetEnumerator();

                while (enu.MoveNext())
                {
                    if (enu.Current == null)
                        continue;

                    var cachingMessage = (Message)enu.Current;
                    var protobufMessage = new Common.Protobuf.Message();

                    if (cachingMessage.PayLoad != null)     // Message is a user'smessage and not an event message
                    {
                        var value = new Common.Protobuf.Value();
                        var ubObject = default(UserBinaryObject);
                        var binaryObject = cachingMessage.PayLoad as UserBinaryObject;

                        if (binaryObject != null)
                            ubObject = binaryObject;

                        if (ubObject != default(UserBinaryObject))
                        {
                            value.data.AddRange(ubObject.DataList);
                            estimatedSize = ubObject.Size + cachingMessage.MessageId.Length * sizeof(char);
                        }
                        protobufMessage.payload = value;
                    }
                    else    // Message is an event message and not a user's message
                    {
                        protobufMessage.internalPayload = new Common.Protobuf.InternalPayload();
                        estimatedSize = estimatedSize + cachingMessage.MessageId.Length * sizeof(char);

                        if (cachingMessage is EventMessage)
                        {
                            var cachingEventMessage = (EventMessage)cachingMessage;
                            var protobufEventMessage = CreateProtobufEventMessage(nCache, cachingEventMessage, clientId);
                            protobufMessage.internalPayload.eventMessage = protobufEventMessage;
                            protobufMessage.internalPayload.payloadType = Common.Protobuf.InternalPayload.PayloadType.CACHE_ITEM_EVENTS;
                        }
                     
                    }

                    protobufMessage.flag = cachingMessage.FlagMap.Data;
                    protobufMessage.messageId = cachingMessage.MessageId;
                    protobufMessage.creationTime = cachingMessage.CreationTime.Ticks;
                    protobufMessage.expirationTime = cachingMessage.MessageMetaData.ExpirationTime;
                    protobufMessage.deliveryOption = (int)cachingMessage.MessageMetaData.DeliveryOption;
                    protobufMessage.subscriptionType = (int)cachingMessage.MessageMetaData.SubscriptionType;
                    protobufMessage.messageRemoveReason = (int)cachingMessage.MessageMetaData.MessgeFailureReason;

                    if (cachingMessage.MessageMetaData.RecepientList != null)
                        protobufMessage.recipientList.AddRange(cachingMessage.MessageMetaData.RecepientList);

                    if (cachingMessage.MessageMetaData.SubscriptionIdentifierList != null)
                        protobufMessage.subscriptionIdentifiers.AddRange(GetSubscriptionIds(cachingMessage.MessageMetaData.SubscriptionIdentifierList));

                    messagesList.Add(protobufMessage);

                    if (estimatedSize >= ServiceConfiguration.ResponseDataSize) // If size is greater than specified size then add it and create new chunk
                    {
                        listOfMessages.Add(messagesList);
                        messagesList = new List<Common.Protobuf.Message>();
                        estimatedSize = 0;
                    }
                }
                if (estimatedSize != 0)
                {
                    listOfMessages.Add(messagesList);
                }
            }
            else
            {
                listOfMessages.Add(new List<Common.Protobuf.Message>());
            }
            return listOfMessages;
        }
        private static List<Common.Protobuf.SubscriptionIdRecepientList> GetSubscriptionIds(List<SubscriptionIdentifier> subscriptionIdentifiers)
        {
            List<Common.Protobuf.SubscriptionIdRecepientList> reciepientIdList = new List<Common.Protobuf.SubscriptionIdRecepientList>();
            if (subscriptionIdentifiers != null)
            {
                foreach (var subscription in subscriptionIdentifiers)
                {
                    var subId = new Common.Protobuf.SubscriptionIdRecepientList();
                    subId.subscriptionName = subscription.SubscriptionName;
                    subId.policy = (int)subscription.SubscriptionPolicy;
                    reciepientIdList.Add(subId);

                }

            }

            return reciepientIdList;
        } 
        private static Common.Protobuf.MessageKeyValueResponse GetKeyValue(Message entry)
        {
            Common.Protobuf.MessageKeyValueResponse messageKeyValue = new Common.Protobuf.MessageKeyValueResponse();
            Common.Protobuf.KeyValuePair keyValue = new Common.Protobuf.KeyValuePair();
            keyValue.key = TopicConstant.DeliveryOption;
            int deliveryOption = (int)entry.MessageMetaData.DeliveryOption;
            keyValue.value = deliveryOption.ToString();
            messageKeyValue.keyValuePair.Add(keyValue);
            return messageKeyValue;
        }

        private static Common.Protobuf.EventMessage CreateProtobufEventMessage(NCache nCache, EventMessage cachingEventMessage, string _clientId)
        {
            var protobufEventMessage = new Common.Protobuf.EventMessage();
            protobufEventMessage.@event = new Common.Protobuf.EventId();
            protobufEventMessage.@event.eventUniqueId = cachingEventMessage.EventID.EventUniqueID;
            protobufEventMessage.key = cachingEventMessage.Key;


            if (cachingEventMessage.CallbackInfos != null)    // Item Level Events
            {
                foreach (CallbackInfo cbInfo in cachingEventMessage.CallbackInfos)
                {
                    if (cbInfo.Client == _clientId)
                    {
                        protobufEventMessage.callbackIds.Add((short)cbInfo.Callback);
                        protobufEventMessage.dataFilters.Add((short)cbInfo.DataFilter);
                    }
                    if (cachingEventMessage.EventID.EventType == Persistence.EventType.ITEM_UPDATED_CALLBACK || cachingEventMessage.EventID.EventType == Persistence.EventType.ITEM_UPDATED_EVENT)
                    {
                        protobufEventMessage.@event.item = EventHelper.ConvertToEventItem(cachingEventMessage.Item, cbInfo.DataFilter);
                        protobufEventMessage.@event.oldItem = EventHelper.ConvertToEventItem(cachingEventMessage.OldItem, cbInfo.DataFilter);
                        protobufEventMessage.eventType = Common.Protobuf.EventMessage.EventType.ITEM_UPDATED_CALLBACK;
                    }
                    else if (cachingEventMessage.EventID.EventType == Persistence.EventType.ITEM_REMOVED_CALLBACK || cachingEventMessage.EventID.EventType == Persistence.EventType.ITEM_REMOVED_EVENT)
                    {
                        protobufEventMessage.@event.item = EventHelper.ConvertToEventItem(cachingEventMessage.Item, cbInfo.DataFilter);
                        protobufEventMessage.eventType = Common.Protobuf.EventMessage.EventType.ITEM_REMOVED_CALLBACK;

                        switch (cachingEventMessage.RemoveReason)
                        {
                            case ItemRemoveReason.DependencyChanged:
                                protobufEventMessage.@event.removeReason = 0;
                                break;
                            case ItemRemoveReason.Expired:
                                protobufEventMessage.@event.removeReason = 1;
                                break;
                            case ItemRemoveReason.Removed:
                                protobufEventMessage.@event.removeReason = 2;
                                break;
                            default:
                                protobufEventMessage.@event.removeReason = 3;
                                break;
                        }
                    }
                }
            }
          
            else    // General Events
            {
                if (cachingEventMessage.EventID.EventType == Persistence.EventType.ITEM_ADDED_EVENT)
                {
                    protobufEventMessage.@event.item = EventHelper.ConvertToEventItem(cachingEventMessage.Item, nCache.ItemAddedFilter);
                    protobufEventMessage.eventType = Common.Protobuf.EventMessage.EventType.ITEM_ADDED_EVENT;
                }
                else if (cachingEventMessage.EventID.EventType == Persistence.EventType.ITEM_UPDATED_EVENT)
                {
                    protobufEventMessage.@event.item = EventHelper.ConvertToEventItem(cachingEventMessage.Item, nCache.ItemUpdatedFilter);
                    protobufEventMessage.@event.oldItem = EventHelper.ConvertToEventItem(cachingEventMessage.OldItem, nCache.ItemUpdatedFilter);
                    protobufEventMessage.eventType = Common.Protobuf.EventMessage.EventType.ITEM_UPDATED_EVENT;
                }
                else if (cachingEventMessage.EventID.EventType == Persistence.EventType.ITEM_REMOVED_EVENT)
                {
                    protobufEventMessage.@event.item = EventHelper.ConvertToEventItem(cachingEventMessage.Item, nCache.ItemRemovedFilter);
                    protobufEventMessage.eventType = Common.Protobuf.EventMessage.EventType.ITEM_REMOVED_EVENT;

                    switch (cachingEventMessage.RemoveReason)
                    {
                        case ItemRemoveReason.DependencyChanged:
                            protobufEventMessage.@event.removeReason = 0;
                            break;
                        case ItemRemoveReason.Expired:
                            protobufEventMessage.@event.removeReason = 1;
                            break;
                        case ItemRemoveReason.Removed:
                            protobufEventMessage.@event.removeReason = 2;
                            break;
                        default:
                            protobufEventMessage.@event.removeReason = 3;
                            break;
                    }
                }
            }
            protobufEventMessage.@event.operationCounter = cachingEventMessage.EventID.OperationCounter;

            protobufEventMessage.flagMap = cachingEventMessage.FlagMap.Data;


            return protobufEventMessage;
        }

       
    }
}

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

using System;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Caching;
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Common.DataStructures.Clustered;


namespace Alachisoft.NCache.SocketServer.Util
{
    internal sealed class KeyPackageBuilder
    {
        private static Caching.Cache _cache = null;

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
                    keys.Add((string)ide.Key);
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    keys.Add((string)enumerator.Current);
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
                    if (estimatedSize >= ServiceConfiguration.ResponseDataSize) // If size is greater than specified size then add it and create new chunck
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
                    if (estimatedSize >= ServiceConfiguration.ResponseDataSize) // If size is greater than specified size then add it and create new chunck
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
                            ubObject = (UserBinaryObject)Cache.SocketServerDataService.GetClientData(cmpEntry.Value, ref cmpEntry.Flag, LanguageContext.DOTNET);
                    }
                    value.data.AddRange(ubObject.DataList);
                    keyPackageResponse.keys.Add((string)enu.Key);
                    keyPackageResponse.flag.Add(cmpEntry.Flag.Data);
                    keyPackageResponse.values.Add(value);

                    estimatedSize = estimatedSize + ubObject.Size + (((string)enu.Key).Length * sizeof(Char));

                    if (estimatedSize >= ServiceConfiguration.ResponseDataSize) // If size is greater than specified size then add it and create new chunck
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
                    UserBinaryObject ubObject = Cache.SocketServerDataService.GetClientData(cmpEntry.Value, ref cmpEntry.Flag, LanguageContext.DOTNET) as UserBinaryObject;
                    Alachisoft.NCache.Common.Protobuf.Value value = new Alachisoft.NCache.Common.Protobuf.Value();
                    value.data.AddRange(ubObject.DataList);
                    keyPackageResponse.flag.Add(cmpEntry.Flag.Data);
                    
                    keyPackageResponse.values.Add(value);
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
            if (dic != null && dic.Count > 0)
            {
                IDictionaryEnumerator enu = dic.GetEnumerator();
                while (enu.MoveNext())
                {
                    Exception ex = enu.Value as Exception;
                    if (ex != null)
                    {
                        keyExceptionPackage.keys.Add((string)enu.Key);

                        Alachisoft.NCache.Common.Protobuf.Exception exc = new Alachisoft.NCache.Common.Protobuf.Exception();
                        exc.message = ex.Message;
                        exc.exception = ex.ToString();
                        exc.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;

                        keyExceptionPackage.exceptions.Add(exc);
                    }
                    // for DS write failed operations
                    if (enu.Value is OperationResult.Status)
                    {
                        OperationResult.Status status = (OperationResult.Status)enu.Value;
                        if (status == OperationResult.Status.Failure || status == OperationResult.Status.FailureDontRemove)
                        {
                            keyExceptionPackage.keys.Add((string)enu.Key);
                            Alachisoft.NCache.Common.Protobuf.Exception message = new Alachisoft.NCache.Common.Protobuf.Exception();
                            message.message = enu.Value.ToString();
                            message.exception = enu.Value.ToString();
                            message.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;
                            keyExceptionPackage.exceptions.Add(message);
                        }
                    }
                }
            }
        }
        
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

        /// <summary>
        /// Package keys and values where values can be Exception or not. If they are no exception, currently,
        /// 0 bytes is returned
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="keyPackage"></param>
        /// <param name="dataPackage"></param>
        internal static void PackageMisc(Hashtable dic, List<Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult> results)
        {
            if (dic != null && dic.Count > 0)
            {
                IDictionaryEnumerator enu = dic.GetEnumerator();

                while (enu.MoveNext())
                {
                    Common.Protobuf.DSUpdatedCallbackResult result = new Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResult();
                    result.key = (string)enu.Key;

                    if (enu.Value is Exception)
                    {
                        result.success = false;

                        Common.Protobuf.Exception ex = new Alachisoft.NCache.Common.Protobuf.Exception();
                        ex.message = ((Exception)enu.Value).Message;
                        ex.exception = ((Exception)enu.Value).ToString();
                        ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;

                        result.exception = ex;
                    }
                    else if (enu.Value is OperationResult.Status)
                    {
                        switch ((OperationResult.Status)enu.Value)
                        {
                            case OperationResult.Status.Success:
                                result.success = true;
                                break;
                            case OperationResult.Status.Failure:
                            case OperationResult.Status.FailureDontRemove:
                                result.success = false;
                                break;
                        }
                    }
                    results.Add(result);
                }
            }
        }

        internal static ClusteredList<List<Common.Protobuf.Message>> GetMessages(IList<object> result)
        {
            int estimatedSize = 0;
            ClusteredList<List<Common.Protobuf.Message>> ListOfMessagesResponse = new ClusteredList<List<Common.Protobuf.Message>>();
            if (result != null && result.Count > 0)
            {
                List<Common.Protobuf.Message> messagesList = new List<Common.Protobuf.Message>();
                IEnumerator<object> enu = result.GetEnumerator();
                while (enu.MoveNext())
                {
                    var message = new Common.Protobuf.Message();
                    var value = new Common.Protobuf.Value();
                    var entry = (Message)enu.Current;
                    BitSet flag = entry.FlagMap;
                    UserBinaryObject ubObject = null;
                    if (entry != null)
                    {
                        var binaryObject = entry.PayLoad as UserBinaryObject;
                        if (binaryObject != null)
                            ubObject = binaryObject;
                    }
                    if (ubObject != null)
                    {
                        value.data.AddRange(ubObject.DataList);
                        estimatedSize = estimatedSize + ubObject.Size + entry.MessageId.Length * sizeof(char);
                    }
                    message.messageId = entry.MessageId;
                    message.flag = flag.Data;
                    message.payload = value;
                    message.creationTime = entry.CreationTime.Ticks;
                    message.expirationTime = entry.MessageMetaData.ExpirationTime;
                    message.deliveryOption = (int)entry.MessageMetaData.DeliveryOption;
                    message.subscriptionType = (int)entry.MessageMetaData.SubscriptionType;
                    message.messageRemoveReason = (int)entry.MessageMetaData.MessgeFailureReason;
                    if (entry.MessageMetaData.RecepientList != null)
                        message.recipientList.AddRange(entry.MessageMetaData.RecepientList);
                    
                    messagesList.Add(message);

                    if (estimatedSize >= ServiceConfiguration.ResponseDataSize) // If size is greater than specified size then add it and create new chunck
                    {
                        ListOfMessagesResponse.Add(messagesList);
                        messagesList = new List<Common.Protobuf.Message>();
                        estimatedSize = 0;
                    }
                }

                if (estimatedSize != 0)
                {
                    ListOfMessagesResponse.Add(messagesList);
                }
            }
            else
            {
                ListOfMessagesResponse.Add(new List<Common.Protobuf.Message>());
            }
            return ListOfMessagesResponse;
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
    }
}

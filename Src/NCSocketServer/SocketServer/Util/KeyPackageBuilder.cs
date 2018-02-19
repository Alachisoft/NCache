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
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer.Util
{
    internal sealed class KeyPackageBuilder
    {
        
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

        /// <summary>
        /// Makes a key and data package form the keys and values of hashtable
        /// </summary>
        /// <param name="dic">Hashtable containing the keys and values to be packaged</param>
        /// <param name="keys">Contains packaged keys after execution</param>
        /// <param name="data">Contains packaged data after execution</param>
        /// <param name="currentContext">Current cache</param>
        internal static List<Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse> PackageKeysValues(IDictionary dic)
        {
            int estimatedSize = 0;
            List<Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse> ListOfKeyPackageResponse = new List<Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse>();
            if (dic != null && dic.Count > 0)
            {

                Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse keyPackageResponse = new Alachisoft.NCache.Common.Protobuf.KeyValuePackageResponse();

                IDictionaryEnumerator enu = dic.GetEnumerator();
                while (enu.MoveNext())
                {
                    Alachisoft.NCache.Common.Protobuf.Value value = new Alachisoft.NCache.Common.Protobuf.Value();
                    UserBinaryObject ubObject = ((CompressedValueEntry)enu.Value).Value as UserBinaryObject;
                    value.data.AddRange(ubObject.DataList);
                    keyPackageResponse.keys.Add((string)enu.Key);
                    keyPackageResponse.flag.Add(((CompressedValueEntry)enu.Value).Flag.Data);
                    keyPackageResponse.values.Add(value);

                    estimatedSize = estimatedSize + ubObject.Size;

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
                    keyPackageResponse.flag.Add(((CompressedValueEntry)enu.Value).Flag.Data);
                    UserBinaryObject ubObject = ((CompressedValueEntry)enu.Value).Value as UserBinaryObject;
                    Alachisoft.NCache.Common.Protobuf.Value value = new Alachisoft.NCache.Common.Protobuf.Value();
                    value.data.AddRange(ubObject.DataList);
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
                }
            }
        }
    }
}

// Copyright (c) 2015 Alachisoft
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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime;
using System.Collections;
using System.Threading;
using Alachisoft.NCache.Integrations.Memcached.Provider.Exceptions;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Alachisoft.NCache.Web.Net;
namespace Alachisoft.NCache.Integrations.Memcached.Provider
{
    public class MemcachedProvider : IMemcachedProvider
    {
        private Cache _cache;
        private Timer _timer;
        public static IMemcachedProvider Instance { get; internal set; }
        private const string ItemVersionKey = "Item_Version_Value";
        private const ulong ItemVersionValue = 1;

        #region Public Interface Implemented Methods

        public OperationResult InitCache(string cacheID)
        {
            if (string.IsNullOrEmpty(cacheID))
                ThrowInvalidArgumentsException();

            OperationResult returnObject = new OperationResult();

            try
            {
                _cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheID);
                
                if (!_cache.Contains(ItemVersionKey))
                {
                    _cache.Add(ItemVersionKey, ItemVersionValue); 
                }
                returnObject = CreateReturnObject(Result.SUCCESS, null);
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }

            return returnObject;
        }

        public OperationResult Set(string key, uint flags, long expirationTimeInSeconds, object dataBlock)
        {
            if (string.IsNullOrEmpty(key) || dataBlock == null)
                ThrowInvalidArgumentsException();

            OperationResult returnObject = InsertItemSuccessfully(key, flags, expirationTimeInSeconds, dataBlock);
            return returnObject;
        }

        public OperationResult Add(string key, uint flags, long expirationTimeInSeconds, object dataBlock)
        {
            if (string.IsNullOrEmpty(key) || dataBlock == null)
                ThrowInvalidArgumentsException();

            OperationResult returnObject = new OperationResult();

            try
            {
                returnObject = AddItemSuccessfully(key, flags, expirationTimeInSeconds, dataBlock);
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return returnObject;
        }

        public OperationResult Replace(string key, uint flags, long expirationTimeInSeconds, object dataBlock)
        {
            if (string.IsNullOrEmpty(key) || dataBlock == null)
                ThrowInvalidArgumentsException();

            OperationResult returnObject = new OperationResult();
            try
            {
                if (_cache.Contains(key))
                    returnObject = InsertItemSuccessfully(key, flags, expirationTimeInSeconds, dataBlock);
                else
                    returnObject = CreateReturnObject(Result.ITEM_NOT_FOUND, null);
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }

            return returnObject;

        }

        public OperationResult CheckAndSet(string key, uint flags, long expirationTimeInSeconds, ulong casUnique, object dataBlock)
        {
            if (string.IsNullOrEmpty(key) || dataBlock == null )
                ThrowInvalidArgumentsException();

            OperationResult returnObject = new OperationResult();
            try
            {
                CacheItem getCacheItem = _cache.GetCacheItem(key);
                if (getCacheItem == null)
                
                    returnObject = CreateReturnObject(Result.ITEM_NOT_FOUND, null);
                else
                {
                    MemcachedItem memCacheItem = (MemcachedItem)getCacheItem.Value;
                    if (memCacheItem.InternalVersion == casUnique)
                        returnObject = InsertItemSuccessfully(key, flags, expirationTimeInSeconds, dataBlock);
                    else
                        returnObject = CreateReturnObject(Result.ITEM_MODIFIED, null);
                }
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return returnObject;
        }
     
        public List<GetOpResult> Get(string[] keys)
        {
            if (keys.Length == 0)
                ThrowInvalidArgumentsException();

            List<GetOpResult> getObjects = new List<GetOpResult>();

            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    CacheItem getObject = _cache.GetCacheItem(keys[i]);
                    if (getObject != null)
                        getObjects.Add(CreateGetObject(keys[i], getObject));
                }
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return getObjects;

        }

        public OperationResult Delete(string key, ulong casUnique)
        {
            if (string.IsNullOrEmpty(key))
                ThrowInvalidArgumentsException();

            OperationResult returnObject = new OperationResult();
            try
            {
                if (casUnique == 0)
                {
                   Object obj = _cache.Remove(key);
                    if (obj == null)
                        returnObject = CreateReturnObject(Result.ITEM_NOT_FOUND, null);
                    else
                        returnObject = CreateReturnObject(Result.SUCCESS, null);
                }
                else
                {
                    CacheItem item = _cache.GetCacheItem(key);
                    if (item == null)
                        returnObject = CreateReturnObject(Result.ITEM_NOT_FOUND, null);
                    else
                    {
                        MemcachedItem memCacheItem = (MemcachedItem)item.Value;
                        if (memCacheItem.InternalVersion != casUnique)
                        {
                            returnObject = CreateReturnObject(Result.ITEM_MODIFIED, null);
                        }
                        else
                        {
                            _cache.Delete(key);
                            returnObject = CreateReturnObject(Result.SUCCESS, null);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }

            return returnObject;
        }

        public OperationResult Append(string key, object dataToAppend, ulong casUnique)
        {
            return Concat(key, dataToAppend, casUnique, UpdateType.Append);
        }

        public OperationResult Prepend(string key, object dataToPrepend, ulong casUnique)
        {
            return Concat(key, dataToPrepend, casUnique, UpdateType.Prepend);
        }

        public MutateOpResult Increment(string key, ulong value, object initialValue, long expirationTimeInSeconds, ulong casUnique)
        {
            return Mutate(key, value, initialValue, expirationTimeInSeconds, casUnique, UpdateType.Increment);
        }

        public MutateOpResult Decrement(string key, ulong value, object initialValue, long expirationTimeInSeconds, ulong casUnique)
        {
            return Mutate(key, value, initialValue, expirationTimeInSeconds, casUnique, UpdateType.Decrement);
        }

        public OperationResult Flush_All(long expirationTimeInSeconds)
        {

            OperationResult returnObject = new OperationResult();
            try
            {
                if (expirationTimeInSeconds == 0)
                {

                    ulong getVersion =(ulong) _cache.Get(ItemVersionKey);
                    _cache.Clear();
                    if (getVersion != null)
                    {
                        _cache.Insert(ItemVersionKey, getVersion);
                    }
                    else
                    {
                        _cache.Insert(ItemVersionKey, ItemVersionValue);
                    }


                }
                else
                {
                    long dueTimeInMilliseconds = expirationTimeInSeconds*1000;
                    _timer = new Timer(FlushExpirationCallBack, null, dueTimeInMilliseconds, 0);
                }
                returnObject = CreateReturnObject(Result.SUCCESS, null);
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return returnObject;
        }

        public OperationResult Touch(string key, long expirationTimeInSeconds)
        {
            if (string.IsNullOrEmpty(key))
                ThrowInvalidArgumentsException();

            OperationResult returnObject = new OperationResult();

            try
            {
               CacheItemAttributes attributes = new CacheItemAttributes();
                attributes.AbsoluteExpiration = CreateExpirationDate(expirationTimeInSeconds);
                bool result = _cache.SetAttributes(key, attributes);
                if (result)
                    returnObject = CreateReturnObject(Result.SUCCESS, null);
                else
                    returnObject = CreateReturnObject(Result.ITEM_NOT_FOUND, null);
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return returnObject;
        }

        public OperationResult GetVersion()
        {
            return CreateReturnObject(Result.SUCCESS, "1.4.5_4_gaa7839e");
        }

        public OperationResult GetStatistics(string argument)
        {
            Hashtable allStatistics = new Hashtable();

            switch (argument)
            {
                case null:
                case "":
                    allStatistics = GeneralStats();
                    break;
                case "settings":
                    allStatistics = SettingsStats();
                    break;
                case "items":
                    allStatistics = ItemsStats();
                    break;
                case "sizes":
                    allStatistics = ItemSizesStats();
                    break;
                case "slabs":
                    allStatistics = SlabsStats();
                    break;
                default:
                    break;
            }

            OperationResult returnObject = CreateReturnObject(Result.SUCCESS, allStatistics);
            return returnObject;
        }

        public OperationResult ReassignSlabs(int sourceClassID, int destinationClassID)
        {
            OperationResult returnObject = CreateReturnObject(Result.SUCCESS, null);
            return returnObject;
        }

        public OperationResult AutomoveSlabs(int option)
        {
            OperationResult returnObject = CreateReturnObject(Result.SUCCESS, null);
            return returnObject;
        }

        public OperationResult SetVerbosityLevel(int verbosityLevel)
        {
            OperationResult returnObject = CreateReturnObject(Result.SUCCESS, null);
            return returnObject;
        }

        public void Dispose()
        {
            try
            {
                _cache.Dispose();
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
        }

        #endregion

        #region Private Utility Methods

        private OperationResult InsertItemSuccessfully(string key, uint flags, long expirationTimeInSeconds, object dataBlock)
        {
            if (expirationTimeInSeconds < 0)
                return CreateReturnObject(Result.SUCCESS, 0);

            OperationResult returnObject = new OperationResult();
            
            try
            {
                ulong getVersion = GetLatestVersion();
                
                CacheItem cacheItem = CreateCacheItem(flags, dataBlock, expirationTimeInSeconds, getVersion);
                _cache.Insert(key, cacheItem);
                returnObject.Value = getVersion;
                returnObject.ReturnResult = Result.SUCCESS;
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return returnObject;
        }

        private OperationResult AddItemSuccessfully(string key, uint flags, long expirationTimeInSeconds, object dataBlock)
        {
            if (expirationTimeInSeconds < 0)
                return CreateReturnObject(Result.SUCCESS, 0);

            OperationResult returnObject = new OperationResult();
            try
            {
                ulong getVersion = GetLatestVersion();
                
                CacheItem cacheItem = CreateCacheItem(flags, dataBlock, expirationTimeInSeconds, getVersion);
               
                _cache.Add(key, cacheItem);
                returnObject.Value = getVersion;
                returnObject.ReturnResult = Result.SUCCESS;
            }
            catch (Alachisoft.NCache.Runtime.Exceptions.OperationFailedException e)
            {
                returnObject = CreateReturnObject(Result.ITEM_EXISTS, null);
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return returnObject;
        }

        private OperationResult Concat(string key, object dataToPrepend, ulong casUnique, UpdateType updateType)
        {
            if (string.IsNullOrEmpty(key) || dataToPrepend == null)
                ThrowInvalidArgumentsException();
         
            OperationResult returnObject = new OperationResult();
            try
            {
                CacheItem getObject = _cache.GetCacheItem(key);
                if (getObject == null)
                    returnObject = CreateReturnObject(Result.ITEM_NOT_FOUND, null);
                else if (getObject.Value == null)
                    returnObject = CreateReturnObject(Result.ITEM_NOT_FOUND, null);
                else
                {
                    MemcachedItem memCacheItem = (MemcachedItem)getObject.Value;
                    if ((casUnique > 0 && memCacheItem.InternalVersion == casUnique) || casUnique == 0)
                        returnObject = JoinObjects(key, getObject, dataToPrepend, updateType);
                    else
                        returnObject = CreateReturnObject(Result.ITEM_MODIFIED, null);
                }
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return returnObject;
        }

        private MutateOpResult Mutate(string key, ulong value, object initialValue, long expirationTimeInSeconds, ulong casUnique, UpdateType updateType)
        {
            if (string.IsNullOrEmpty(key)|| (initialValue != null && IsUnsignedNumeric(initialValue) == false))
                ThrowInvalidArgumentsException();

            MutateOpResult returnObject = new MutateOpResult();
            try
            {
               
                CacheItem getObject = _cache.GetCacheItem(key);
                if (getObject == null)
                {
                    if (initialValue == null || expirationTimeInSeconds == uint.MaxValue)
                    {
                        returnObject.ReturnResult = Result.ITEM_NOT_FOUND;
                        returnObject.Value = null;
                    }
                    else
                    {
                        OperationResult opResult = InsertItemSuccessfully(key, 10, expirationTimeInSeconds,
                        BitConverter.GetBytes(Convert.ToUInt32(initialValue)));
                        returnObject.Value = opResult.Value;
                        returnObject.ReturnResult = opResult.ReturnResult;
                        returnObject.MutateResult = Convert.ToUInt64(initialValue);
                    }
                }
                else
                {
                    MemcachedItem memCacheItem = (MemcachedItem)getObject.Value;
                    if ((casUnique > 0 && memCacheItem.InternalVersion == casUnique) || casUnique == 0)
                        returnObject = UpdateIfNumeric(key, getObject, value, updateType);
                    else
                    {
                        returnObject.ReturnResult = Result.ITEM_MODIFIED;
                        returnObject.Value = null;
                    }
                }
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }
            return returnObject;
        }
       
        //whenever the item is updated, the version is incremented by 1
        private CacheItem CreateCacheItem(uint flags, object dataBlock, long expirationTimeInSeconds, ulong version)
        {
            MemcachedItem memCacheItem = new MemcachedItem();
            memCacheItem.Data = CreateObjectArray(flags, dataBlock);
            memCacheItem.InternalVersion = version;
            CacheItem cacheItem = new CacheItem(memCacheItem);
    ;
            if (expirationTimeInSeconds != 0)
                cacheItem.AbsoluteExpiration = CreateExpirationDate(expirationTimeInSeconds);
            return cacheItem;
        }

        private byte[] CreateObjectArray(uint flags, object dataBlock)
        {
            byte[] flagBytes = BitConverter.GetBytes(flags);
            byte[] dataBytes = (byte[])dataBlock;
            byte[] objectArray = new byte[flagBytes.Length + dataBytes.Length];
            System.Buffer.BlockCopy(flagBytes, 0, objectArray, 0, flagBytes.Length);
            System.Buffer.BlockCopy(dataBytes, 0, objectArray, flagBytes.Length, dataBytes.Length);
            return objectArray;
        }

        private ObjectArrayData GetObjectArrayData(object retrievedObject)
        {
            byte[] objectArray = (byte[])retrievedObject;
            byte[] dataArray = new byte[objectArray.Length - 4];
            System.Buffer.BlockCopy(objectArray, 4, dataArray, 0, dataArray.Length);

            ObjectArrayData objectArrayData = new ObjectArrayData();
            objectArrayData.flags = BitConverter.ToUInt32(objectArray, 0);
            objectArrayData.dataBytes = dataArray;
            return objectArrayData;

        }

        private DateTime CreateExpirationDate(long expirationTimeInSeconds)
        {

            DateTime dateTime;
            if (expirationTimeInSeconds <= 2592000)
                dateTime = DateTime.Now.AddSeconds(expirationTimeInSeconds);
            else
            {
                System.DateTime unixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                unixTime = unixTime.AddSeconds(expirationTimeInSeconds).ToLocalTime();
                dateTime = unixTime;
            }
            return dateTime;
        }

        private OperationResult CreateReturnObject(Result result, object value)
        {
            OperationResult returnObject = new OperationResult();
            returnObject.ReturnResult = result;
            returnObject.Value = value;
            return returnObject;
        }

        private GetOpResult CreateGetObject(string key, CacheItem cacheItem)
        {
           
            GetOpResult getObject = new GetOpResult();
            MemcachedItem memCacheItem = (MemcachedItem)cacheItem.Value;
            ObjectArrayData objectArrayData = GetObjectArrayData(memCacheItem.Data);
            getObject.Key = key;
            getObject.Flag = objectArrayData.flags;
            getObject.Value = objectArrayData.dataBytes;
            getObject.Version = memCacheItem.InternalVersion;
            getObject.ReturnResult = Result.SUCCESS;
            return getObject;
        }

        private OperationResult JoinObjects(string key, CacheItem cacheItem, object objectToJoin, UpdateType updateType)
        {
            OperationResult returnObject = new OperationResult();
            ObjectArrayData objectDataArray = GetObjectArrayData(((MemcachedItem)cacheItem.Value).Data);
            byte[] originalByteObject = objectDataArray.dataBytes;
            byte[] byteObjectToJoin = (byte[])objectToJoin;

            byte[] joinedObject = new byte[originalByteObject.Length + byteObjectToJoin.Length];

            if (updateType == UpdateType.Append)
            {
                System.Buffer.BlockCopy(originalByteObject, 0, joinedObject, 0, originalByteObject.Length);
                System.Buffer.BlockCopy(byteObjectToJoin, 0, joinedObject, originalByteObject.Length, byteObjectToJoin.Length);
            }
            else
            {
                System.Buffer.BlockCopy(byteObjectToJoin, 0, joinedObject, 0, byteObjectToJoin.Length);
                System.Buffer.BlockCopy(originalByteObject, 0, joinedObject, byteObjectToJoin.Length, originalByteObject.Length);
            }

            try
            {
                
                MemcachedItem memCacheItem = new MemcachedItem();
                memCacheItem.Data = CreateObjectArray(objectDataArray.flags, joinedObject);
                
                ulong getVersion = GetLatestVersion();
               
                memCacheItem.InternalVersion = getVersion;
                
                cacheItem.Value = memCacheItem;
                
                _cache.Insert(key, cacheItem);
                
                returnObject = CreateReturnObject(Result.SUCCESS, getVersion);
            }
            catch (Exception e)
            {
                ThrowCacheRuntimeException(e);
            }

            return returnObject;
        }

        private ulong GetLatestVersion()
        {
            ulong version;
            if (_cache.Contains(ItemVersionKey))
            {
                version = (ulong) _cache.Get(ItemVersionKey);
                version++;
            }
            else
            {
                version = ItemVersionValue;
            }
            _cache.Insert(ItemVersionKey, version);
            return version;
        }


        private MutateOpResult UpdateIfNumeric(string key, CacheItem cacheItem, ulong value, UpdateType updateType)
        {
            MutateOpResult returnObject = new MutateOpResult();
            MemcachedItem memCachedItem = (MemcachedItem)cacheItem.Value;
            if (memCachedItem != null)
            {
                ObjectArrayData objectDataArray = GetObjectArrayData(memCachedItem.Data);

                string tempObjectString = "";
                try
                {
                    tempObjectString = Encoding.ASCII.GetString(objectDataArray.dataBytes);
                }
                catch (Exception e)
                {
                    ThrowCacheRuntimeException(e);
                }

                if (IsUnsignedNumeric(tempObjectString))
                {
                    ulong originalValue = Convert.ToUInt64(tempObjectString);
                    ulong finalValue;

                    if (updateType == UpdateType.Increment)
                    {
                        finalValue = originalValue + value;
                    }
                    else
                    {
                        if (value > originalValue)
                            finalValue = 0;
                        else
                            finalValue = originalValue - value;
                    }

                    try
                    {

                        MemcachedItem memCacheItem = new MemcachedItem();
                        memCacheItem.Data = CreateObjectArray(objectDataArray.flags, Encoding.ASCII.GetBytes(finalValue + ""));


                        ulong getVersion = GetLatestVersion();

                        memCacheItem.InternalVersion = getVersion;

                        cacheItem.Value = memCacheItem;

                        _cache.Insert(key, cacheItem);

                        returnObject.ReturnResult = Result.SUCCESS;
                        returnObject.Value = getVersion;
                        returnObject.MutateResult = finalValue;
                    }
                    catch (Exception e)
                    {
                        ThrowCacheRuntimeException(e);
                    }
                }
                else
                {
                    returnObject.ReturnResult = Result.ITEM_TYPE_MISMATCHED;
                    returnObject.Value = null;
                    returnObject.MutateResult = 0;
                }
            }
            return returnObject;
        }

        private bool IsUnsignedNumeric(object item)
        {
            try
            {
                Convert.ToUInt64(item);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private void FlushExpirationCallBack(Object stateInfo)
        {
            try
            {
                ulong getVersion = (ulong)_cache.Get(ItemVersionKey);
                _cache.Clear();
                if (getVersion != null)
                {
                    _cache.Insert(ItemVersionKey, getVersion);
                }
                else
                {
                    _cache.Insert(ItemVersionKey, ItemVersionValue);
                }
                _timer.Dispose();
            }
            catch (Exception e)
            {
                //Do nothing
            }
        }

        private void ThrowInvalidArgumentsException()
        {
            InvalidArgumentsException exception = new InvalidArgumentsException("Invalid Arguments Specified.");
            throw exception;
        }

        private void ThrowCacheRuntimeException(Exception ex)
        {
            CacheRuntimeException exception = new CacheRuntimeException("Exception Occured at server. " + ex.Message, ex);
            throw exception;
        }

        private Hashtable GeneralStats()
        {
            Hashtable generalStatistics = new Hashtable();
            string statValue;

            statValue = "0";
            generalStatistics.Add("pid", statValue);
            statValue = "0";
            generalStatistics.Add("uptime", statValue);
            statValue = "0";
            generalStatistics.Add("time", statValue);
            statValue = "0";
            generalStatistics.Add("version", statValue);
            statValue = "0";
            generalStatistics.Add("pointer_size", statValue);
            statValue = "0";
            generalStatistics.Add("rusage_user", statValue);
            statValue = "0";
            generalStatistics.Add("rusage_system", statValue);
            statValue = "0";
            generalStatistics.Add("curr_items", statValue);
            statValue = "0";
            generalStatistics.Add("total_items", statValue);
            statValue = "0";
            generalStatistics.Add("bytes", statValue);
            statValue = "0";
            generalStatistics.Add("curr_connections", statValue);
            statValue = "0";
            generalStatistics.Add("total_connections", statValue);
            statValue = "0";
            generalStatistics.Add("connection_structures", statValue);
            statValue = "0";
            generalStatistics.Add("reserved_fds ", statValue);
            statValue = "0";
            generalStatistics.Add("cmd_get", statValue);
            statValue = "0";
            generalStatistics.Add("cmd_set", statValue);
            statValue = "0";
            generalStatistics.Add("cmd_flush", statValue);
            statValue = "0";
            generalStatistics.Add("cmd_touch", statValue);
            statValue = "0";
            generalStatistics.Add("get_hits", statValue);
            statValue = "0";
            generalStatistics.Add("get_misses", statValue);
            statValue = "0";
            generalStatistics.Add("delete_misses", statValue);
            statValue = "0";
            generalStatistics.Add("delete_hits", statValue);
            statValue = "0";
            generalStatistics.Add("incr_misses", statValue);
            statValue = "0";
            generalStatistics.Add("incr_hits", statValue);
            statValue = "0";
            generalStatistics.Add("decr_misses", statValue);
            statValue = "0";
            generalStatistics.Add("decr_hits", statValue);
            statValue = "0";
            generalStatistics.Add("cas_misses", statValue);
            statValue = "0";
            generalStatistics.Add("cas_hits", statValue);
            statValue = "0";
            generalStatistics.Add("cas_badval", statValue);
            statValue = "0";
            generalStatistics.Add("touch_hits", statValue);
            statValue = "0";
            generalStatistics.Add("touch_misses", statValue);
            statValue = "0";
            generalStatistics.Add("auth_cmds", statValue);
            statValue = "0";
            generalStatistics.Add("auth_errors", statValue);
            statValue = "0";
            generalStatistics.Add("evictions", statValue);
            statValue = "0";
            generalStatistics.Add("reclaimed", statValue);
            statValue = "0";
            generalStatistics.Add("bytes_read", statValue);
            statValue = "0";
            generalStatistics.Add("bytes_written", statValue);
            statValue = "0";
            generalStatistics.Add("limit_maxbytes", statValue);
            statValue = "0";
            generalStatistics.Add("threads", statValue);
            statValue = "0";
            generalStatistics.Add("conn_yields", statValue);
            statValue = "0";
            generalStatistics.Add("hash_power_level", statValue);
            statValue = "0";
            generalStatistics.Add("hash_bytes", statValue);
            statValue = "0";
            generalStatistics.Add("hash_is_expanding", statValue);
            statValue = "0";
            generalStatistics.Add("expired_unfetched", statValue);
            statValue = "0";
            generalStatistics.Add("evicted_unfetched", statValue);
            statValue = "0";
            generalStatistics.Add("slab_reassign_running", statValue);
            statValue = "0";
            generalStatistics.Add("slabs_moved ", statValue);

            return generalStatistics;
        }

        private Hashtable SettingsStats()
        {
            Hashtable settingsStatistics = new Hashtable();
            string statValue;

            statValue = "0";
            settingsStatistics.Add("maxbytes", statValue);
            statValue = "0";
            settingsStatistics.Add("maxconns", statValue);
            statValue = "0";
            settingsStatistics.Add("tcpport", statValue);
            statValue = "0";
            settingsStatistics.Add("udpport", statValue);
            statValue = "0";
            settingsStatistics.Add("inter", statValue);
            statValue = "0";
            settingsStatistics.Add("verbosity", statValue);
            statValue = "0";
            settingsStatistics.Add("oldest", statValue);
            statValue = "0";
            settingsStatistics.Add("evictions", statValue);
            statValue = "0";
            settingsStatistics.Add("domain_socket", statValue);
            statValue = "0";
            settingsStatistics.Add("umask", statValue);
            statValue = "0";
            settingsStatistics.Add("growth_factor", statValue);
            statValue = "0";
            settingsStatistics.Add("chunk_size", statValue);
            statValue = "0";
            settingsStatistics.Add("num_threads", statValue);
            statValue = "0";
            settingsStatistics.Add("stat_key_prefix", statValue);
            statValue = "0";
            settingsStatistics.Add("detail_enabled", statValue);
            statValue = "0";
            settingsStatistics.Add("reqs_per_event", statValue);
            statValue = "0";
            settingsStatistics.Add("cas_enabled", statValue);
            statValue = "0";
            settingsStatistics.Add("tcp_backlog", statValue);
            statValue = "0";
            settingsStatistics.Add("auth_enabled_sasl", statValue);
            statValue = "0";
            settingsStatistics.Add("item_size_max", statValue);
            statValue = "0";
            settingsStatistics.Add("maxconns_fast", statValue);
            statValue = "0";
            settingsStatistics.Add("hashpower_init", statValue);
            statValue = "0";
            settingsStatistics.Add("slab_reassign", statValue);
            statValue = "0";
            settingsStatistics.Add("slab_automove", statValue);

            return settingsStatistics;
        }

        private Hashtable ItemsStats()
        {
            Hashtable itemsStatistics = new Hashtable();
            string statValue;

            statValue = "0";
            itemsStatistics.Add("number", statValue);
            statValue = "0";
            itemsStatistics.Add("age", statValue);
            statValue = "0";
            itemsStatistics.Add("evicted", statValue);
            statValue = "0";
            itemsStatistics.Add("evicted_nonzero", statValue);
            statValue = "0";
            itemsStatistics.Add("evicted_time", statValue);
            statValue = "0";
            itemsStatistics.Add("outofmemory", statValue);
            statValue = "0";
            itemsStatistics.Add("tailrepairs", statValue);
            statValue = "0";
            itemsStatistics.Add("reclaimed", statValue);
            statValue = "0";
            itemsStatistics.Add("expired_unfetched", statValue);
            statValue = "0";
            itemsStatistics.Add("evicted_unfetched", statValue);

            return itemsStatistics;
        }

        private Hashtable ItemSizesStats()
        {

            Hashtable itemSizesStatistics = new Hashtable();

            itemSizesStatistics.Add("0", "0");

            return itemSizesStatistics;
        }

        private Hashtable SlabsStats()
        {
            Hashtable generalStatistics = new Hashtable();
            string statValue;

            statValue = "0";
            generalStatistics.Add("pid", statValue);
            statValue = "0";
            generalStatistics.Add("uptime", statValue);
            statValue = "0";
            generalStatistics.Add("time", statValue);
            statValue = "0";
            generalStatistics.Add("version", statValue);
            statValue = "0";
            generalStatistics.Add("pointer_size", statValue);
            statValue = "0";
            generalStatistics.Add("rusage_user", statValue);
            statValue = "0";
            generalStatistics.Add("rusage_system", statValue);
            statValue = "0";
            generalStatistics.Add("curr_items", statValue);
            statValue = "0";
            generalStatistics.Add("total_items", statValue);
            statValue = "0";
            generalStatistics.Add("bytes", statValue);
            statValue = "0";
            generalStatistics.Add("curr_connections", statValue);
            statValue = "0";
            generalStatistics.Add("total_connections", statValue);
            statValue = "0";
            generalStatistics.Add("connection_structures", statValue);
            statValue = "0";
            generalStatistics.Add("reserved_fds ", statValue);
            statValue = "0";
            generalStatistics.Add("cmd_get", statValue);
            statValue = "0";
            generalStatistics.Add("cmd_set", statValue);
            statValue = "0";
            generalStatistics.Add("cmd_flush", statValue);
            statValue = "0";
            generalStatistics.Add("cmd_touch", statValue);
            statValue = "0";
            generalStatistics.Add("get_hits", statValue);
            statValue = "0";
            generalStatistics.Add("get_misses", statValue);
            statValue = "0";
            generalStatistics.Add("delete_misses", statValue);
            statValue = "0";
            generalStatistics.Add("delete_hits", statValue);
            statValue = "0";
            generalStatistics.Add("incr_misses", statValue);
            statValue = "0";
            generalStatistics.Add("incr_hits", statValue);
            statValue = "0";
            generalStatistics.Add("decr_misses", statValue);
            statValue = "0";
            generalStatistics.Add("decr_hits", statValue);
            statValue = "0";
            generalStatistics.Add("cas_misses", statValue);
            statValue = "0";
            generalStatistics.Add("cas_hits", statValue);
            statValue = "0";
            generalStatistics.Add("cas_badval", statValue);
            statValue = "0";
            generalStatistics.Add("touch_hits", statValue);
            statValue = "0";
            generalStatistics.Add("touch_misses", statValue);
            statValue = "0";
            generalStatistics.Add("auth_cmds", statValue);
            statValue = "0";
            generalStatistics.Add("auth_errors", statValue);
            statValue = "0";
            generalStatistics.Add("evictions", statValue);
            statValue = "0";
            generalStatistics.Add("reclaimed", statValue);
            statValue = "0";
            generalStatistics.Add("bytes_read", statValue);
            statValue = "0";
            generalStatistics.Add("bytes_written", statValue);
            statValue = "0";
            generalStatistics.Add("limit_maxbytes", statValue);
            statValue = "0";
            generalStatistics.Add("threads", statValue);
            statValue = "0";
            generalStatistics.Add("conn_yields", statValue);
            statValue = "0";
            generalStatistics.Add("hash_power_level", statValue);
            statValue = "0";
            generalStatistics.Add("hash_bytes", statValue);
            statValue = "0";
            generalStatistics.Add("hash_is_expanding", statValue);
            statValue = "0";
            generalStatistics.Add("expired_unfetched", statValue);
            statValue = "0";
            generalStatistics.Add("evicted_unfetched", statValue);
            statValue = "0";
            generalStatistics.Add("slab_reassign_running", statValue);
            statValue = "0";
            generalStatistics.Add("slabs_moved ", statValue);

            return generalStatistics;
        }

        #endregion

        private enum UpdateType
        {
            Append,
            Prepend,
            Increment,
            Decrement
        }

        private struct ObjectArrayData
        {
            public uint flags;
            public byte[] dataBytes;
        }
    }
}

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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Runtime.Processor;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Processor
{
    internal class EntryProcessorManager
    {
        public static TimeSpan DefaultLockTimeOut = new TimeSpan(0, 0, 60);

    /**
     * The runtime context associated with the current cache.
     */
    private CacheRuntimeContext _context;
    private Alachisoft.NCache.Caching.Cache _cacheRoot;
    private String _cacheName;

    public EntryProcessorManager(String cacheName, CacheRuntimeContext context, Alachisoft.NCache.Caching.Cache cacheRoot) {
        _cacheName = cacheName;
        _context = context;
        _cacheRoot = cacheRoot;
    }

    public Hashtable ProcessEntries(string[] keys, IEntryProcessor entryProcessor, Object[] arguments, BitSet writeOptionFlag, String defaultWriteThru, OperationContext operationContext) {

        Hashtable resultMap = new Hashtable();
        foreach (string key in keys) {
            try {
                IEntryProcessorResult result = this.ProcessEntry(key, entryProcessor, arguments, writeOptionFlag, defaultWriteThru, operationContext);
                if (result != null) {
                    resultMap.Add(key, result);
                }
            } catch (Exception ex) {
                _context.NCacheLog.Error("Cache.InvokeEntryProcessor", "exception is thrown while processing key: " + key + ex.Message);
            }
        }
        return resultMap;
    }

    public IEntryProcessorResult ProcessEntry(string key, IEntryProcessor entryProcessor, Object[] arguments, BitSet writeOptionFlag, String defaultWriteThru, OperationContext operationContext) 
    {
        IEntryProcessorResult result = null;
        EPCacheEntry epEntry = null;
        try {
            epEntry = GetEPCacheEntry(key, entryProcessor.IgnoreLock(), operationContext);
            object value = null;
            if (epEntry !=null && epEntry.CacheEntry != null)         
                value = epEntry.CacheEntry.Value;            
            NCacheMutableEntry mutableEntry = new NCacheMutableEntry(key, value);

            result = new EntryProcessorResult(key, entryProcessor.ProcessEntry(mutableEntry, arguments));

            if (mutableEntry.IsUpdated)
            {
                epEntry.CacheEntry = MakeCacheEntry(epEntry.CacheEntry, mutableEntry.Value);

                UpdateEPCacheEntry(key, epEntry, writeOptionFlag, defaultWriteThru);
            }
            else if (mutableEntry.IsRemoved) 
            {
                RemoveEPCacheEntry(key, epEntry, writeOptionFlag, defaultWriteThru);
            }

        } catch (EntryProcessorException ex) {
            return new EntryProcessorResult(key, ex);
        } catch (Exception ex) {
            return new EntryProcessorResult(key, new EntryProcessorException(ex));
        }
        finally
        {
            if(epEntry!=null && epEntry.LockHandle!=null)
            {
                try {
                    _cacheRoot.Unlock(key,epEntry.LockHandle.LockId,false,new OperationContext());
                } catch(Exception ex)
                {                   
                    _context.NCacheLog.Error("EntryProcessorManager.ProcesssEntry", "exception is thrown while unlocking key: " + key.ToString() + ex.Message);  
                }
            }
        }     
        return result;
    }

    private EPCacheEntry GetEPCacheEntry(Object key, Boolean ignoreLock, OperationContext operationContext) 
    {
        Object lockId = null;
        ulong version = 0;
        DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        DateTime lockDate = time.Date;
        BitSet flagMap = new BitSet();
        operationContext.Add(OperationContextFieldName.ReaderBitsetEnum, flagMap);
        operationContext.Add(OperationContextFieldName.DataFormat, DataFormat.Object);
        operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
        
        LockAccessType lockAccessType = LockAccessType.IGNORE_LOCK;

        if (!ignoreLock) {
            lockAccessType = LockAccessType.ACQUIRE;
        }

        CacheEntry entry = (CacheEntry) _cacheRoot.GetCacheEntry(key, null, null, ref lockId, ref lockDate, EntryProcessorManager.DefaultLockTimeOut, lockAccessType, operationContext, ref version);
        
        LockHandle handle = null;
        if (lockId != null) {
            handle = new LockHandle(lockId.ToString(), lockDate);
        }

        if (entry != null)
        {
            CallbackEntry callbackEntry = entry.Value as CallbackEntry;
            object value = callbackEntry != null ? callbackEntry.Value : entry.Value;
            if (value != null)
            {
                entry.Value = _context.CachingSubSystemDataService.GetCacheData(value, entry.Flag);
            }
        }

        return new EPCacheEntry(entry, handle);
    }

    private void UpdateEPCacheEntry(Object key, EPCacheEntry epCacheEntry, BitSet writeOptionFlag, String writethruProvider) 
    {
        CacheEntry entry = epCacheEntry.CacheEntry;
        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);
        object value;
        if (entry.Value is CallbackEntry)
        {
            value = ((CallbackEntry)entry.Value).Value;
        }
        else
        {
            value = entry.Value;
        }
        object obj = _context.CachingSubSystemDataService.GetClientData(value, ref writeOptionFlag, Common.Util.LanguageContext.DOTNET);

        _cacheRoot.Insert(key, obj, entry.ExpirationHint, entry.SyncDependency, entry.EvictionHint, entry.GroupInfo != null ? entry.GroupInfo.Group : null, entry.GroupInfo != null ? entry.GroupInfo.SubGroup : null, entry.QueryInfo, writeOptionFlag, entry.LockId != null ? entry.LockId : null, entry.Version, entry.LockAccessType, writethruProvider, entry.ResyncProviderName != null ? entry.ResyncProviderName : null, operationContext); 
    }

    private void RemoveEPCacheEntry(string key, EPCacheEntry epCacheEntry, BitSet flag, String writethruProvider)  
    {
        CacheEntry entry = epCacheEntry.CacheEntry;
        object lockID = null;
        LockAccessType lockAccess = LockAccessType.IGNORE_LOCK;
        if (epCacheEntry.LockHandle != null) {
            lockAccess = LockAccessType.DEFAULT;
            lockID = epCacheEntry.LockHandle.LockId;
        }
        
        OperationContext operationContext = new OperationContext(OperationContextFieldName.OperationType, OperationContextOperationType.CacheOperation);
        operationContext.Add(OperationContextFieldName.RaiseCQNotification, true);

        _cacheRoot.Remove(key, flag, null, lockID, 0, lockAccess, writethruProvider, operationContext);
    }

    private BitSet GetDSFlag(BitSet flagMap, DSWriteOption dsWriteOption) {
        if (flagMap == null) {
            flagMap = new BitSet();
        }

        switch (dsWriteOption) {
            case DSWriteOption.WriteBehind: {
                flagMap.SetBit((byte) BitSetConstants.WriteBehind);
            }
            break;
            case DSWriteOption.WriteThru:
            {
                flagMap.SetBit((byte) BitSetConstants.WriteThru);
            }
            break;
            case DSWriteOption.OptionalWriteThru:
            {
                flagMap.SetBit((byte) BitSetConstants.WriteThru);
                flagMap.SetBit((byte) BitSetConstants.OptionalDSOperation);
            }
            break;
        }

        return flagMap;
    }

    /**
     * Performs application-defined tasks associated with freeing, releasing, or
     * resetting unmanaged resources.
     */
    public void Dispose() {

    }

    private CacheEntry MakeCacheEntry(CacheEntry entry, Object value) 
    {        
        if (entry == null) {
            entry = new CacheEntry(value, null, null);
        }
        else
        {
            if (entry.Value is CallbackEntry)
            {
                ((CallbackEntry) entry.Value).Value = value;
            }
            else
            {                
                entry.Value = value;                
            } 
        }
        
        return entry;
    }



    }
}

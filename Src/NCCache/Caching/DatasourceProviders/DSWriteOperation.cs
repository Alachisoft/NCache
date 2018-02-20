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
using System.Collections;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Caching.EvictionPolicies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Util;

namespace Alachisoft.NCache.Caching.DatasourceProviders
{
    internal class DSWriteOperation : ICompactSerializable
    {
        /// <summary> Key of this operation </summary>
        protected object _key;
        /// <summary> item. </summary>
        protected CacheEntry _entry;
        /// <summary>operation type</summary>
        protected OpCode _opCode;

        protected WriteOperation _writeOperation = null;
     
        /// <summary></summary>
        protected string _providerName;
        protected string _cacheId;
        protected int _retryCount = 0;
        protected CacheRuntimeContext _context;


        public DSWriteOperation(CacheRuntimeContext context, Object key, CacheEntry entry, OpCode opcode, string providerName)
        {
            this._key = key;
            this._entry = entry;
            this._opCode = opcode;
            this._providerName = providerName;
            this._cacheId = context.SerializationContext;
            this._context = context;

        }

        /// <summary>
        /// Get key associate with the this task
        /// </summary>
        public Object Key
        {
            get { return _key; }
        }
     
        /// <summary>
        /// Get cache entry
        /// </summary>
        public CacheEntry Entry
        {
            get { return _entry; }
        }

        /// <summary>
        /// Get operation type
        /// </summary>
        public OpCode OperationCode
        {
            get { return _opCode; }
        }

        /// <summary>
        /// Get provider name
        /// </summary>
        public string ProviderName
        {
            get { return _providerName; }
            set { _providerName = value; }
        }
        public int RetryCount
        {
            get { return _retryCount; }
            set { _retryCount = value; }
        }
        public WriteOperation WriteOperation
        {
            get { return this._writeOperation; }
            set { this._writeOperation = value; }
        }

        public WriteOperation GetWriteOperation(LanguageContext languageContext, OperationContext operationContext)
        {

            object value;
            ProviderCacheItem providerCacheItem = null;
            if (_writeOperation != null)
                return new WriteOperation(_writeOperation.Key, _writeOperation.ProviderCacheItem, _writeOperation.OperationType, this._retryCount);
            if (_entry != null)
            {
                if (_entry.Value is CallbackEntry)
                {
                    value = ((CallbackEntry)_entry.Value).Value;
                }
                else
                {
                    value = _entry.Value;
                }

                if (value != null)
                {
                    if (_opCode != OpCode.Remove) // Don't need data for remove operation
                    {
                        value = _context.CacheWriteThruDataService.GetCacheData(value, _entry.Flag);
                    }
                }


                providerCacheItem = GetProviderCacheItemFromCacheEntry(_entry, value, operationContext);
            }
    
            //WriteOperations
            return new WriteOperation(_key.ToString(), providerCacheItem, SetWriteOperationType(_opCode), _retryCount);
        }
        
        WriteOperationType SetWriteOperationType(OpCode opCode)
        {
            switch (opCode)
            {
                case OpCode.Add:
                    return WriteOperationType.Add;
                case OpCode.Update:
                    return WriteOperationType.Update;
                case OpCode.Remove:
                    return WriteOperationType.Delete;
            }
            return WriteOperationType.Add;
        }
        private Object Deserialize(LanguageContext languageContext, Object value, BitSet flag)
        {
            switch (languageContext)
            {
                case LanguageContext.DOTNET:
                    value = SerializationUtil.SafeDeserialize(value, _cacheId, flag);
                    break;

            }
            return value;
        }
        
        ProviderCacheItem GetProviderCacheItemFromCacheEntry(CacheEntry cacheEntry, object value, OperationContext operationContext)
        {
            ProviderCacheItem providerCacheItem = new ProviderCacheItem(value);
            
            if (cacheEntry.EvictionHint != null && cacheEntry.EvictionHint._hintType == EvictionHintType.PriorityEvictionHint)
            {

                providerCacheItem.ItemPriority = ((PriorityEvictionHint)cacheEntry.EvictionHint).Priority;
            }
            else
            {
                providerCacheItem.ItemPriority = cacheEntry.Priority;
            }
            if (cacheEntry.GroupInfo != null)
            {
                providerCacheItem.Group = cacheEntry.GroupInfo.Group;
                providerCacheItem.SubGroup = cacheEntry.GroupInfo.SubGroup;
            }

            DateTime absoluteExpiration = DateTime.MaxValue.ToUniversalTime();
            TimeSpan slidingExpiration = TimeSpan.Zero;

            ExpirationHint hint = cacheEntry.ExpirationHint;
            if (hint != null)
            {
                providerCacheItem.ResyncItemOnExpiration = hint.NeedsReSync;
                AutoExpiration.DependencyHelper helper = new AutoExpiration.DependencyHelper();
                providerCacheItem.Dependency = helper.GetActualCacheDependency(hint, ref absoluteExpiration, ref slidingExpiration);
            }

            if (absoluteExpiration != DateTime.MaxValue.ToUniversalTime())
            {
                providerCacheItem.AbsoluteExpiration = absoluteExpiration.ToLocalTime();
            }
            providerCacheItem.SlidingExpiration = slidingExpiration;

            if (cacheEntry.QueryInfo != null)
            {
               
                if (cacheEntry.QueryInfo["tag-info"] != null)
                {
                    Hashtable tagInfo = cacheEntry.QueryInfo["tag-info"] as Hashtable;
                    if (tagInfo != null)
                    {
                        ArrayList tagsList = tagInfo["tags-list"] as ArrayList;
                        if (tagsList != null && tagsList.Count > 0)
                        {
                            Tag[] tags = new Tag[tagsList.Count];
                            int i = 0;
                            foreach (string tag in tagsList)
                            {
                                tags[i++] = new Tag(tag);
                            }

                            providerCacheItem.Tags = tags;
                        }
                    }
                }

                if (cacheEntry.QueryInfo["named-tag-info"] != null)
                {
                    Hashtable tagInfo = cacheEntry.QueryInfo["named-tag-info"] as Hashtable;
                    if (tagInfo != null)
                    {
                        Hashtable tagsList = tagInfo["named-tags-list"] as Hashtable;
                        if (tagsList != null)
                        {
                            NamedTagsDictionary namedTags = new NamedTagsDictionary();

                            foreach (DictionaryEntry tag in tagsList)
                            {
                                Type tagType = tag.Value.GetType();
                                string tagKey = tag.Key.ToString();

                                if (tagType == typeof(int))
                                {
                                    namedTags.Add(tagKey, (int)tag.Value);
                                }
                                else if (tagType == typeof(long))
                                {
                                    namedTags.Add(tagKey, (long)tag.Value);
                                }
                                else if (tagType == typeof(float))
                                {
                                    namedTags.Add(tagKey, (float)tag.Value);
                                }
                                else if (tagType == typeof(double))
                                {
                                    namedTags.Add(tagKey, (double)tag.Value);
                                }
                                else if (tagType == typeof(decimal))
                                {
                                    namedTags.Add(tagKey, (decimal)tag.Value);
                                }
                                else if (tagType == typeof(bool))
                                {
                                    namedTags.Add(tagKey, (bool)tag.Value);
                                }
                                else if (tagType == typeof(char))
                                {
                                    namedTags.Add(tagKey, (char)tag.Value);
                                }
                                else if (tagType == typeof(string))
                                {
                                    namedTags.Add(tagKey, (string)tag.Value);
                                }
                                else if (tagType == typeof(DateTime))
                                {
                                    namedTags.Add(tagKey, (DateTime)tag.Value);
                                }
                            }

                            if (namedTags.Count > 0)
                            {
                                providerCacheItem.NamedTags = namedTags;
                            }
                        }
                    }
                }

            }
            providerCacheItem.ResyncProviderName = cacheEntry.ResyncProviderName;
            return providerCacheItem;
        }

        public void Deserialize(CompactReader reader)
        {
            _key = reader.ReadObject();
            _entry = reader.ReadObject() as CacheEntry;
            _opCode =(OpCode)reader.ReadInt32();
            _providerName = reader.ReadObject() as string;
            _cacheId = reader.ReadObject() as string;
            _retryCount = reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_key);
            writer.WriteObject(_entry);
            writer.Write((int)_opCode);
            writer.WriteObject(_providerName);
            writer.WriteObject(_cacheId);
            writer.Write(_retryCount);
        }
    }
}
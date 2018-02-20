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
using Alachisoft.NCache.Runtime.MapReduce;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching;
using System.Collections;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.MapReduce.InputProviders
{
    internal class CacheInputProvider : MapReduceInput
    {
        private QueryFilter queryFilter = null;
        private CacheBase inputSource = null;
        private IList keyList = null;
        private IEnumerator keysEnumerator = null;
        private bool isDataSerialized = true;
        private string serializationContext = null;
        private CacheRuntimeContext context = null;

        public CacheInputProvider(CacheBase source, QueryFilter filter, CacheRuntimeContext context)
        {
            this.queryFilter = filter;
            this.inputSource = source;
            this.context = context;
            if (this.context != null)
                this.serializationContext = this.context.SerializationContext;
        }

        public void Initialize(System.Collections.Hashtable parameters)
        {
        }

        public void LoadInput()
        {
            try
            {
                if (this.queryFilter != null)
                {
                    QueryResultSet result = inputSource.Search(queryFilter.Query, queryFilter.Parameters, new OperationContext());
                    if (result != null && result.SearchKeysResult != null && result.SearchKeysResult.Count > 0)
                    {
                        keyList = result.SearchKeysResult;
                        if (inputSource.Context.NCacheLog.IsInfoEnabled)
                            inputSource.Context.NCacheLog.Info("InputProvider.LoadQuery", "Task input loaded, " + keyList.Count + " keys found.");
                    }
                }
                else 
                {
                    keyList = new ArrayList(inputSource.Keys);
                    if (inputSource.Context.NCacheLog.IsInfoEnabled)
                        inputSource.Context.NCacheLog.Info("InputProvider.LoadKeys", "Task input loaded, " + keyList.Count + " keys found.");
                }

                if (keyList != null)
                    keysEnumerator = keyList.GetEnumerator();

            }
            catch (Exception ex)
            {
                inputSource.Context.NCacheLog.Error("InputProvider.LoadInput", "" + ex.Message);
                throw new OperationFailedException(ex.Message);
            }
        }

        public object Current
        {
            get 
            {
                DictionaryEntry pair = new DictionaryEntry();
                try
                {
                    string key = (string)keysEnumerator.Current;

                    OperationContext oc = new OperationContext();
                    CacheEntry entry = inputSource.Get(key, false, oc);

                    object value = null;
                    if (entry != null)
                    {
                        if (entry.Value is CallbackEntry)
                            value = ((CallbackEntry)entry.Value).Value;
                        else
                            value = entry.Value;

                        if (value != null)
                        {
                            if (context.InMemoryDataFormat == Common.Enum.DataFormat.Binary)
                            {
                                value = context.CachingSubSystemDataService.GetCacheData(value, entry.Flag);
                            }
                            else if (context.InMemoryDataFormat == Common.Enum.DataFormat.Object)
                            {
                                BitSet bitset = new BitSet();
                                value = context.CachingSubSystemDataService.GetClientData(value, ref bitset, Common.Util.LanguageContext.DOTNET);
                            }
                            pair = new DictionaryEntry(key, value);
                        }
                        
                    }

                    return pair;
                }
                catch (Exception ex)
                {
                    if (context != null && context.NCacheLog != null && context.NCacheLog.IsErrorEnabled)
                        context.NCacheLog.Error("CacheInputProvider.Current", ex.Message);
                    //throw ex;
                }
                return pair;
            }
        }

        public bool MoveNext()
        {
            if (keysEnumerator != null && keysEnumerator.MoveNext())
                return true;
            else
                return false;
        }

        public void Reset()
        { }

        public DictionaryEntry Entry
        {
            get { return (DictionaryEntry)Current; }
        }

        public object Key
        {
            get { return ((DictionaryEntry)Current).Key; }
        }

        public object Value
        {
            get { return ((DictionaryEntry)Current).Value; }
        }
    }
}

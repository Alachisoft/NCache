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
using System.Collections;
using Alachisoft.NCache.Caching.Topologies;

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// provides Enumerator over replicated client cache
    /// </summary>
    internal class LazyKeysetEnumerator : IDictionaryEnumerator
    {
        /// <summary> Parent of the enumerator. </summary>
        protected CacheBase					_cache = null;

        /// <summary> The list of keys to enumerate over. </summary>
        protected object[]					_keyList = null;

        /// <summary> The current position of the enumeration. </summary>
        protected int						_current = 0;

        /// <summary> Flag to indicate invalid state of enumerator. </summary>
        protected bool						_bvalid = false;

        /// <summary> Flag to allow the enumerator to return null object values. </summary>
        protected bool						_bAllowNulls = false;

        /// <summary> Holder for current dictionary entry. </summary>
        protected DictionaryEntry			_de;

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="keyList"></param>
        /// <param name="bAllowNulls"></param>
        public LazyKeysetEnumerator(CacheBase cache, object[] keyList, bool bAllowNulls)
        {
            _cache = cache;
            _keyList = keyList;	
            _bAllowNulls = bAllowNulls;
            ((IEnumerator)this).Reset();
        }
		
        #region	/                 --- IEnumerator ---           /

        /// <summary>
        /// Set the enumerator to its initial position. which is before the first element in 
        /// the collection
        /// </summary>
        void IEnumerator.Reset()
        {
            _bvalid = false;
            _current = 0;
        }
		
        /// <summary>
        /// Advance the enumerator to the next element of the collection 
        /// </summary>
        /// <returns></returns>
        bool IEnumerator.MoveNext()
        {
            _bvalid = _keyList != null && _current < _keyList.Length;

            if(_bvalid)
            {
                _de = new DictionaryEntry(_keyList[_current++], null);
            }

            return _bvalid;
        }

        /// <summary>
        /// Gets the current element in the collection
        /// </summary>
        object IEnumerator.Current 
        {
            get 
            {
                if(!_bvalid) throw new InvalidOperationException();
                if(_de.Value == null)
                {
                    try
                    {
                        _de.Value = FetchObject(_de.Key,new OperationContext(OperationContextFieldName.OperationType,OperationContextOperationType.CacheOperation));
                    }
                    catch(Exception e)
                    {
                        _cache.NCacheLog.Error("LazyKeysetEnumerator.Current",  e.Message);
                    }
                    if(!_bAllowNulls && (_de.Value == null))
                        throw new InvalidOperationException();
                }
                return _de;
            }
        }

        #endregion

        #region	/                 --- IDictionaryEnumerator ---           /

        /// <summary>
        /// Gets the key and value of the current dictionary entry.
        /// </summary>
        DictionaryEntry IDictionaryEnumerator.Entry
        {
            get 
            {
                if(!_bvalid) throw new InvalidOperationException();
                return _de; 
            }
        }

        /// <summary>
        ///	Gets the key of the current dictionary entry  
        /// </summary>
        object IDictionaryEnumerator.Key
        {
            get 
            { 
                if(!_bvalid) throw new InvalidOperationException();
                return _de.Key; 
            }
        }

        /// <summary>
        /// Gets the value of the current dictionary entry
        /// </summary>
        object IDictionaryEnumerator.Value 
        {
            get
            {
                if(!_bvalid) throw new InvalidOperationException();
                if(_de.Value == null)
                {
                    try
                    {
                        _de.Value = FetchObject(_de.Key,new OperationContext(OperationContextFieldName.OperationType,OperationContextOperationType.CacheOperation));
                    }
                    catch (NullReferenceException) { }
                    catch (Exception e)
                    {
                        _cache.NCacheLog.Error("LazyKeysetEnumerator.Value", e.ToString());
                    }
                    if(!_bAllowNulls && (_de.Value == null))
                        throw new InvalidOperationException();
                }
                return _de.Value;
            }
        }			

        #endregion

        /// <summary>
        /// Does the lazy loading of object. This method is virtual so containers can customize object 
        /// fetching logic.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected virtual object FetchObject(object key, OperationContext operationContext)
        {
            operationContext.Add(OperationContextFieldName.GenerateQueryInfo, true);
            return _cache.Get(key,operationContext);
        }
    }
}
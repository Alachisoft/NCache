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

namespace Alachisoft.NCache.Caching.Util
{
    /// <summary>
    /// Provides Enumerator over Cache. This has to be marshall by ref so that it can be
    /// used outside the service as well.
    /// </summary>
    internal class CacheEnumerator : MarshalByRefObject, IDictionaryEnumerator
	{
		/// <summary> Enumerator over Keys  </summary>
		IDictionaryEnumerator _enumerator;
        /// <summary> Dictionary Entry  </summary>
		DictionaryEntry	_de;
		/// <summary>Cache name used for deserialization. </summary>
		string _cacheContext;
		/// <summary>
		/// Constructs CacheStoreBase Enumerator 
		/// </summary>
		/// <param name="c"></param>
		/// <param name="etr"></param>
		public CacheEnumerator(string cacheContext,IDictionaryEnumerator enumerator)
		{
			_enumerator = enumerator;				
			_cacheContext = cacheContext;
		}

		#region	/                 --- IEnumerator ---           /

		/// <summary>
		/// Set the enumerator to its initial position. which is before the first element in the collection
		/// </summary>
		void IEnumerator.Reset()
		{
			_enumerator.Reset();				
		}
		/// <summary>
		/// Advance the enumerator to the next element of the collection 
		/// </summary>
		/// <returns></returns>
		bool IEnumerator.MoveNext()
		{
			if(_enumerator.MoveNext())
			{
				_de = new DictionaryEntry(_enumerator.Key, null);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Gets the current element in the collection
		/// </summary>
		object IEnumerator.Current 
		{
			get 
			{
				if(_de.Value == null )
				{
					_de.Value = FetchObject();
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
				if(_de.Value == null )
				{
					_de.Value = FetchObject();
				}	
				return _de;
			}
		}

		/// <summary>
		/// Gets the key of the current dictionary entry 
		/// </summary>
		object IDictionaryEnumerator.Key
		{
			get
			{
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
				if(_de.Value == null )
				{
					_de.Value = FetchObject();
				}	
				return _de.Value;
			}
		}			

		#endregion

		/// <summary>
		/// Does the lazy loading of object. 
		/// </summary>
		/// <returns></returns>
		protected object FetchObject()
		{
			CacheEntry e = _enumerator.Value as CacheEntry;
            return e;
		}
	}
}

// Copyright (c) 2017 Alachisoft
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
using Enyim.Caching.Memcached;
using System.Collections.Generic;

namespace Enyim.Caching
{
	public interface IMemcachedClient : IDisposable
	{
		object Get(string key);
		T Get<T>(string key);
		IDictionary<string, object> Get(IEnumerable<string> keys);

		bool TryGet(string key, out object value);
		bool TryGetWithCas(string key, out CasResult<object> value);

		CasResult<object> GetWithCas(string key);
		CasResult<T> GetWithCas<T>(string key);
		IDictionary<string, CasResult<object>> GetWithCas(IEnumerable<string> keys);

		bool Append(string key, ArraySegment<byte> data);
		CasResult<bool> Append(string key, ulong cas, ArraySegment<byte> data);

		bool Prepend(string key, ArraySegment<byte> data);
		CasResult<bool> Prepend(string key, ulong cas, ArraySegment<byte> data);

		bool Store(StoreMode mode, string key, object value);
		bool Store(StoreMode mode, string key, object value, DateTime expiresAt);
		bool Store(StoreMode mode, string key, object value, TimeSpan validFor);

		CasResult<bool> Cas(StoreMode mode, string key, object value);
		CasResult<bool> Cas(StoreMode mode, string key, object value, ulong cas);
		CasResult<bool> Cas(StoreMode mode, string key, object value, DateTime expiresAt, ulong cas);
		CasResult<bool> Cas(StoreMode mode, string key, object value, TimeSpan validFor, ulong cas);

		ulong Decrement(string key, ulong defaultValue, ulong delta);
		ulong Decrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt);
		ulong Decrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor);

		CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, ulong cas);
		CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, DateTime expiresAt, ulong cas);
		CasResult<ulong> Decrement(string key, ulong defaultValue, ulong delta, TimeSpan validFor, ulong cas);

		ulong Increment(string key, ulong defaultValue, ulong delta);
		ulong Increment(string key, ulong defaultValue, ulong delta, DateTime expiresAt);
		ulong Increment(string key, ulong defaultValue, ulong delta, TimeSpan validFor);

		CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, ulong cas);
		CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, DateTime expiresAt, ulong cas);
		CasResult<ulong> Increment(string key, ulong defaultValue, ulong delta, TimeSpan validFor, ulong cas);

		bool Remove(string key);

		void FlushAll();

		ServerStats Stats();
		ServerStats Stats(string type);

		event Action<IMemcachedNode> NodeFailed;
	}
}

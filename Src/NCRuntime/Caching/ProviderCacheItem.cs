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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;

#if JAVA
using Alachisoft.TayzGrid.Runtime;
#else
using Alachisoft.NCache.Runtime;
#endif
#if JAVA
using Alachisoft.TayzGrid.Runtime.Dependencies;
#else
using Alachisoft.NCache.Runtime.Dependencies;
#endif 
#if JAVA
using Alachisoft.TayzGrid.Runtime.Caching;
#else
using Alachisoft.NCache.Runtime.Caching;
#endif

#if JAVA
namespace Alachisoft.TayzGrid.Runtime.Caching
#else
namespace Alachisoft.NCache.Runtime.Caching
#endif
{
    /// <remark>
    /// This Feature is Not Available in Express
    /// </remark>
    public class ProviderCacheItem 
{
		private object _value;
        private DateTime _absoluteExpiration = DateTime.MaxValue.ToUniversalTime();
		private TimeSpan _slidingExpiration = TimeSpan.Zero;
		private CacheItemPriority _priority = CacheItemPriority.Default;
		private CacheDependency _dependency;
		private string _resyncProviderName;
		private Tag[] _tags;
		private string _group;
		private string _subGroup;
		private bool _resyncItem;
        private bool _isJavaReadThrough;
        private NamedTagsDictionary _namedTags;
		/// <summary>
		/// Initializes an instance of CacheItem.
		/// </summary>
		/// <param name="value"></param>
		public ProviderCacheItem(object value)
		{
			this._value = value;
		}

		/// <summary>
		/// Gets/Sets the group of cache item.
		/// </summary>
		public string Group
		{
			get { return _group; }
			set { _group = value; }
		}

		/// <summary>
		/// Gets/Sets the sub group of cache item.
		/// </summary>
		public string SubGroup
		{
			get { return _subGroup; }
			set { _subGroup = value; }
		}

		/// <summary>
		/// Gets/Sets the Tags for the cache item.
		/// </summary>
        public Tag[] Tags 
		{
			get { return _tags; }
			set { _tags = value; }
		}

        /// <summary>
        /// Gets/Sets the Tags for the cache item.
        /// </summary>
        public NamedTagsDictionary NamedTags
        {
            get { return _namedTags; }
            set { _namedTags = value; }
        }
		/// <summary>
		/// Gets/Sets the flag which indicates whether item should be reloaded on
		/// expiration if ReadThru provider is specified.
		/// </summary>
		public bool ResyncItemOnExpiration
		{
			get { return _resyncItem; }
			set { _resyncItem = value; }
		}

		/// <summary>
		/// Gets/Sets the CacheDepedency.
		/// </summary>
		public CacheDependency Dependency
		{
			get { return _dependency; }
			set { _dependency = value; }
		}

		/// <summary>
		/// Gets/Sets absolute expiration.
		/// </summary>
		public DateTime AbsoluteExpiration
		{
			get { return _absoluteExpiration; }
			set { _absoluteExpiration = value; }
		}

		/// <summary>
		/// Gets/Sets the sliding expiration.
		/// </summary>
		public TimeSpan SlidingExpiration
		{
			get { return _slidingExpiration; }
			set { _slidingExpiration = value; }
		}

		/// <summary>
		/// Gets/Sets the priority of item.
		/// </summary>
		public CacheItemPriority ItemPriority
		{
			get { return _priority; }
			set { _priority = value; }
		}

		/// <summary>
		/// Gets/Sets the value of CacheItem.
		/// </summary>
		public object Value
		{
			get { return _value; }
			set { _value = value; }
		}

		/// <summary>
		/// Gets/Sets Provider name for re-synchronization of cache
		/// </summary>
		public string ResyncProviderName
		{
			get { return _resyncProviderName; }
			set { _resyncProviderName = value; }
		}
    /// <summary>
        /// Gets/Sets ReadThruough if ReadThru is a type of Java 
    /// </summary>
    public bool IsJavaReadThrough
    {
        get { return _isJavaReadThrough; }
        set { _isJavaReadThrough = value; }
    }
	}
}

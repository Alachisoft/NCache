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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Defines a callback method for notifying applications when a cached item 
    /// is removed from the <see cref="Cache"/>.
    /// </summary>
    /// <param name="key">The index location for the item removed from the cache.</param>
    /// <param name="value">The object item removed from the cache.</param>
    /// <param name="reason">The reason the item was removed from the cache, as specified by 
    /// the <see cref="CacheItemRemovedReason"/> enumeration.</param>
    /// <remarks>Since this handler is invoked every time an item is removed from the <see cref="Cache"/>, doing a lot
    /// of processing inside the handler might have an impact on the performance of the cache and cluster. It
    /// is therefore advisable to do minimal processing inside the handler.
    /// </remarks>
    /// <example>The following example demonstrates how to use the <see cref="CacheItemRemovedCallback"/> class to notify 
    /// an application when an item is removed from the application's <see cref="Cache"/> object. You could include this 
    /// code in a code declaration block in the Web Forms page, or in a page code-behind file.
    /// <code>
    /// 
    /// static bool itemRemoved = false;
    /// static CacheItemRemovedReason reason;
    /// 
    /// CacheItemRemovedCallback onRemove = null;
    ///
    ///	public void RemovedCallback(string k, object v, CacheItemRemovedReason r)
    ///	{
    ///		itemRemoved = true;
    ///		reason = r;
    ///	}
    ///
    ///	public void AddItemToCache(object sender, EventArgs e) 
    ///	{
    ///		itemRemoved = false;
    ///		onRemove = new CacheItemRemovedCallback(this.RemovedCallback);
    ///		if (Cache["Key1"] == null)
    ///			NCache.Cache.Insert("Key1", "Value 1", null, DateTime.Now.AddMinutes(60), TimeSpan.Zero, CacheItemPriority.High, onRemove);
    ///	}
    ///	
    /// </code>
    /// </example>
    /// <requirements>
    /// <constraint>This member is not available in SessionState edition.</constraint> 
    /// </requirements>
    [Obsolete("This delegate is deprecated. 'Please use CacheDataNotificationCallback(string key, CacheEventArg cacheEventArgs)'", false)]
    internal delegate void CacheItemRemovedCallback(string key, object value, CacheItemRemovedReason reason);
}
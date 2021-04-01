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

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// Defines a callback method for notifying applications when an item 
    /// is updated in the <see cref="Cache"/>.
    /// </summary>
    /// <param name="key">The cache key used to reference the item.</param>
    /// <param name="cacheEventArgs">The cache event arguments.</param>
    /// <remarks>Since this handler is invoked every time an item is removed from the <see cref="Cache"/>, doing a lot
    /// of processing inside the handler might have an impact on the performance of the cache and cluster. It
    /// is therefore advisable to do minimal processing inside the handler.
    /// </remarks>
    /// <example>The following example demonstrates how to use the <see cref="CacheDataNotificationCallback"/> class to notify 
    /// an application when an item is updated to the application's <see cref="Cache"/> object. You could include this 
    /// code in a code declaration block in the Web Forms page, or in a page code-behind file.
    /// <code>
    /// 
    ///	public static void OnCacheDataModification(string key, CacheEventArg args)
    ///{
    ///         //
    ///}
    ///
    ///	protected void Application_Start(object sender, EventArgs e)
    ///	{
    ///		try
    ///		{
    ///			ICache cache = CacheManager.GetCache("myCache");
    ///			CacheDataNotificationCallback dataNotificationCallback = new CacheDataNotificationCallback(OnCacheDataModification);
    ///		}
    ///		catch(Exception e)
    ///		{
    ///		}
    ///	}
    ///	
    /// </code>
    /// </example>
    public delegate void CacheDataNotificationCallback(string key, CacheEventArg cacheEventArgs);
}
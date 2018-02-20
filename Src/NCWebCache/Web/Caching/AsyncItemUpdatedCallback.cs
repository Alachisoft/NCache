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
namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Defines a callback method for notifying applications when a request 
    /// for asynchronous insert operation completes.
    /// </summary>
    /// <param name="key">The cache key used to reference the item.</param>
    /// <param name="result">It indicates the result of the async operation. If the operation 
    /// completes successfully, the result contains <see cref="Alachisoft.NCache.Caching.AsyncOpResult.Success"/> otherwise it contains
    /// an <see cref="Alachisoft.NCache.Runtime.Exceptions.OperationFailedException"/> describing the cause of the operation failure.</param>
    /// <remarks>Since this handler is invoked every time an item is removed from the <see cref="Cache"/>, doing a lot
    /// of processing inside the handler might have an impact on the performance of the cache and cluster. It
    /// is therefore advisable to do minimal processing inside the handler.
    /// </remarks>
    /// <example>The following example demonstrates how to use the <see cref="AsyncItemUpdatedCallback"/> class to notify 
    /// an application when an item is asynchronously updated in the <see cref="Cache"/> object. You could include this 
    /// code in a code declaration block in the Web Forms page, or in a page code-behind file.
    /// <code>
    /// 
    ///	public void OnAsyncItemUpdated(string key, object result)
    ///	{
    ///		// ...
    ///	}
    ///
    ///	protected void Application_Start(object sender, EventArgs e)
    ///	{
    ///		try
    ///		{
    ///			NCache.InitializeCache("myCache");
    ///         CacheItem item = new CacheItem("value");
    ///         item.AsyncItemUpdateCallback = new AsyncItemUpdateCallback(OnAsyncItemUpdated);
    ///			NCache.Cache.InsertAsync("key", item);
    ///		}
    ///		catch(Exception e)
    ///		{
    ///		}
    ///	}
    ///	
    /// </code>
    /// </example>
    public delegate void AsyncItemUpdatedCallback(string key, object result);
}
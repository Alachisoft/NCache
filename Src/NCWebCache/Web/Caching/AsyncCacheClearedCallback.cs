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
    /// for asynchronous cache clear operation completes.
    /// </summary>
    /// <param name="result">The result of the asynchronous operation. If the operation completes successfully, it contains
    /// <see cref="Alachisoft.NCache.Caching.AsyncOpResult.Success"/> otherwise it contains an <see cref="Alachisoft.NCache.Runtime.Exceptions.OperationFailedException"/> 
    /// indicating the cause of the operation failure.</param>
    /// <remarks>Since this handler is invoked every time an item is removed from the <see cref="Cache"/>, doing a lot
    /// of processing inside the handler might have an impact on the performance of the cache and cluster. It
    /// is therefore advisable to do minimal processing inside the handler.
    /// </remarks>
    /// <example>The following example demonstrates how to use the <see cref="AsyncCacheClearedCallback"/> class to notify 
    /// an application when application's <see cref="Cache"/> object is cleared asynchronously. You could include this 
    /// code in a code declaration block in the Web Forms page, or in a page code-behind file.
    /// <code>
    /// 
    ///	public void OnAsyncCacheCleared(object result)
    ///	{
    ///		// ...
    ///	}
    ///
    ///	protected void Application_Start(object sender, EventArgs e)
    ///	{
    ///		try
    ///		{
    ///			NCache.InitializeCache("myCache");
    ///			NCache.Cache.ClearAsync(new AsyncCacheClearedCallback(OnAsyncCacheCleared));
    ///		}
    ///		catch(Exception e)
    ///		{
    ///		}
    ///	}
    ///	
    /// </code>
    /// </example>
    public delegate void AsyncCacheClearedCallback(object result);
}
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

namespace Alachisoft.NCache.Web.Caching
{
    /// <summary>
    /// Defines a callback method for notifying applications when the <see cref="Cache"/> is stopped.
    /// </summary>
    /// <param name="cacheId">The Identification of the cache being stopped. It is helpful as multiple cache instances 
    /// can exists with in the same application.</param>
    /// <remarks>This handler is invoked when a <see cref="Cache"/> is stopped.</remarks>
    /// <example>The following example demonstrates how to use the <see cref="CacheStoppedCallback"/> class to notifiy 
    /// an application when a cache is stopped. You could include this 
    /// code in a code declaration block in the Web Forms page, or in a page code-behind file.
    /// <code>
    /// 
    ///	public void OnCacheStopped(string cacheId)
    ///	{
    ///		// ...
    ///	}
    ///
    ///	protected void Application_Start(object sender, EventArgs e)
    ///	{
    ///		try
    ///		{
    ///			NCache.InitializeCache("myCache");
    ///			NCache.Cache.CacheStopped += new CacheStoppedCallback(this.OnCacheStopped);
    ///		}
    ///		catch(Exception e)
    ///		{
    ///		}
    ///	}
    ///	
    /// </code>
    /// </example>
    internal delegate void CacheStoppedCallback(string cacheId);
}

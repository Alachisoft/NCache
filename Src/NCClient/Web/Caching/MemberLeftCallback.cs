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
    /// Defines a callback method for notifying applications when a node leaves the cluster.
    /// </summary>
    /// <param name="nodeInfo">The information of the member that has left the cluster. It describes the member in terms of the IP Address
    /// and server port.</param>
    /// <remarks>This handler is invoked every time a node leaves the cluster.
    /// </remarks>
    /// <example>The following example demonstrates how to use the <see cref="MemberLeftCallback"/> class to notify 
    /// an application when a node leaves the cluster. You could include this 
    /// code in a code declaration block in the Web Forms page, or in a page code-behind file.
    /// <code>
    /// 
    ///	public void OnMemberLeft(NodeInfo nodeInfo)
    ///	{
    ///		// ...
    ///	}
    ///
    ///	protected void Application_Start(object sender, EventArgs e)
    ///	{
    ///		try
    ///		{
    ///			ICache cache = CacheManager.GetCache("myCache");
    ///			cache.NotificationService.MemberLeft += new MemberLeftCallback(this.OnMemberLeft);
    ///		}
    ///		catch(Exception ex)
    ///		{
    ///		}
    ///	}
    ///	
    /// </code>
    /// </example>
    /// <requirements>
    /// <constraint>This member is only available in Enterprise edition.</constraint> 
    /// </requirements>
    public delegate void MemberLeftCallback(NodeInfo nodeInfo);
}
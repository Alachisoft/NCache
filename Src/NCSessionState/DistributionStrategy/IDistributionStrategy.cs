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
#if !NETCORE
using System.Web.SessionState;

using Alachisoft.NCache.Web.SessionStateManagement;



namespace Alachisoft.NCache.Web.SessionState.DistributionStrategy
{
    /// <summary>
    /// Implemented by classes providing session to cache and cache to session 
    /// data transfer.
    /// </summary>
    internal interface IDistributionStrategy
	{
		/// <summary>
		/// Fills the system ASP.NET session from NCache.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="cache"></param>
		/// <param name="strict"></param>
        void FillSessionFromCache(ISessionCache cache, HttpSessionState session, NSessionStateModule module, bool strict);

        /// <summary>
        /// Fills NCache from the system ASP.NET session.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="session"></param>
        /// <param name="async"></param>
        void FillCacheFromSession(ISessionCache cache, HttpSessionState session, NSessionStateModule module, bool strict, bool isAbandoned);
    }

}
#endif


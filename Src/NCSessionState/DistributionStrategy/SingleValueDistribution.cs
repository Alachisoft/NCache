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
using System;
using System.Collections;
using System.Web;
using System.Web.SessionState;
using Alachisoft.NCache.Caching.AutoExpiration;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.SessionStateManagement;




namespace Alachisoft.NCache.Web.SessionState.DistributionStrategy
{

	/// <summary>
	/// Summary description for SingleValueDistribution.
	/// </summary>
	internal class SingleValueDistribution : IDistributionStrategy
	{
		#region	/                 --- IDistributionStrategy Members ---           /

		/// <summary>
		/// Fills the system ASP.NET session from NCache.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="cache"></param>
		/// <param name="strict"></param>
        void IDistributionStrategy.FillSessionFromCache(ISessionCache cache, HttpSessionState session, NSessionStateModule module, bool strict)
		{
			try
			{
				string sessionId = session.SessionID;
				if (strict)
					session.Clear();

				IDictionaryEnumerator i = (IDictionaryEnumerator)cache.GetEnumerator();
				while (i != null && i.MoveNext())
				{
					SessionKey key = i.Key as SessionKey;
					if (key != null && key.SessionID == sessionId)
					{
						session[key.Key] = i.Value;
					}
				}
			}
			catch (Exception exc)
			{
                if(strict)
				session.Clear();
                module.RaiseExceptions(exc, "SingleValueDistribution.FillSessionFromCache");
                //if (exceptionsEnabled) throw;
			}
		}

		/// <summary>
		/// Fills NCache from the system ASP.NET session.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="session"></param>
		/// <param name="strict"></param>
		/// <param name="async"></param>
        void IDistributionStrategy.FillCacheFromSession(ISessionCache cache, HttpSessionState session, NSessionStateModule module, bool strict, bool isAbandoned)
        {
            if (cache == null) return;

            string sessionId = session.SessionID;
            SessionKey key = new SessionKey(sessionId, module.ApplicationId);
            try
            {
               
                if (session.Count == 0) //We need not to keep any empty session in the cache. [Asif Imam] April 09, 08 (This is incomplete. As it is never used. See Monolithic strategy for detail use-case)
                    return;
                    
                    cache.Remove(sessionId, key.ToString(), false);

                    cache.Insert(sessionId, key.ToString(), DateTime.Now,  Alachisoft.NCache.Runtime.Caching.ExpirationConstants.AbsoluteNoneExpiration, TimeSpan.FromMinutes(session.Timeout), CacheItemPriority.NotRemovable);

                    
                    string[] tempStrArr = new string[1];
                    tempStrArr[0] = key.ToString();
                    foreach (string skey in session.Keys)
                    {

                        cache.Insert(sessionId, SessionKey.CompositeKey(sessionId, skey), session[skey], Alachisoft.NCache.Runtime.Caching.ExpirationConstants.AbsoluteNoneExpiration, Alachisoft.NCache.Runtime.Caching.ExpirationConstants.SlidingNoneExpiration, CacheItemPriority.NotRemovable);


                    }
               


                if (strict)
                    session.Clear();
            }
            catch (Exception exc)
            {
                module.RaiseExceptions(exc, "SingleValueDistribution.FillCacheFromSession");
                //if (exceptionsEnabled) throw;
            }
        }

		#endregion
	}
}
#endif
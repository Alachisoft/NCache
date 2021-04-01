// ===============================================================================
// Alachisoft (R) NCache Sample Code
// NCache Events sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using Alachisoft.NCache.Web.Caching;

namespace Events
{

    //using CacheDataModificationListener = Alachisoft.NCache.Web.Caching.CacheDataModificationListener;
    //using CacheEventArg = Alachisoft.NCache.Web.Events.CacheEventArg;

	public class CacheDataModificationImpl : CacheDataModificationListener
	{

		public override void cacheDataModified(string @string, CacheEventArg cea)
		{
			throw new System.NotSupportedException("Not supported yet."); //To change body of generated methods, choose Tools | Templates.
		}

		public override void cacheCleared()
		{
			throw new System.NotSupportedException("Not supported yet."); //To change body of generated methods, choose Tools | Templates.
		}

	}

}
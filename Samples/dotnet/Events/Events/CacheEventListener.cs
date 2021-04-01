using System;

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

namespace Events
{

	using CacheEvent = com.alachisoft.tayzgrid.@event.CacheEvent;
	using CacheListener = com.alachisoft.tayzgrid.@event.CacheListener;

	public class CacheEventListener : CacheListener
	{

		//These methods can be modified to suite the needs.
		//Can be customzid to include full cache and item details.

		public override void cacheCleared()
		{
			//Define your implementation here
			//Examples includes writing to log files

			Console.WriteLine("Cache cleared!");
		}

		public override void cacheItemAdded(CacheEvent ce)
		{
			Console.WriteLine("Cache: An item is added");
		}

		public override void cacheItemRemoved(CacheEvent ce)
		{
			Console.WriteLine("Cache: An item is removed");
		}

		public override void cacheItemUpdated(CacheEvent ce)
		{
			Console.WriteLine("Cache: An item is updated");
		}

	}

}
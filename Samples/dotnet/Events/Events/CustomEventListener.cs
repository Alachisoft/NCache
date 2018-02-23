using System;

// ===============================================================================
// Alachisoft (R) TayzGrid Sample Code
// TayzGrid Events sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

namespace Events
{

	using CustomEvent = com.alachisoft.tayzgrid.@event.CustomEvent;
	using CustomListener = com.alachisoft.tayzgrid.@event.CustomListener;

	public class CustomEventListener : CustomListener
	{

		public override void customEventOccured(CustomEvent ce)
		{
			Console.WriteLine("Object with key: " + ce.Key + " has raised custom event.");
		}

	}

}
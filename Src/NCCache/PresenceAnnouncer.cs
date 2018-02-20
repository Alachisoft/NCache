using System;

using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
	interface IPresenceAnnouncement
	{
		bool AnnouncePresence(bool urgent);
	}

	/// <summary>
	/// The periodic update task. Replicates cache stats to all nodes. _stats are used for
	/// load balancing as well as statistics reporting.
	/// </summary>
	class PeriodicPresenceAnnouncer : TimeScheduler.Task
	{
		/// <summary> The parent on this task. </summary>
		private IPresenceAnnouncement _parent = null;

		/// <summary> The periodic interval for stat replications. </summary>
		private long _period = 5000;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public PeriodicPresenceAnnouncer(IPresenceAnnouncement parent)
		{
			_parent = parent;
		}

		/// <summary>
		/// Overloaded Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="period"></param>
		public PeriodicPresenceAnnouncer(IPresenceAnnouncement parent, long period)
		{
			_parent = parent;
			_period = period;
		}

		/// <summary>
		/// Sets the cancell flag.
		/// </summary>
		public void Cancel()
		{
			lock (this) { _parent = null; }
		}

		/// <summary>
		/// The task is cancelled or not. 
		/// </summary>
		/// <returns></returns>
		bool TimeScheduler.Task.IsCancelled()
		{
			lock (this) { return _parent == null; }
		}

		/// <summary>
		/// The interval between replications.
		/// </summary>
		/// <returns></returns>
		long TimeScheduler.Task.GetNextInterval()
		{
			return _period;
		}

		/// <summary>
		/// Do the replication.
		/// </summary>
		void TimeScheduler.Task.Run()
		{
			if (_parent != null)
			{
				_parent.AnnouncePresence(false);
			}
		}
	}

}

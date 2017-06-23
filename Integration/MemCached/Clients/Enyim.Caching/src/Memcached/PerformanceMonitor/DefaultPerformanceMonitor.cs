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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Enyim.Caching.Memcached
{
	public class DefaultPerformanceMonitor : IPerformanceMonitor
	{
		private OpMonitor pcGet;
		private OpMonitor pcSet;
		private OpMonitor pcAdd;
		private OpMonitor pcReplace;
		private OpMonitor pcDelete;
		private OpMonitor pcIncrement;
		private OpMonitor pcDecrement;
		private OpMonitor pcAppend;
		private OpMonitor pcPrepend;

		public DefaultPerformanceMonitor(string instance)
		{
			this.pcGet = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Get);
			this.pcSet = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Set);
			this.pcAdd = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Add);
			this.pcReplace = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Replace);
			this.pcDelete = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Delete);
			this.pcIncrement = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Increment);
			this.pcDecrement = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Decrement);
			this.pcAppend = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Append);
			this.pcPrepend = new OpMonitor(instance, DefaultPerformanceMonitor.CategoryName, DefaultPerformanceMonitor.Names.Prepend);
		}

		#region [ IDisposable                  ]

		~DefaultPerformanceMonitor()
		{
			this.Dispose();
		}

		public void Dispose()
		{
			((IDisposable)this).Dispose();
		}

		void IDisposable.Dispose()
		{
			GC.SuppressFinalize(this);

			if (this.pcGet != null)
			{
				this.pcGet.Dispose();
				this.pcSet.Dispose();
				this.pcAdd.Dispose();
				this.pcReplace.Dispose();
				this.pcDelete.Dispose();
				this.pcIncrement.Dispose();
				this.pcDecrement.Dispose();
				this.pcAppend.Dispose();
				this.pcPrepend.Dispose();

				this.pcGet = null;
				this.pcSet = null;
				this.pcAdd = null;
				this.pcReplace = null;
				this.pcDelete = null;
				this.pcIncrement = null;
				this.pcDecrement = null;
				this.pcAppend = null;
				this.pcPrepend = null;
			}
		}

		#endregion
		#region [ consts                       ]

		public const string CategoryName = "Enyim.Caching.Memcached";

		internal static class Names
		{
			public const string Get = "Get";
			public const string Set = "Set";
			public const string Add = "Add";
			public const string Replace = "Replace";
			public const string Delete = "Delete";
			public const string Increment = "Increment";
			public const string Decrement = "Decrement";
			public const string Append = "Append";
			public const string Prepend = "Prepend";
		}

		#endregion
		#region  [ IPerformanceMonitor          ]

		void IPerformanceMonitor.Get(int amount, bool success)
		{
			this.pcGet.Increment(amount, success);
		}

		void IPerformanceMonitor.Store(StoreMode mode, int amount, bool success)
		{
			switch (mode)
			{
				case StoreMode.Add:
					this.pcAdd.Increment(amount, success);
					break;

				case StoreMode.Replace:
					this.pcReplace.Increment(amount, success);
					break;

				case StoreMode.Set:
					this.pcSet.Increment(amount, success);
					break;
			}
		}

		void IPerformanceMonitor.Delete(int amount, bool success)
		{
			this.pcDelete.Increment(amount, success);
		}

		void IPerformanceMonitor.Mutate(MutationMode mode, int amount, bool success)
		{
			switch (mode)
			{
				case MutationMode.Increment:
					this.pcIncrement.Increment(amount, success);
					break;

				case MutationMode.Decrement:
					this.pcDecrement.Increment(amount, success);
					break;
			}
		}

		void IPerformanceMonitor.Concatenate(ConcatenationMode mode, int amount, bool success)
		{
			switch (mode)
			{
				case ConcatenationMode.Append:
					this.pcAppend.Increment(amount, success);
					break;

				case ConcatenationMode.Prepend:
					this.pcPrepend.Increment(amount, success);
					break;
			}
		}

		#endregion
		#region [ OpMonitor                    ]

		internal class OpMonitor : IDisposable
		{
			private PerformanceCounter pcTotal;
			private PerformanceCounter pcHits;
			private PerformanceCounter pcMisses;
			private PerformanceCounter pcTotalPerSec;
			private PerformanceCounter pcHitsPerSec;
			private PerformanceCounter pcMissesPerSec;

			const string Total = " Total";
			const string Hits = " Hits";
			const string Misses = " Misses";
			const string TotalPerSec = " Total/sec";
			const string HitsPerSec = " Hits/sec";
			const string MissesPerSec = " Misses/sec";

			public OpMonitor(string instance, string category, string name)
			{
				this.pcTotal = new PerformanceCounter(category, name + Total, instance, false);
				this.pcHits = new PerformanceCounter(category, name + Hits, instance, false);
				this.pcMisses = new PerformanceCounter(category, name + Misses, instance, false);
				this.pcTotalPerSec = new PerformanceCounter(category, name + TotalPerSec, instance, false);
				this.pcHitsPerSec = new PerformanceCounter(category, name + HitsPerSec, instance, false);
				this.pcMissesPerSec = new PerformanceCounter(category, name + MissesPerSec, instance, false);

				// reste the counters to 0
				this.pcHits.RawValue = 0;
				this.pcHitsPerSec.RawValue = 0;
				this.pcMisses.RawValue = 0;
				this.pcMissesPerSec.RawValue = 0;
				this.pcTotal.RawValue = 0;
				this.pcTotalPerSec.RawValue = 0;
			}

			~OpMonitor()
			{
				this.Dispose();
			}

			public void Increment(int amount, bool success)
			{
				this.pcTotal.IncrementBy(amount);
				this.pcTotalPerSec.IncrementBy(amount);

				if (success)
				{
					this.pcHits.IncrementBy(amount);
					this.pcHitsPerSec.IncrementBy(amount);
				}
				else
				{
					this.pcMisses.IncrementBy(amount);
					this.pcMissesPerSec.IncrementBy(amount);
				}
			}

			internal static CounterCreationData[] CreateCounters(string op)
			{
				var retval = new CounterCreationData[6];

				retval[0] = new CounterCreationData(op + Total, "Total number of " + op + " operations during the client's lifetime", PerformanceCounterType.NumberOfItems64);
				retval[1] = new CounterCreationData(op + Hits, "Total number of successful " + op + " operations during the client's lifetime", PerformanceCounterType.NumberOfItems64);
				retval[2] = new CounterCreationData(op + Misses, "Total number of failed " + op + " operations during the client's lifetime", PerformanceCounterType.NumberOfItems64);

				retval[3] = new CounterCreationData(op + TotalPerSec, "Number of " + op + " operations handled by the client per second.", PerformanceCounterType.RateOfCountsPerSecond64);
				retval[4] = new CounterCreationData(op + HitsPerSec, "Number of successful " + op + " operations handled by the client per second.", PerformanceCounterType.RateOfCountsPerSecond64);
				retval[5] = new CounterCreationData(op + MissesPerSec, "Number of failed " + op + " operations handled by the client per second.", PerformanceCounterType.RateOfCountsPerSecond64);

				return retval;
			}

			public void Dispose()
			{
				((IDisposable)this.pcTotalPerSec).Dispose();
			}

			void IDisposable.Dispose()
			{
				GC.SuppressFinalize(this);

				if (this.pcHits != null)
				{
					this.pcHits.Dispose();
					this.pcHitsPerSec.Dispose();
					this.pcMisses.Dispose();
					this.pcMissesPerSec.Dispose();
					this.pcTotal.Dispose();
					this.pcTotalPerSec.Dispose();

					this.pcHits = null;
					this.pcHitsPerSec = null;
					this.pcMisses = null;
					this.pcMissesPerSec = null;
					this.pcTotal = null;
					this.pcTotalPerSec = null;
				}
			}
		}

		#endregion
	}
}

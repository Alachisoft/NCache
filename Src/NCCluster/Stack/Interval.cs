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
// $Id: Interval.java,v 1.1.1.1 2003/09/09 01:24:12 belaban Exp $

using System;

namespace Alachisoft.NGroups.Stack
{
	
	
	/// <summary> Manages retransmission timeouts. Always returns the next timeout, until the last timeout in the
	/// array is reached. Returns the last timeout from then on, until reset() is called.
	/// </summary>
	/// <author>  John Giorgiadis
	/// </author>
	/// <author>  Bela Ban
	/// </author>
	internal class Interval
	{
		private int nextInt = 0;
		private long[] interval = null;
		
		public Interval(long[] interval)
		{
			if (interval.Length == 0)
				throw new System.ArgumentException("Interval()");
			this.interval = interval;
		}
		
		public virtual long first()
		{
			return interval[0];
		}
		
		/// <returns> the next interval 
		/// </returns>
		public virtual long next()
		{
			lock (this)
			{
				if (nextInt >= interval.Length)
					return (interval[interval.Length - 1]);
				else
					return (interval[nextInt++]);
			}
		}
		
		public virtual long[] getInterval()
		{
			return interval;
		}
		
		public virtual void  reset()
		{
			lock (this)
			{
				nextInt = 0;
			}
		}
	}
}

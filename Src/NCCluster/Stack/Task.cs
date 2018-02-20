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
// $Id: AckMcastSenderWindow.java,v 1.5 2004/07/05 14:17:32 belaban Exp $
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NGroups.Stack
{
    /// <summary> The retransmit task executed by the scheduler in regular intervals</summary>
    internal abstract class Task : TimeScheduler.Task
    {
        private Interval intervals;
        private bool cancelled_Renamed_Field;

        protected internal Task(long[] intervals)
        {
            this.intervals = new Interval(intervals);
            this.cancelled_Renamed_Field = false;
        }
        public virtual long GetNextInterval()
        {
            return (intervals.next());
        }
        public virtual void cancel()
        {
            cancelled_Renamed_Field = true;
        }
        public virtual bool IsCancelled()
        {
            return (cancelled_Renamed_Field);
        }

        public virtual void Run() { }
        public string getName()
        {
            return "AckMcastSenderWindow.Task";
        }
    }
}

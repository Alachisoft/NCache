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
using System.Collections;

namespace Alachisoft.NGroups.Stack
{
    /// <summary> The entry associated with an initial group of missing messages
    /// with contiguous sequence numbers and with all its subgroups.<br>
    /// E.g.
    /// - initial group: [5-34]
    /// - msg 12 is acknowledged, now the groups are: [5-11], [13-34]
    /// <p>
    /// Groups are stored in a list as long[2] arrays of the each group's
    /// bounds. For speed and convenience, the lowest & highest bounds of
    /// all the groups in this entry are also stored separately
    /// </summary>
    internal class RetransmitterEntry : Task
    {
        private Retransmitter enclosingInstance;
        public System.Collections.ArrayList list;
        public long low;
        public long high;

        public RetransmitterEntry(Retransmitter enclosingInstance, long low, long high, long[] intervals) : base(intervals)
        {
            this.enclosingInstance = enclosingInstance;
            this.low = low;
            this.high = high;
            list = new System.Collections.ArrayList();
            list.Add(low);
            list.Add(high);
        }

        /// <summary> Remove the given seqno and resize or partition groups as
        /// necessary. The algorithm is as follows:<br>
        /// i. Find the group with low <= seqno <= high
        /// ii. If seqno == low,
        /// a. if low == high, then remove the group
        /// Adjust global low. If global low was pointing to the group
        /// deleted in the previous step, set it to point to the next group.
        /// If there is no next group, set global low to be higher than
        /// global high. This way the entry is invalidated and will be removed
        /// all together from the pending msgs and the task scheduler
        /// iii. If seqno == high, adjust high, adjust global high if this is
        /// the group at the tail of the list
        /// iv. Else low < seqno < high, break [low,high] into [low,seqno-1]
        /// and [seqno+1,high]
        /// 
        /// </summary>
        /// <param name="seqno">the sequence number to remove
        /// </param>
        public virtual void remove(long seqno)
        {
            int i;
            long loBound = -1;
            long hiBound = -1;

            lock (this)
            {
                for (i = 0; i < list.Count; i += 2)
                {
                    loBound = (long)list[i];
                    hiBound = (long)list[i + 1];

                    if (seqno < loBound || seqno > hiBound)
                        continue;
                    break;
                }
                if (i == list.Count)
                    return;

                if (seqno == loBound)
                {
                    if (loBound == hiBound)
                    {
                        list.RemoveAt(i);
                        list.RemoveAt(i);
                    }
                    else
                        list[i] = ++loBound;

                    if (i == 0)
                        low = list.Count == 0 ? high + 1 : loBound;
                }
                else if (seqno == hiBound)
                {
                    list[i + 1] = --hiBound;
                    if (i == list.Count - 1)
                        high = hiBound;
                }
                else
                {
                    list[i + 1] = seqno - 1;

                    list.Insert(i + 2, hiBound);
                    list.Insert(i + 2, seqno + 1);
                }
            }
        }

        /// <summary> Retransmission task:<br>
        /// For each interval, call the retransmission callback command
        /// </summary>
        public override void Run()
        {
            ArrayList cloned;
            lock (this)
            {
                cloned = (ArrayList)list.Clone();
            }
            for (int i = 0; i < cloned.Count; i += 2)
            {
                long loBound = (long)cloned[i];
                long hiBound = (long)cloned[i + 1];
                enclosingInstance.cmd.retransmit(loBound, hiBound, enclosingInstance.sender);
            }
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (low == high)
                sb.Append(low);
            else
                sb.Append(low).Append(':').Append(high);
            return sb.ToString();
        }
    } // end class Entry

}

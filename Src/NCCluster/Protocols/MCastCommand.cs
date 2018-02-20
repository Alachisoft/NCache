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
// $Id: TOTAL.java,v 1.6 2004/07/05 14:17:16 belaban Exp $
using Alachisoft.NGroups.Stack;
using Alachisoft.NCache.Common.Net;
using System.Collections;

namespace Alachisoft.NGroups.Protocols
{
    /// <summary> The retransmission listener - It is called by the
    /// <code>AckSenderWindow</code> when a retransmission should occur
    /// </summary>
    internal class MCastCommand : RetransmitCommand
    {
        private void InitBlock(TOTAL enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        private TOTAL enclosingInstance;
        public TOTAL Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        public MCastCommand(TOTAL enclosingInstance)
        {
            InitBlock(enclosingInstance);
        }
        public virtual void retransmit(long seqNo, Message msg)
        {
            ArrayList mbrs = msg.Dests.Clone() as ArrayList;

            string subGroupID = Enclosing_Instance._mbrsSubgroupMap[mbrs[0]] as string;
            ArrayList groupMbrs = (ArrayList)Enclosing_Instance._sequencerTbl[subGroupID] as ArrayList;
            Address groupSequencerAddr = groupMbrs[0] as Address;
            if (groupSequencerAddr != null)
                Enclosing_Instance._retransmitMcastRequest(seqNo, groupSequencerAddr);
        }

        public void retransmit(long seqno, Message msg, Address dest)
        {
            // No Implementation
        }

        public void retransmit(long first_seqno, long last_seqno, Address sender)
        {
            // No Implementation
        }
        
    }

}

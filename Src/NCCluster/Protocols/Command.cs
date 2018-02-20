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

namespace Alachisoft.NGroups.Protocols
{
    /// <summary> The retransmission listener - It is called by the
    /// <code>AckSenderWindow</code> when a retransmission should occur
    /// </summary>
    internal class Command : RetransmitCommand
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
        public Command(TOTAL enclosingInstance)
        {
            InitBlock(enclosingInstance);
        }
        public virtual void retransmit(long seqNo, Message msg)
        {
            Enclosing_Instance._retransmitBcastRequest(seqNo);
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

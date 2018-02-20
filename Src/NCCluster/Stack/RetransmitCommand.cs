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
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NGroups.Stack
{
    /// <summary> Called by retransmitter thread whenever a message needs to be re-sent
    /// to a destination. <code>dest</code> has to be set in the
    /// <code>dst</code> field of <code>msg</code>, as the latter was sent
    /// multicast, but now we are sending a unicast message. Message has to be
    /// copied before sending it (as headers will be appended and therefore
    /// the message changed!).
    /// </summary>
    interface RetransmitCommand
    {
        /// <summary> Retranmit the given msg
        /// 
        /// </summary>
        /// <param name="seqno">the sequence number associated with the message
        /// </param>
        /// <param name="msg">the msg to retransmit (it should be a copy!)
        /// </param>
        /// <param name="dest">the msg destination
        /// </param>
        void retransmit(long seqno, Message msg, Address dest);

        /// <summary> Get the missing messages between sequence numbers
        /// <code>first_seqno</code> and <code>last_seqno</code>. This can either be done by sending a
        /// retransmit message to destination <code>sender</code> (nak-based scheme), or by
        /// retransmitting the missing message(s) to <code>sender</code> (ack-based scheme).
        /// </summary>
        /// <param name="first_seqno">The sequence number of the first missing message
        /// </param>
        /// <param name="last_seqno"> The sequence number of the last missing message
        /// </param>
        /// <param name="sender">The destination of the member to which the retransmit request will be sent
        /// (nak-based scheme), or to which the message will be retransmitted (ack-based scheme).
        /// </param>
        void retransmit(long first_seqno, long last_seqno, Address sender);

        void retransmit(long seqno, Message msg);
    }
}

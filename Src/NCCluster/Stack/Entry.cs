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
namespace Alachisoft.NGroups.Stack
{
    /// <summary> The entry associated with a pending msg</summary>
    internal class Entry : Task
    {
        private void InitBlock(AckMcastSenderWindow enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        private AckMcastSenderWindow enclosingInstance;
        public AckMcastSenderWindow Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        /// <summary>The msg sequence number </summary>
        public long seqno;
        /// <summary>The msg to retransmit </summary>
        public Message msg = null;
        /// <summary>destination addr -> boolean (true = received, false = not) </summary>
        public System.Collections.Hashtable senders = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
        /// <summary>How many destinations have received the msg </summary>
        public int num_received = 0;

        public Entry(AckMcastSenderWindow enclosingInstance, long seqno, Message msg, System.Collections.ArrayList dests, long[] intervals) : base(intervals)
        {
            InitBlock(enclosingInstance);
            this.seqno = seqno;
            this.msg = msg;
            for (int i = 0; i < dests.Count; i++)
                senders[dests[i]] = false;
        }

        internal virtual bool allReceived()
        {
            return (num_received >= senders.Count);
        }

        /// <summary>Retransmit this entry </summary>
        public override void Run()
        {
            Enclosing_Instance._retransmit(this);
        }

        public override string ToString()
        {
            System.Text.StringBuilder buff = new System.Text.StringBuilder();
            buff.Append("num_received = " + num_received + ", received msgs = " + Global.CollectionToString(senders));
            return (buff.ToString());
        }
    }
}

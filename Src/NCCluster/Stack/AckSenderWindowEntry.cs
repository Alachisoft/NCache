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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NGroups.Stack
{
    /// <summary>Struct used to store message alongside with its seqno in the message queue </summary>
    internal class AckSenderWindowEntry
    {
        private void InitBlock(AckSenderWindow enclosingInstance)
        {
            this.enclosingInstance = enclosingInstance;
        }
        private AckSenderWindow enclosingInstance;
        public AckSenderWindow Enclosing_Instance
        {
            get
            {
                return enclosingInstance;
            }

        }
        internal long seqno;
        internal Message msg;

        internal AckSenderWindowEntry(AckSenderWindow enclosingInstance, long seqno, Message msg)
        {
            InitBlock(enclosingInstance);
            this.seqno = seqno;
            this.msg = msg;
        }
    }
}

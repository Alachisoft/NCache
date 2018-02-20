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
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NGroups.Protocols
{
    /// <summary>
    /// Asynchronously sends the multicast message.
    /// </summary>
    internal class TCPAsyncMulticast : AsyncProcessor.IAsyncTask
    {
        private TCP _parent;
        private Message _message;
        private bool _reEstablishConnection;

        public TCPAsyncMulticast(TCP parent, Message m, bool reEstablish)
        {
            _parent = parent;
            _message = m;
            _reEstablishConnection = reEstablish;
        }
        #region IAsyncTask Members

        /// <summary>
        /// broadcasts the message.
        /// </summary>
        public void Process()
        {
            if (_parent != null)
            {
                if (_parent.Stack.NCacheLog.IsInfoEnabled) _parent.Stack.NCacheLog.Info("TCP.AsnycMulticast.Process", "broadcasting message");
                _parent.sendMulticastMessage(_message, _reEstablishConnection, Priority.High);
            }
        }

        #endregion
    }
}

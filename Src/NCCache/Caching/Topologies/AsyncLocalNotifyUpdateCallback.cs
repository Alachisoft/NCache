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

using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    ///Asynchronous notification dispatcher. 
    /// </summary>
    internal class AsyncLocalNotifyUpdateCallback : AsyncProcessor.IAsyncTask
    {
        /// <summary> The listener class </summary>
        private ICacheEventsListener _listener;

        /// <summary> Message to broadcast </summary>
        private object _key;

        private object _entry;
        private OperationContext _operationContext;
        private EventContext _eventContext;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="data"></param>
        public AsyncLocalNotifyUpdateCallback(ICacheEventsListener listener, object key, object entry, OperationContext operationContext, EventContext eventContext)
        {
            _listener = listener;
            _key = key;
            _entry = entry;
            _operationContext = operationContext;
            _eventContext = eventContext;
        }

        /// <summary>
        /// Implementation of message sending.
        /// </summary>

        void AsyncProcessor.IAsyncTask.Process()
        {
            _listener.OnCustomUpdateCallback(_key, _entry, _operationContext, _eventContext);
        }

    }
}

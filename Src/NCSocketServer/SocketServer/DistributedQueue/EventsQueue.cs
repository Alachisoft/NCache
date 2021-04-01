//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer
{
    public class EventsQueue : IQueue
    {
        private ClusteredQueue<Object> _queue = new ClusteredQueue<Object>();

        public EventsQueue()
        {
            
        }

        public void Enqueue(object item)
        {
            lock (this)
            {
                _queue.Enqueue(item);
            }
        }

        public object Dequeue()
        {
            object eventItem;
            QueuedItem item;
            
            Alachisoft.NCache.Common.Protobuf.Response response = null;
            Alachisoft.NCache.Common.Protobuf.BulkEventResponse bulkEvent = new Common.Protobuf.BulkEventResponse();

            lock (this)
            {
                item = new QueuedItem();
                for (int i = 1; i <= ServiceConfiguration.EventBulkCount && _queue.Count>0 ; i++)
                {
                    eventItem = null;
                    {
                        eventItem = _queue.Dequeue();
                        item.Count = i;                         
                    }

                    if (eventItem == null)
                        break;

                    bulkEvent.eventList.Add((Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse)eventItem);
                }

                if (bulkEvent.eventList.Count > 0)
                {
                    response = new Common.Protobuf.Response();
                    response.bulkEventResponse = bulkEvent;
                    response.responseType = Common.Protobuf.Response.Type.BULK_EVENT;
                }

                item.Item = (object)response;
                return item;
            }
        }

        public int Count
        {
            get { return _queue.Count; }
        }
    }
}

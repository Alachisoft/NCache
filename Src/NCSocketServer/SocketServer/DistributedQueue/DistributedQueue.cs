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
using System.Text;
using System.Collections;
using System.Threading;
using Alachisoft.NCache.SocketServer.Statistics;

namespace Alachisoft.NCache.SocketServer
{
    class DistributedQueue :IDistributedQueue
    {
        SortedDictionary<string, IQueue> _reqisteredQueues = new SortedDictionary<string, IQueue>();
        List<SlaveQueue> _qeueues = new List<SlaveQueue>();
        private StatisticsCounter _perfStatsCollector;


        long _count;
        int _currentQueueIndex =0;
        bool _closed;
       
        public DistributedQueue(StatisticsCounter statsCollector)
        {
            _perfStatsCollector = statsCollector;
        }

        public object Dequeue()
        {
            QueuedItem item = null;

            do
            {
                lock (this)
                {
                    if (_qeueues.Count != 0 && _count >0)
                    {
                        if (_currentQueueIndex >= _qeueues.Count)
                            _currentQueueIndex = 0;

                        int startIndex = _currentQueueIndex;

                        for (; _currentQueueIndex < _qeueues.Count; _currentQueueIndex++)
                        {
                            item = DequeueInternal(_currentQueueIndex);
                            if (item != null) break;
                        }

                        if (item == null)
                        {
                            for (int i = 0; i < startIndex; i++)
                            {
                                item = DequeueInternal(i);
                                if (item != null)
                                {
                                    _currentQueueIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (item == null)
                        Monitor.Wait(this);

                    _currentQueueIndex++;
                }
            } while (item == null && !_closed);           

            return item;
        }


        private QueuedItem DequeueInternal(int queueIndex)
        {
            QueuedItem item = null;
            QueuedItem queuedItem = new Alachisoft.NCache.SocketServer.QueuedItem();

            try
            {
                IQueue innerQueue = _qeueues[queueIndex].Queue;
                queuedItem.RegisteredClientId = _qeueues[queueIndex].RegisteredClientId;

                if (innerQueue.Count > 0)
                {
                    item = (QueuedItem)innerQueue.Dequeue(); // inner queue also return an queued item object
                    queuedItem.Item = item.Item;
                    _count -= item.Count;
                }
            }
            catch (Exception)
            { }

            return queuedItem;
        }



        public string RegisterSlaveQueue(IQueue queue, string clientId)
        {
            string queueId = Guid.NewGuid().ToString();
            SlaveQueue slaveQ = new SlaveQueue();

            lock (this)
            {
                if (!_reqisteredQueues.ContainsKey(queueId))
                {
                    _count += queue.Count;
                    _reqisteredQueues.Add(queueId, queue); 
                    
                    slaveQ.Queue = queue;
                    slaveQ.SlaveId = queueId;
                    slaveQ.RegisteredClientId = clientId;

                    _qeueues.Add(slaveQ);
                }
            }
            return queueId;
        }

        public void UnRegisterSlaveQueue(string queueId)
        {
            lock (this)
            {

                if (queueId == null) return;

                if (_reqisteredQueues.ContainsKey(queueId))
                {
                    IQueue queue = _reqisteredQueues[queueId] as IQueue;
                    _count -= queue.Count;
                    _reqisteredQueues.Remove(queueId);
                    RemoveSlaveQueue(queueId);
                }
            }
        }


        internal void RemoveSlaveQueue(string queueId)
        {
            for (int i = 0; i < _qeueues.Count; i++)
            {
                if (queueId.Equals(_qeueues[i].SlaveId))
                {
                    _qeueues.Remove(_qeueues[i]);
                }
            }
        }


        public void Enqueue(object item, string queueId)
        {
            if (queueId == null) return;

            lock (this)
            {
                bool enqueued = false;
                if (_reqisteredQueues.ContainsKey(queueId))
                {
                    IQueue queue = _reqisteredQueues[queueId] as IQueue;
                    if (queue != null)
                    {
                        enqueued = true;
                        queue.Enqueue(item);
                        _count++;
                    }
                }
                if(enqueued) Monitor.PulseAll(this);
            }
        }

        public long Count
        {
            get { lock (this) { return _count; } }
        }

        public void Close()
        {
            lock (this)
            {
                _closed = true;
                Monitor.PulseAll(this);
            }
        }
    }
}

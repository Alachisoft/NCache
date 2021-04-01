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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using System.Threading;

using System.Collections;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Caching.Messaging;
using Alachisoft.NCache.Common.Pooling;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif
namespace Alachisoft.NCache.Caching
{
    class EventManager
    {
        private Thread _pollingThread;
        private bool _poll;
        private DateTime _lastPoll = DateTime.Now;
        private const int _pollingInterval = 10;  // polling interval is in seconds; i.e. 15 seconds
        private readonly object _lockObj = new object();
        private bool _isNotificationRegister = false;
        private long _version;
        MessageResponse result;
        private SubscriptionInfo subInfo;
        private OperationContext operationContext;
        private CacheRuntimeContext context;
        IDictionary<string, IList<string>> topicWiseMessageIds = new Dictionary<string, IList<string>>();
        IList<string> genenralEventsMessageIds = new List<string>();
        IList<string> itemLevelEventsMessageIds = new List<string>();
        IList<string> CQEventsMessageIds = new List<string>();

        public void StartPolling(CacheRuntimeContext context, OperationContext operationContext)
        {
            this.context = context;
            this.operationContext = operationContext;
            lock (this)
            {
                if (!_isNotificationRegister)
                {
                    StartPollForMessage();
                    _isNotificationRegister = true;
                }
            }
        }

        private void StartPollForMessage()
        {
            _poll = true;
            _pollingThread = new Thread(PollForEvents);
            _pollingThread.IsBackground = true;
            _pollingThread.Name = "EventPollingThread";
            _pollingThread.Start();
        }

        private void PollForEvents()
        {
            try
            {
                long currentVersion = -1;
                while (_poll)
                {
                    try
                    {
                        lock (_lockObj)
                        {
                            //wait only if there is no change event fired from server side causing version update
                            if (currentVersion == _version)
                                Monitor.Wait(_lockObj, _pollingInterval * 1000);
                        }
                        TimeSpan diff = DateTime.Now.Subtract(_lastPoll);
                        currentVersion = _version;
                        Poll();

                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        private void Poll()
        {
            ArrayList nodes = new ArrayList();
            ArrayList oldclients = new ArrayList();
            ArrayList localclients = new ArrayList();
            //Get assigned messages.
#if SERVER
            //cluster
            if (context.CacheImpl is ClusterCacheBase) 
            {
                nodes = ((ClusterCacheBase)context.CacheImpl)._stats.Nodes;
                foreach (NodeInfo nodeInfo in nodes)
                {
                    oldclients = nodeInfo.OldConnectedClientsInfo;
                    foreach (string client in oldclients)
                        {
                                PollForEventMessage(client);
                        }
                }
            }
            else
#endif
            {
                nodes = context.CacheImpl.stats.ConnectedClients;
                localclients = context.CacheImpl.ClientsInfoList;
                foreach (string clientId in nodes)
                {
                    foreach (string client in localclients)
                    {
                          PollForEventMessage(client);
                    }
                }
            }

        }

        private void PollForEventMessage(string clientId)
        {
            
                SubscriptionInfo subscriptionInfo = new SubscriptionInfo() { SubscriptionId = SubscriptionInfo.EventsSubscriptionName, ClientId = clientId, Type = SubscriptionType.Subscriber, SubPolicyType = SubscriptionPolicyType.EventSubscription, Expiration = TimeSpan.MaxValue.Ticks };
                result = context.CacheImpl.GetAssignedMessage(subscriptionInfo, operationContext);

                foreach (var pair in result.AssignedMessages)
                {
                    IList<object> eventMessages = pair.Value;
                    lock (eventMessages)
                    {
                        IEnumerator<object> enu = eventMessages.GetEnumerator();
                        while (enu.MoveNext())
                        {
                            //Create Events here and fire
                            var message = (EventMessage)enu.Current;
                            EventContext eventContext = SetEventContext(message);
                            switch (eventContext.EventID.EventType)
                            {
                                case Persistence.EventType.ITEM_UPDATED_CALLBACK:
                                    context.CacheImpl.RaiseOldCustomUpdateCalbackNotifier(message.Key, message.CallbackInfos, eventContext);
                                    break;
                                case Persistence.EventType.ITEM_REMOVED_CALLBACK:
                                    CacheEntry entry = CacheEntry.CreateCacheEntry(context.FakeObjectPool);
                                    entry.Notifications = new Notifications();
                                    entry.Notifications.ItemRemoveCallbackListener = new ArrayList();
                                    entry.Notifications.ItemRemoveCallbackListener = message.CallbackInfos;
                                    entry.MarkInUse(NCModulesConstants.Global);
                                    context.CacheImpl.RaiseOldCustomRemoveCalbackNotifier(message.Key, entry, message.RemoveReason, operationContext, eventContext);
                                    break;
                                
                            }

                            switch (message.MessageMetaData.TopicName)
                            {
                                case TopicConstant.ItemLevelEventsTopic:
                                    itemLevelEventsMessageIds.Add(message.MessageId);
                                    break;
                                
                            }

                        }
                    }
                }

          

            if (itemLevelEventsMessageIds.Count > 0)
            {
                topicWiseMessageIds.Add(TopicConstant.ItemLevelEventsTopic, itemLevelEventsMessageIds);
                itemLevelEventsMessageIds = new List<string>();
            }

           

            if (topicWiseMessageIds.Count > 0)
            {
                context.CacheImpl.AcknowledgeMessageReceipt(clientId, topicWiseMessageIds, operationContext);
                topicWiseMessageIds = new Dictionary<string, IList<string>>();
            }
        }

        private EventContext SetEventContext(EventMessage message)
        {
            EventContext eventContext = new EventContext();
            /*eventContext.EventID = message.EventID;       */    
            eventContext.Add(EventContextFieldName.EventID, message.EventID);                       
            eventContext.Item = message.Item;
            eventContext.OldItem = message.OldItem;
            eventContext.TaskFailureReason = message.TaskFailureReason;
            
            return eventContext; 
        }

        public void StopPolling()
        {
            try
            {
                lock (this)
                {
                    _poll = false;
                    _isNotificationRegister = false;
                    if (_pollingThread != null)
#if !NETCORE
                        _pollingThread.Abort();
#elif NETCORE
                        _pollingThread.Interrupt();
#endif
                    _pollingThread = null;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}

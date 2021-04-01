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
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Alachisoft.NCache.Caching.Messaging
{
    class MessageManager :ITopicEventListener
    {
        private IMessageStore _store;
        private CacheRuntimeContext _context;
        private Thread _assignmentThread;
        private Thread _notificationThread;
        private TimeSpan _assignmentTimeout = TimeSpan.FromSeconds(20);
        private TimeSpan _notificationInterval = TimeSpan.FromMilliseconds(500);
        private long _version;
        private object _mutex = new object();
        private ICacheEventsListener _cacheEventListener;
        private AutoExpirationTask _taskExpiry;
        private long _cleanInterval = 15000;
        private bool _hadPendingWork;
        private DateTime _expirationTimestamp = DateTime.Now;

        public MessageManager(CacheRuntimeContext context)
        {
           
            _context = context;

        }

        public void StartMessageProcessing()
        {
            _store = _context.CacheImpl;
            _cacheEventListener = _context.CacheImpl as ICacheEventsListener;

            _store.RegiserTopicEventListener(this);

            if(_assignmentThread == null)
            {
                _assignmentThread = new Thread(new ThreadStart(ProcessMessage));
                _assignmentThread.IsBackground = true;
                _assignmentThread.Name = "Messaging.MessageProcessor";
                _assignmentThread.Start();
            }

            if(_notificationThread == null)
            {
                _notificationThread = new Thread(new ThreadStart(NotifyClients));
                _notificationThread.IsBackground = true;
                _notificationThread.Name = "Messaging.Notification";
                _notificationThread.Start();
            }

            if (_taskExpiry == null)
            {
                _taskExpiry = new AutoExpirationTask(this, _cleanInterval);
                _context.TimeSched.AddTask(_taskExpiry);
            }
        }

        public void StopMessageProcessing()
        {
            if(_assignmentThread != null && _assignmentThread.IsAlive)
            {
#if !NETCORE
            _assignmentThread.Abort();
#elif NETCORE
                _assignmentThread.Interrupt();
#endif
            }

            if (_notificationThread != null && _notificationThread.IsAlive)
            {
#if !NETCORE
            _notificationThread.Abort();
#elif NETCORE
                _notificationThread.Interrupt();
#endif
            }

            if (_taskExpiry != null)
            {
                _taskExpiry.Cancel();
            }

        }

       
        #region /                           --- Message Assignments and removal ----                                     /

        private void ProcessMessage()
        {
            try
            {
                while (true)
                {
                    long currentVersion = 0;
                    _hadPendingWork = false;
                    try
                    {
                        lock (_mutex)  currentVersion = _version;

                        //Revoke expired assignments
                        RevokeExpiredAssignments();
                        //Remove inactiveClients
                        RemoveInactiveClients();
                        //Assignment of new messages
                        AssignPendingMessages();
                        //Remove delivered messages
                        RemoveDeliveredMessages();
                      
                       
                    }
                    catch (ThreadAbortException) { break; }
                    catch (ThreadInterruptedException) { break; }
                    catch (Exception e)
                    {
                        _context.NCacheLog.Error("MessageProcessor", e.ToString());
                    }
                    finally
                    {
                        //For for new udpates to come
                        WaitForUpdates(currentVersion);
                    }
                }
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("MessageProcessor", e.ToString());
            }
        }

        private void RemoveInactiveClients()
        {
            
            var inactiveClients = _store.GetInActiveClientSubscriptions(ServiceConfiguration.PubSubClientInactivityTimeout);

            int runCount = 0;
            if(inactiveClients != null)
            {
                OperationContext context = null;

                try
                {
                    context = OperationContext.CreateAndMarkInUse(_context.TransactionalPoolManager, NCModulesConstants.PubSub, OperationContextFieldName.InternalOperation, true);

                    foreach (KeyValuePair<string, IList<string>> pair in inactiveClients)
                    {
                        foreach (string inactiveClient in pair.Value)
                        {
                            if (runCount++ > 200)
                            {
                                _hadPendingWork = true;
                                break; //to prevent starvation and let other operations happen
                            }
                            try
                            {
                                if (_context.NCacheLog.IsInfoEnabled)
                                    _context.NCacheLog.Info("MessageProcessor.RemoveInactiveClients", inactiveClient + " subscription removed due to inactivity against topic " + pair.Key);

                                _store.TopicOperation(new SubscriptionOperation(pair.Key, TopicOperationType.RemoveInavtiveSubscripber, new SubscriptionInfo() { ClientId = inactiveClient }), context);
                            }
                            catch (Exception e)
                            {
                                _context.NCacheLog.Error("MessageProcessor.RemoveInactiveClients", e.ToString());
                            }
                        }
                    }
                }
                finally
                {
                    MiscUtil.ReturnOperationContextToPool(context, _context.TransactionalPoolManager);
                    context?.MarkFree(NCModulesConstants.PubSub);
                }
            }
        }

       

        private void RemoveDeliveredMessages()
        {
            OperationContext context = new OperationContext();

            IList<MessageInfo> deliveredMessages = _store.GetDeliveredMessages();

            _store.RemoveMessages(deliveredMessages, MessageRemovedReason.Delivered, new OperationContext());
        }

       
        private void RevokeExpiredAssignments()
        {
            OperationContext context = new OperationContext();

            if (DateTime.Now < _expirationTimestamp.AddSeconds(10))
                return;

            IList<MessageInfo> unacknowledgedMessages = _store.GetUnacknowledgeMessages(new TimeSpan(0,0,20));
            int runCount = 0;
            foreach (MessageInfo message in unacknowledgedMessages)
            {
                _store.AssignmentOperation(message, null, TopicOperationType.RevokeAssignment, new OperationContext());
                if (runCount++ > 200)
                {
                    _hadPendingWork = true;
                    break; //to prevent starvation and let other operations happen
                }
            }

            _expirationTimestamp = DateTime.Now;
        }

        private void AssignPendingMessages()
        {
            OperationContext context = new OperationContext();

            int runCount = 0;
            while (true)
            {
                if (runCount++ > 200)
                {
                    _hadPendingWork = true;
                    break; //to prevent starvation and let other operations happen
                }
                var message = _store.GetNextUnassignedMessage(TimeSpan.MaxValue, context);
                if (message != null)
                {
                    SubscriptionInfo subscription = new SubscriptionInfo() { Type = Common.Enum.SubscriptionType.Subscriber };
                    if (message.Topic == "$GeneralEvents$" || message.Topic == "$ItemLevelEvents$" || message.Topic == "$ContinuousQueryEvents$" || message.Topic == "$CollectionEvents$")
                    {
                        subscription.SubPolicyType = Runtime.Caching.SubscriptionPolicyType.EventSubscription;
                        message.DeliveryOption = Runtime.Caching.DeliveryOption.All;
                    }

                    if (message.DeliveryOption == Runtime.Caching.DeliveryOption.Any)
                    {
                        subscription = _store.GetSubscriber(message.Topic, Common.Enum.SubscriptionType.Subscriber, context);
                            
                        if (subscription == null)
                        {
                            continue;
                        }
                    }
                    _store.AssignmentOperation(message, subscription, TopicOperationType.AssignSubscription, new OperationContext());
                }
                else
                    break;

            }
        }

    
        private void UpdateVersion()
        {
           lock(_mutex)
            {
                _version++;
                Monitor.PulseAll(_mutex);
            }
        }

        public void WaitForUpdates(long currentVersion)
        {
            lock(_mutex)
            {
                //see if version is already udpated
                if (currentVersion < _version || _hadPendingWork) return;

                //wait for version udpate
                Monitor.Wait(_mutex, TimeSpan.FromSeconds(5));
            }
        }

        #endregion

        #region /                           --- Client notificaitons ----                                     /
        private void NotifyClients()
        {
            try
            {
                while(true)
                {
                    try
                    {
                        Thread.Sleep(_notificationInterval);

                        IList<string> clients = _store.GetNotifiableClients();

                        if (clients != null && clients.Count > 0)
                        {
                            foreach (string client in clients)
                            {
                                _cacheEventListener.OnPollNotify(client, 11, Caching.Events.EventTypeInternal.PubSub);
                            }
                        }
                    }
                    catch (ThreadInterruptedException) { _context.NCacheLog.Error("MessageManager.NotifyClients", "stopping notiication thead");  break; }
                    catch (ThreadAbortException) { _context.NCacheLog.Error("MessageManager.NotifyClients", "stopping notiication thead"); break; }
                    catch (Exception ex)
                    {
                        _context.NCacheLog.Error("MessageManager.NotifyClients", ex.ToString());
                    }
                }
            }
            catch (ThreadInterruptedException) { _context.NCacheLog.Error("MessageManager.NotifyClients", "stopping notiication thead"); }
            catch (ThreadAbortException) { _context.NCacheLog.Error("MessageManager.NotifyClients", "stopping notiication thead"); }
            catch (Exception e)
            {
                _context.NCacheLog.Error("MessageManager.NotifyClients", "stopping notiication thead");
                _context.NCacheLog.Error("MessageManager.NotifyClients", e.ToString());
            }
        }

        internal void Evict(long sizeToEvict)
        {
            IList<MessageInfo> messages = _store.GetEvicatableMessages(sizeToEvict);
            _store.RemoveMessages(messages, MessageRemovedReason.Evictied, new OperationContext());

            if (_context.PerfStatsColl != null) _context.PerfStatsColl.IncrementEvictPerSecStatsBy(messages.Count);
        }

        #endregion

        #region /                           --- ITopicListener ----                                                  /

        public void OnSubscriptionCreated(Topic topic)
        {
            UpdateVersion();
            
        }

        public void OnSubscriptionRemoved(Topic topic)
        {
            UpdateVersion();
        }

        public void OnMessageArrived(Topic topic)
        {
            UpdateVersion();
        }

        public void OnMessageDelivered(Topic topic)
        {
            UpdateVersion();
        }

        public void OnSizeIncrement(long sizeChange)
        {

        }

        public void OnCountIncrement(long count)
        {

        }

        public void OnSizeDecrement(long sizeChange)
        {

        }

        public void OnCountDecrement(long count)
        {

        }

        #endregion

        #region /                           --- Expiration ----                                                  /

        /// <summary>
        /// The Task that takes care of auto-expiration of items.
        /// </summary>
        class AutoExpirationTask : TimeScheduler.Task
        {
            /// <summary> Reference to the parent. </summary>
            private MessageManager _parent = null;

            /// <summary> Periodic interval </summary>
            private long _interval = 1000;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="interval"></param>
            internal AutoExpirationTask(MessageManager parent, long interval)
            {
                _parent = parent;
                _interval = interval;
            }

            public long Interval
            {
                get { lock (this) { return _interval; } }
                set { lock (this) { _interval = value; } }
            }

            /// <summary>
            /// Sets the cancel flag.
            /// </summary>
            public void Cancel()
            {
                lock (this) { _parent = null; }
            }

            /// <summary>
            /// True if task is cancelled, false otherwise
            /// </summary>
            public bool IsCancelled
            {
                get { return this._parent == null; }
            }

            /// <summary>
            /// returns true if the task has completed.
            /// </summary>
            /// <returns>bool</returns>
            bool TimeScheduler.Task.IsCancelled()
            {
                lock (this) { return _parent == null; }
            }

            /// <summary>
            /// tells the scheduler about next interval.
            /// </summary>
            /// <returns></returns>
            long TimeScheduler.Task.GetNextInterval()
            {
                lock (this) { return _interval; }
            }

            /// <summary>
            /// This is the main method that runs as a thread. CacheManager does all sorts of house 
            /// keeping tasks in that method.
            /// </summary>
            void TimeScheduler.Task.Run()
            {
                if (_parent == null) return;
                ExpireMesages();
            }

            private void ExpireMesages()
            {
                try
                {
                    _parent.Expire();
                }
                catch(Exception ex)
                {

                }

            }

         
        }

       

        private bool Expire()
        {
            try
            {
                var expiredMessages = _store.GetExpiredMessages();

                if (expiredMessages != null && expiredMessages.Count > 0)
                {
                    if (_context.PerfStatsColl != null)
                        _context.PerfStatsColl.IncrementMessageExpiredPerSec(expiredMessages.Count);

                    _store.RemoveMessages(expiredMessages, MessageRemovedReason.Expired, new OperationContext());
                }
                
            }
            catch (Exception e)
            {
                _context.NCacheLog.Error("MessageManger.Expire", e.ToString());
            }

            return true;
        }

        internal void SetExpirationInterval(long cleanInterval)
        {
            if (cleanInterval > 0)
            {
                _cleanInterval = cleanInterval;
                if (_taskExpiry != null)
                    _taskExpiry.Interval = _cleanInterval;
            }
        }
        
        #endregion
    }


}

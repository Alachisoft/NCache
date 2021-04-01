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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.Messaging
{
    class SubscriptionRefresherTask : TimeScheduler.Task
    {
        private bool _cancelled;
        private CacheRuntimeContext _context;
        private long _interval = 10 * 1000; //msec
        private IMessageStore _store;

        public SubscriptionRefresherTask(IMessageStore store, CacheRuntimeContext context)
        {
            _store = store;
            _context = context;
        }

        public long GetNextInterval()
        {
            return _interval;
        }

        public bool IsCancelled()
        {
            return _cancelled;
        }

        public void Run()
        {
            OperationContext context = null;

            try
            {
                var activeClients = _store.GetActiveClientSubscriptions(TimeSpan.FromMilliseconds(_interval));

                if(activeClients != null)
                {
                    context = OperationContext.CreateAndMarkInUse(_context.TransactionalPoolManager, NCModulesConstants.PubSub, OperationContextFieldName.InternalOperation, true);

                    foreach(KeyValuePair<string,IList<string>> pair in activeClients)
                    {
                        foreach (string client in pair.Value)
                        {
                            if(_context.NCacheLog.IsInfoEnabled)  _context.NCacheLog.Info("SubscriptionRefresher", client + " being refreshed for topic " + pair.Key);
                            _store.TopicOperation(new SubscriptionOperation(pair.Key, Common.Enum.TopicOperationType.RefreshSubscription,
                                                  new SubscriptionInfo() { ClientId = client }), context);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                _context.NCacheLog.Error("SubscriptionReresherTask", e.ToString());
            }
            finally
            {
                MiscUtil.ReturnOperationContextToPool(context, _context.TransactionalPoolManager);
                context?.MarkFree(NCModulesConstants.PubSub);
            }
        }

        internal void Cancle()
        {
            _cancelled = true;
        }
    }
}

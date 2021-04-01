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
using Alachisoft.NCache.Runtime.Caching;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Client
{
    /// <summary>
    /// 
    /// </summary>
    internal class TopicSubscription : ITopicSubscription
    {
        private readonly Topic _topic;
        private string _subscriptionName;
        private SubscriptionPolicyType _subscriptionPolicyType;
        private event MessageReceivedCallback _evtMessageReceived;
        private int count=1;
        private long _expiration;
        private long  _creationTime;
        private SubscriptionPolicy _subscriptionPolicy;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="messageReceivedCallback"></param>
        public TopicSubscription(Topic topic,string subscriptionName,SubscriptionPolicyType subscriptionPolicy, MessageReceivedCallback messageReceivedCallback)
        {
            _topic = topic;
            _subscriptionName = subscriptionName;
            _subscriptionPolicyType = subscriptionPolicy;
            _evtMessageReceived += messageReceivedCallback;
        }

        event MessageReceivedCallback ITopicSubscription.OnMessageRecieved
        {
            add
            {
                _evtMessageReceived += value;
            }

            remove
            {
                _evtMessageReceived -= value;
            }
        }

        public ITopic Topic
        {
            get { return _topic; }
        }



        public string SubscriptionName
        {
            get
            {
                return _subscriptionName;
            }
        }

        internal SubscriptionPolicyType GetSubscriptionPolicyType
        {
            get
            { return _subscriptionPolicyType; }
        }

        internal long Expiration
        {
            get { return _expiration; }
            set { _expiration = value; }
        }

        internal long CreationTime
        {
            get { return _creationTime; }
            set { _creationTime = value; }
        }

        public SubscriptionPolicy SubscriptionPolicy
        {
            get{ return _subscriptionPolicy;}
        }

        internal void SetSubscriptionPolicy(SubscriptionPolicyType subscriptionPolicyType)
        {
            _subscriptionPolicyType = subscriptionPolicyType;

        }

        public void UpdateCount()
        {
            count++;
        }

        public void UnSubscribe()
        {
            if (this.count == 1)
            {
                _topic.UnSubscribe(this, true);
            }
            else
            {
                this.count = count - 1;
            }
        }

        public void UnSubscribeEventTopic()
        {
            _topic.UnSubscribeEventTopic(this, true);
        }

        internal virtual void OnMessageRecieved(MessageEventArgs args)
        {
            try
            {
                if (_evtMessageReceived != null)
                {
                    Delegate[] list = _evtMessageReceived.GetInvocationList();
                    foreach (Delegate t in list)
                    {
                        MessageReceivedCallback cb = t as MessageReceivedCallback;
                        if (cb != null)
                        {
#if !NETCORE
                            cb.BeginInvoke(this, args, null, null);
#elif NETCORE
                            //TODO: ALACHISOFT (BeginInvoke is not supported in .Net Core thus using TaskFactory)
                            TaskFactory factory = new TaskFactory();
                            Task task = factory.StartNew(() => cb(this, args));
#endif
                        }
                    }
                }
            }
            catch (Exception ex)
            { }
        }

        internal void Dispose()
        {
            _topic.UnSubscribe(this,false,true);
            _evtMessageReceived = null;
        }

        public void Close()
        {
            throw new NotImplementedException();
        }
    }
}

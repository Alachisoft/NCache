// Copyright (c) 2018 Alachisoft
// 
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

using System;
using Alachisoft.NCache.Runtime.Caching;

#if NETCORE
using System.Threading.Tasks;
#endif

namespace Alachisoft.NCache.Web.Caching
{
    internal class TopicSubscription : ITopicSubscription
    {
        private readonly Topic _topic;
        private string _subscriberId;
        private event MessageReceivedCallback _evtMessageReceived;

        public TopicSubscription(Topic topic, MessageReceivedCallback messageReceivedCallback)
        {
            _topic = topic;
            _subscriberId = new Shorter().GuidString;
            _evtMessageReceived += messageReceivedCallback;
        }

        event MessageReceivedCallback ITopicSubscription.OnMessageRecieved
        {
            add { _evtMessageReceived += value; }

            remove { _evtMessageReceived -= value; }
        }

        public ITopic Topic
        {
            get { return _topic; }
        }

        internal string SubscriberId
        {
            get { return _subscriberId; }
        }

        public void UnSubscribe()
        {
            _topic.UnSubscribe(this, true);
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
                            TaskFactory factory = new TaskFactory();
                            Task task = factory.StartNew(() => cb(this, args));
#endif
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        internal void Dispose()
        {
            _topic.UnSubscribe(this);
            _evtMessageReceived = null;
        }
    }
}
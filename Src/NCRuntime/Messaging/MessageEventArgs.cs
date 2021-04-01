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

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// Arguments containing details of received message including topic, sender and type.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        private readonly IMessage _message;
        private readonly DeliveryOption _deliveryOption;
        private readonly ITopic _topic;
        private readonly string _topicName;
        
        /// <summary>
        /// Creates MessageEventArgs
        /// </summary>
        /// <param name="message">Message to be delivered</param>
        /// <param name="deliverOption">How message should be delivered</param>
        /// <param name="messageTopic">Topic to which message belongs</param>
        /// <param name="topicName"> Topic on which message is published.</param> 

        public MessageEventArgs(IMessage message, DeliveryOption deliverOption, ITopic messageTopic,string topicName)
        {
            _message = message;
            _deliveryOption = deliverOption;
            _topic = messageTopic;
            _topicName = topicName;
        }

        
        /// <summary>
        /// Message to be delivered
        /// </summary>
        public IMessage Message
        {
            get { return _message; }
        }

        /// <summary>
        /// How message should be delivered
        /// </summary>
        public DeliveryOption DeliveryOption
        {
            get { return _deliveryOption; }
        }

        /// <summary>
        /// Topic to which message belongs
        /// </summary>
        public ITopic Topic
        {
            get { return _topic; }
        }

        /// <summary>
        /// Topic on which message is published.
        /// </summary>
        public string TopicName
        {
            get { return _topicName; }
        }
    }
}

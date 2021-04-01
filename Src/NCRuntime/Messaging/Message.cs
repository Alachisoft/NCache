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
    /// A message contains the actual 
    /// data object which is sent by the publisher and delivered to the interested subscribers for the topic.
    /// </summary>
    public class Message : IMessage
    {
        private TimeSpan? _expirationTime;
        private object _payload;
        private string _messageId;
        private DateTime _creationTime;
        private static readonly TimeSpan noExpiration = TimeSpan.MaxValue;

        /// <summary>
        /// Initializes an instance of Message.
        /// </summary>
        /// <param name="payload">Payload of message.</param>
        /// <param name="timeSpan">Expiry time of message.</param>
        public Message(object payload, TimeSpan? timeSpan = null)
        {
            if (payload == null)
                throw new ArgumentNullException("payload");

            _creationTime = DateTime.Now;
            ExpirationTime = timeSpan;
            _payload = payload;
            _messageId = GuidString;
            
        }

        /// <summary>
        /// Auto generated ID for the message, as same messages can be stored on different topics.
        /// </summary>
        public string MessageId
        {
            get { return _messageId; }
            internal set { _messageId = value; }
        }

        /// <summary>
        /// ExpirationTime of TimeSpan type after which the message is expired from the topic. 
        /// This can also accept null value, which will ensure that the message is not expired from the topic. 
        /// In case of no expiration time specified, null is considered as default.
        /// </summary>
        public TimeSpan? ExpirationTime
        {
            get { return _expirationTime; }
            set { _expirationTime = value; }
        }

        /// <summary>
        /// 	The actual data object of interest for subscribers, for example, Order.
        /// </summary>
        public object Payload
        {
            get { return _payload; }
        }

        /// <summary>
        /// Creation time in DateTime for the message.
        /// </summary>
        public DateTime CreationTime
        {
            get { return _creationTime; }
            internal set { _creationTime = value; }
        }

        private string GuidString
        {
            get
            {
                string enc = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                enc = enc.Replace("/", "_");
                enc = enc.Replace("+", "-");
                return enc.Substring(0, 22);
            }
        }

        /// <summary>
        /// Specifies TimeSpan.MaxValue for the message, so that it is not expired.
        /// </summary>
        public static TimeSpan NoExpiration
        {
            get
            {
                return noExpiration;
            }
        }


    }
}

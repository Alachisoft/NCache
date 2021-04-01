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
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Client
{
    internal class MessageItem
    {
        public bool Deserialized { get; internal set; }
        internal string MessageId { get; set; }
        internal object Payload { get; set; }
        internal BitSet Flag { get; set; }
        internal DateTime CreationTime { get; set; }
        internal TimeSpan ExpirationTime { get; set; }
        internal DeliveryOption DeliveryOption { get; set; }
        internal HashSet<string> RecipientList { get; set; }
        internal SubscriptionType SubscriptionType { get; set; }
        internal MessgeFailureReason MessageFailureReason { get; set; }
        internal List<SubscriptionIdentifier> SubscriptionIdentifierList { get; set; }
    }
}

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
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;
using System.Collections;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.Caching.Messaging
{
    interface IMessageStore
    {

        #region /                       ------- Message related operations -------                         /

    

        bool StoreMessage(string topic, Message message, OperationContext context);

        MessageInfo GetNextUnassignedMessage(TimeSpan timeout, OperationContext context);


        SubscriptionInfo GetSubscriber(string topic, Common.Enum.SubscriptionType type, OperationContext context);

        IList<SubscriptionInfo> GetAllSubscriber(string topic, OperationContext context);

        bool AssignmentOperation(MessageInfo messageInfo, SubscriptionInfo subscriptionInfo,TopicOperationType type, OperationContext context);

        MessageResponse GetAssignedMessage(SubscriptionInfo subscriptionInfo, OperationContext operationContext);

        void AcknowledgeMessageReceipt(string clientId, IDictionary<string, IList<string>> topicWiseMessageIds, OperationContext operationContext);

        IList<MessageInfo> GetUnacknowledgeMessages(TimeSpan assginmentTimeout);


        void RevokeAssignment(MessageInfo message, SubscriptionInfo subscription, OperationContext context);

        IList<MessageInfo> GetDeliveredMessages();

        IList<MessageInfo> GetExpiredMessages();

        IList<MessageInfo> GetEvicatableMessages(long sizeToEvict);

        IList<string> GetNotifiableClients();

        void RemoveMessages(IList<MessageInfo> messagesTobeRemoved, MessageRemovedReason reason, OperationContext context);

        void RemoveExpiredSubscriptions(IDictionary<string,IList<SubscriptionIdentifier>> toRemoveSubscriptions, OperationContext context);
        

        #endregion

        #region /                       ------- Topic related operations -------                         /

        bool TopicOperation(TopicOperation operation, OperationContext operationContext);

        void RegiserTopicEventListener(ITopicEventListener listener);

        IDictionary<string, IList<string>> GetInActiveClientSubscriptions(TimeSpan inactivityThreshold);

        IDictionary<string, IList<string>> GetActiveClientSubscriptions(TimeSpan inactivityThreshold);

        #endregion

        #region /                       ------- State transfer related operations -------                         /

        TopicState GetTopicsState();

        void SetTopicsState(TopicState topicState);

        TransferrableMessage GetTransferrableMessage(string topic, string messageId);

        bool StoreTransferrableMessage(string topic, TransferrableMessage message);

        Dictionary<string, TopicStats> GetTopicsStats(bool defaultTopicStats= false);
      





        #endregion
    }
}

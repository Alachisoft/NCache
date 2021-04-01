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

namespace Alachisoft.NCache.Common.Protobuf
{
    public interface ResponseBase
    {
        long requestId { get; set; }
        int commandID { get; set; }
    }

    public partial class Response : ResponseBase { }
    public partial class InsertResponse : ResponseBase { }
    public partial class BulkInsertResponse : ResponseBase { }
    public partial class DeleteResponse : ResponseBase { }
    public partial class RemoveResponse : ResponseBase { }
    public partial class CountResponse : ResponseBase { }
    public partial class ClearResponse : ResponseBase { }
    public partial class ContainBulkResponse : ResponseBase { }
    public partial class AddResponse : ResponseBase { }
    public partial class BulkAddResponse : ResponseBase { }
    public partial class AddAttributeResponse : ResponseBase { }
    public partial class RemoveByTagResponse : ResponseBase { }
    public partial class GetTagResponse : ResponseBase { }
    public partial class GetCacheItemResponse : ResponseBase { }
    public partial class BulkGetCacheItemResponse : ResponseBase { }
    public partial class RemoveGroupResponse : ResponseBase { }
    public partial class GetGroupNextChunkResponse : ResponseBase { }
    public partial class BulkRemoveResponse : ResponseBase { }
    public partial class BulkDeleteResponse : ResponseBase { }
    public partial class RemoveTopicResponse : ResponseBase { }
    public partial class GetTopicResponse : ResponseBase { }
    public partial class GetMessageResponse : ResponseBase { }
    public partial class MessageCountResponse : ResponseBase { }
    public partial class MessagePublishResponse : ResponseBase { }
 
    public partial class DisposeResponse : ResponseBase { }
    public partial class DisposeReaderResponse : ResponseBase { }

    public partial class ExecuteReaderResponse : ResponseBase { }
    public partial class LockResponse : ResponseBase { }
    public partial class UnlockResponse : ResponseBase { }
    public partial class VerifyLockResponse : ResponseBase { }
    public partial class IsLockedResponse : ResponseBase { }
    public partial class RegisterBulkKeyNotifResponse : ResponseBase { }
    public partial class UnregisterKeyNotifResponse : ResponseBase { }
    public partial class RegisterKeyNotifResponse : ResponseBase { }
    public partial class UnregisterBulkKeyNotifResponse : ResponseBase { }
    public partial class UnSubscribeTopicResponse : ResponseBase { }
    public partial class SubscribeTopicResponse : ResponseBase { }

    public partial class GetReaderChunkResponse : ResponseBase { }
    public partial class MessageAcknowledgmentResponse : ResponseBase { }
    public partial class GetProductVersionResponse : ResponseBase { }
    public partial class RaiseCustomEventResponse : ResponseBase { }
    public partial class PingResponse : ResponseBase { }
    public partial class PollResponse : ResponseBase { }
    public partial class TouchResponse : ResponseBase { }
    public partial class RegisterPollNotifResponse : ResponseBase { }
    public partial class SyncEventsResponse : ResponseBase { }
    public partial class SearchResponse : ResponseBase { }
    public partial class GetSerializationFormatResponse : ResponseBase { }
    public partial class InquiryRequestResponse : ResponseBase { }
    public partial class GetResponse : ResponseBase { }
    public partial class GetNextChunkResponse : ResponseBase { }
    public partial class GetConnectedClientsResponse : ResponseBase { }
  
  
 

}

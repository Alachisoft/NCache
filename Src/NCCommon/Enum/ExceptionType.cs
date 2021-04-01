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
namespace Alachisoft.NCache.Common.Enum
{
    public enum ExceptionType
    {
        OPERATIONFAILED = 0,
        AGGREGATE,
        CONFIGURATION,
        GENERALFAILURE,
        SECURITY,
        NOTSUPPORTED,
        MAX_CLIENTS_REACHED,
        STREAM_ALREADY_LOCKED,
        STREAM_CLOSED,
        STREAM_EXC,
        STREAM_INVALID_LOCK,
        STREAM_NOT_FOUND,
        TYPE_INDEX_NOT_FOUND,
        ATTRIBUTE_INDEX_NOT_FOUND,
        STATE_TRANSFER_EXCEPTION,
        INVALID_READER_EXCEPTION,
        LICENSING_EXCEPION
    }
}
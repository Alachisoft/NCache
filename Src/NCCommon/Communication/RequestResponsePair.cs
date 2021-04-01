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

using Alachisoft.NCache.Common.Communication.Exceptions;

namespace Alachisoft.NCache.Common.Communication
{
    internal class RequestResponsePair
    {
        private object _request;
        private object _response;
        private ChannelException _channelException;
        private bool _requestSentOverChannel;

        public object Request
        {
            get { return _request; }
            set { _request = value; }
        }

        public object Response
        {
            get { return _response; }
            set { _response = value; }
        }

        public ChannelException ChannelException
        {
            get { return _channelException; }
            set { _channelException = value; }
        }

        public bool RequestSentOverChannel
        {
            get { return _requestSentOverChannel; }
            set { _requestSentOverChannel = value; }
        }
    }
}

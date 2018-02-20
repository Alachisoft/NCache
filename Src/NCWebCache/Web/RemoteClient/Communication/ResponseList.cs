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

using System.Collections.Generic;
using Alachisoft.NCache.Web.Command;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.Collections;

namespace Alachisoft.NCache.Web.Communication
{
    internal class ResponseList
    {
        const int FIRST_CHUNK = 1;

        private long _requestId = -1;
        private Dictionary<int, CommandResponse> _responses = new Dictionary<int, CommandResponse>();

        private object _mutex = new object();
        private CommandBase _command;

        public ResponseList(CommandBase command)
        {
            _command = command;
        }

        internal CommandBase Command
        {
            get { return _command; }
        }

        internal long RequestId
        {
            get { return _requestId; }
            set { _requestId = value; }
        }

        internal IList Responses
        {
            get
            {
                IList responses = new ClusteredArrayList();

                for (int i = 1; i <= _responses.Count; i++)
                {
                    responses.Add(_responses[i]);
                }

                return responses;
            }
        }

        internal bool IsComplete
        {
            get
            {
                lock (_mutex)
                {
                    bool result = _responses.ContainsKey(FIRST_CHUNK);
                    if (result)
                    {
                        CommandResponse firstChunk = _responses[FIRST_CHUNK];
                        result = _responses.Count == firstChunk.NumberOfChunks;
                    }

                    return result;
                }
            }
        }

        internal void AddResponse(CommandResponse response)
        {
            lock (_mutex)
            {
                if (!_responses.ContainsKey(response.SequenceId))
                    _responses.Add(response.SequenceId, response);
            }
        }

        internal void Clear()
        {
            lock (_mutex)
            {
                _responses.Clear();
            }
        }
    }
}
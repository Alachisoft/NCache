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

using Alachisoft.NCache.Common.Protobuf;
using System.Collections.Generic;
using System.IO;

namespace Alachisoft.NCache.Client
{
    class ResponseIntegrator : IComparer<FragmentedResponse>
    {
        private Dictionary<Common.Net.Address, Dictionary<long, List<FragmentedResponse>>> _serverMap = new Dictionary<Common.Net.Address, Dictionary<long, List<FragmentedResponse>>>();

        public Response AddResponseFragment(Common.Net.Address server, FragmentedResponse responseFragment)
        {
            List<FragmentedResponse> responseList = null;
            Dictionary<long, List<FragmentedResponse>> resposeTable = null;
            lock (this)
            {
                if (!_serverMap.ContainsKey(server))
                {
                    _serverMap.Add(server, new Dictionary<long, List<FragmentedResponse>>());
                }

                resposeTable = _serverMap[server];

                if (!resposeTable.ContainsKey(responseFragment.messageId))
                {
                    resposeTable.Add(responseFragment.messageId, new List<FragmentedResponse>());
                }
            }

            responseList = resposeTable[responseFragment.messageId];
            responseList.Add(responseFragment);

            if (responseList.Count == responseFragment.totalFragments)
            {
                lock (this)
                {
                    resposeTable.Remove(responseFragment.messageId);
                }

                responseList.Sort(this);
                Response finalResponse = null;

                using (MemoryStream stream = new MemoryStream())
                {
                    foreach (FragmentedResponse fragment in responseList)
                    {
                        stream.Write(fragment.message, 0, fragment.message.Length);
                    }

                    stream.Position = 0;
                    finalResponse = ProtoBuf.Serializer.Deserialize<Alachisoft.NCache.Common.Protobuf.Response>(stream);
                    stream.Close();
                }
                return finalResponse;

            }

            return null;
        }

        public int Compare(FragmentedResponse x, FragmentedResponse y)
        {
            if (x.fragmentNo < y.fragmentNo)
                return -1;

            if (x.fragmentNo > y.fragmentNo)
                return 1;

            return 0;
        }

        public void RemoveServer(Common.Net.Address server)
        {
            lock (this)
            {
                if (_serverMap.ContainsKey(server))
                    _serverMap.Remove(server);
            }
        }
    }
}

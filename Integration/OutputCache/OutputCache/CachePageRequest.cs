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
// limitations under the License

namespace Alachisoft.NCache.Web.NOutputCache
{
    class CachePageRequest
    {
        string _pageId;
        object _req_mutex = new object();
        NItem _result;
        bool _requestInProgres;

        public CachePageRequest(string pageId)
        {
            _pageId = pageId;
        }

        public NItem FetchPage(NOutputCache module)
        {
            NItem result = null;
            lock (_req_mutex)
            {
                if (_requestInProgres)
                {
                    System.Threading.Monitor.Wait(_req_mutex);
                    return _result;
                }
                _requestInProgres = true;
            }

            try
            {
                result = module.Cache.Get(_pageId, NOutputCache.Reader.GetPageSettings(_pageId)) as NItem;
            }
            finally
            {
                lock (_req_mutex)
                {
                    _requestInProgres = false;
                    _result = result;
                    System.Threading.Monitor.PulseAll(_req_mutex);
                }
            }
            return _result;
        }
    }
}

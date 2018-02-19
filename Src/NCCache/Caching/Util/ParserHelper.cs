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

using System.Text;
using Alachisoft.NCache.Parser;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Caching.Util
{
    class ParserHelper
    {
        private const string ResourceName = "Alachisoft.NCache.Cache.Caching.Queries.NCQL.cgt";
        private Reduction _currentReduction;
        private ILogger _ncacheLog;
        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }


        public ParserHelper(ILogger NCacheLog)
        {
            this._ncacheLog = NCacheLog;
            
        }

        public ParseMessage Parse(string query)
        {
            NCQLParser parser = new NCQLParser(ResourceName, _ncacheLog);
            System.IO.TextReader tr = new System.IO.StringReader(query);
            ParseMessage message = parser.Parse(tr, true);
            _currentReduction = parser.CurrentReduction;
            return message;
        }

        public Reduction CurrentReduction
        {
            get { return _currentReduction; }
        }
    }
}

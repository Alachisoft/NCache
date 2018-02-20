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

using Alachisoft.NCache.Caching.AutoExpiration;

namespace Alachisoft.NCache.RuntimeDependencies
{
    class DependencyListenerFactory : IDependencyListenerFactory
    {
        public DependencyListener Create(string key, string connString, string queryString,
            IDBConnectionPool connectionPool, IDependencyChangeListener dependencyListener,
            Alachisoft.NCache.Common.Logger.ILogger logger, ExpirationHintType hintType,
            System.Data.CommandType cmdType, System.Collections.IDictionary cmdParams)
        {
#if !NET20
            return new OracleDependencyListener(key, connString, queryString, connectionPool, dependencyListener,
                logger, hintType, cmdType, cmdParams);
#else
            return null;
#endif
        }
    }
}
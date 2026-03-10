//  Copyright (c) 2026 Alachisoft
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


namespace Alachisoft.NCache.Common.Snmp.Oids
{
    public class ParentOids
    {
        public const string Oid = "1.3.6.1.4.1.12";
        public const string NCache = Oid + ".1";

        #region ___________Cache Table_______________
        public const string Cache = NCache + ".1";
        public const string CacheTable = Cache + ".1";
        public const string CacheEntry = CacheTable + ".1";
        #endregion

        #region ___________Client Table_______________
        public const string Client = NCache + ".2";
        public const string ClientTable = Client + ".1";
        public const string ClientEntry = ClientTable + ".1";
        #endregion

        #region ___________Bridge Table_______________
        public const string Bridge = NCache + ".3";
        public const string BridgeTable = Bridge + ".1";
        public const string BridgeEntry = BridgeTable + ".1";
        #endregion
        #region __________Bridge Cache Table______________
        public const string BridgedCache = NCache + ".4";
        public const string BridgedCacheTable = BridgedCache + ".1";
        public const string BridgedCacheEntry = BridgedCacheTable + ".1";
        #endregion

    }
}

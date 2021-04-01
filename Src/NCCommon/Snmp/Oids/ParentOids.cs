﻿//  Copyright (c) 2021 Alachisoft
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
        public const string Server = NCache + ".0";
        public const string Cache = NCache + ".1";
        public const string Client = NCache + ".2";

    }
}

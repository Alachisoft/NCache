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
using System;
namespace Alachisoft.NCache.Caching.AutoExpiration
{
    public interface IDBConnectionPool
    {
        global::System.Data.IDbConnection AddToDbConnectionPool(string connectionString, global::System.Data.IDbConnection connection);
        void RemoveFromDbConnectionPool(string connectionString);
        void RemoveFromDbConnectionPool(string connectionString, bool isConnectionInvalid);
        global::System.Data.IDbConnection GetConnection(string connString);
    }
}

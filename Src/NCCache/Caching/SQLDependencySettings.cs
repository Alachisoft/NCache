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
using System.Net;
using System.Data.SqlClient;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching
{
    //
    /// This change is made for clients who wants to use SQLDependency without SERVICE and QUEUE creation rights. 
    /// Initial feature request is from BofA.
    /// Following decisions are made:
    ///         1. When secure SQLDependency in enabled cache will not create the SQL Servicer/Queue
    ///         2. In above case predefined named of service and queue will be used; user will have to create
    ///            the service and queue before using SQLDependency. In return we will connect with predefined service and queue 
    ///         3. Service and Queue Name will be "NCacheSQLService-[ip-address]", "NCacheSQLQueue-[ip-address]"; here ip-address will be of machine on which NCache service process will be running.
    ///       

    public class SQLDependencySettings
    {
        private readonly string SERVICE_PREFIX = "NCacheSQLService-";
        private readonly string QUEUE_PREFIX = "NCacheSQLQueue-";

        private bool _useDefaultServiceQueue = true;
        private string _serviceName = null;
        private string _queueName = null;

        public SQLDependencySettings()
        {
           SERVICE_PREFIX = ServiceConfiguration.NCacheSQLNotificationService + "-";
           QUEUE_PREFIX = ServiceConfiguration.NCacheSQLNotificationQueue + "-";

        }

        public void initialize(bool useDefaultServiceQueue, IPAddress ipAddress)
        {
            _useDefaultServiceQueue = useDefaultServiceQueue;
            if (_useDefaultServiceQueue == false)
            {
                _serviceName = SERVICE_PREFIX + ipAddress.ToString();
                _queueName = QUEUE_PREFIX + ipAddress.ToString();
            }
        }

        public bool UseDefaultServiceQueue
        {
            get { return _useDefaultServiceQueue;  }
        }

        public string ServiceName
        {
            get { return _serviceName; }
        }

        public string QueueName
        {
            get { return _queueName; }
        }

        public string GetDependencyOptions(string connString)
        {
            SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connString);
            string dbname = sqlConnectionStringBuilder.InitialCatalog;
            return String.Format("Service={0};local database={1}", ServiceName, sqlConnectionStringBuilder.InitialCatalog);
        }
    }
}

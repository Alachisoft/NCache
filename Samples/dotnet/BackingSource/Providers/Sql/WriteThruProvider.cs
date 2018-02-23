// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Collections;
using Alachisoft.NCache.Runtime.DatasourceProviders;
using Alachisoft.NCache.Sample.Data;
using Alachisoft.NCache.Samples.Utility;

namespace Alachisoft.NCache.Samples.Providers
{
    /// <summary>
    /// Contains methods used to save/update an object to the master data source. 
    /// </summary>
    public class SqlWriteThruProvider : Alachisoft.NCache.Runtime.DatasourceProviders.IWriteThruProvider
    {
        /// <summary>
        /// Object used to communicate with the Datasource.
        /// </summary>
        private SqlDatasource sqlDatasource;

        /// <summary>
        /// Perform tasks like allocating resources or acquiring connections
        /// </summary>
        /// <param name="parameters">Startup paramters defined in the configuration</param>
        /// <param name="cacheId">Define for which cache provider is configured</param>
        public void Init(IDictionary parameters, string cacheId)
        {
            object connString = parameters["connstring"];
            sqlDatasource = new SqlDatasource();
            sqlDatasource.Connect(connString == null ? "" : connString.ToString());
        }

        /// <summary>
        /// Perform tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        public void Dispose()
        {
            sqlDatasource.DisConnect();
        }

        #region IWriteThruProvider Members

        public OperationResult WriteToDataSource(WriteOperation operation)
        {
            bool result = false;
            OperationResult operationResult = new OperationResult(operation, OperationResult.Status.Failure);
            Customer value = (Customer)operation.ProviderCacheItem.Value;
            if (value.GetType().Equals(typeof(Customer)))
            {
                result = sqlDatasource.SaveCustomer((Customer)value);
            }
            if (result) operationResult.DSOperationStatus = OperationResult.Status.Success;
            return operationResult;
        }

        public OperationResult[] WriteToDataSource(WriteOperation[] operation)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}

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

using System.Collections;

namespace Alachisoft.NCache.Runtime.DatasourceProviders
{
    /// <summary>
    /// Contains methods used to save/update an object to the master data source. Must be 
    /// implemented by write-through components.
    /// </summary>

    public interface IWriteThruProvider
    {
        /// <summary>
        /// Perform tasks like allocating resources or acquiring connections etc.
        /// </summary>
        /// <param name="parameters">Startup paramters defined in the configuration</param>
        /// <param name="cacheId">Id of the Cache</param>
        void Init(IDictionary parameters, string cacheId);
        /// <summary>
        /// Responsible for atomic write operations on data source.
        /// </summary>
        /// <param name="operation">write operation applied on data source</param>
        /// <returns>failed operations, null otherwise</returns>
        OperationResult WriteToDataSource(WriteOperation operation);
        /// <summary>
        /// Responsible for bulk write operations on data source.
        /// </summary>
        /// <param name="operation">array of write operations applied on data source</param>
        /// <returns>array of failed operations</returns>
        OperationResult[] WriteToDataSource(WriteOperation[] operations);
        /// <summary>
        /// Perform tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        void Dispose();
        #region Previous IWriteThruProvider methods
        /// <summary>
        /// Responsible for updating/inserting an object to the data source. The Key and the 
        /// object are passed as parameter.
        /// </summary>
      

        /// <summary>
        /// Responsible for adding cache item in data source.
        /// Cache item will not be added to cache if it fails to add in data source.
        /// </summary>
        /// <param name="key">key used to reference the cache item</param>
        /// <param name="value">cache item</param>
        /// <returns>true if add operation is successful, false otherwise</returns>
      

        /// <summary>
        /// Responsible for updating cache item in data source.
        /// Cache item will not be updated in cache if it fails to update in data source.
        /// </summary>
        /// <param name="key">key used to reference the object</param>
        /// <param name="value">cache item</param>
        /// <returns>true if update operation is successful, false otherwise</returns>
       

        /// <summary>
        /// Responsible for removing cache item in data source.
        /// Cache item will not be removed from cache if it fails is not removed from data source.
        /// </summary>
        /// <param name="key">key used to reference the  cache item</param>
        /// <returns>true if remove operation is successful, false otherwise</returns>
      
        
        /// <summary>
        /// Clears data source. Cache will not be cleared if data source is not cleared
        /// </summary>
        /// <returns>true if clear operation is successful, false otherwise</returns>
       

        /// <summary>
        /// Responsible for adding array of  cache items in data source.
        /// Cache items,failed to add, will not be added in cache
        /// </summary>
        /// <param name="keys">array of keys</param>
        /// <param name="vals">array of cache items</param>
        /// <returns>dictionary of keys, that failed, and exception</returns>
       

        /// <summary>
        /// Responsible for updating array of  cache items in data source.
        /// Cache items,failed to update, will not be updated in cache
        /// </summary>
        /// <param name="keys">array of keys</param>
        /// <param name="vals">array of cache items</param>
        /// <returns>dictionary of keys, that failed, and exception</returns>
        

        /// <summary>
        /// Responsible for removing array of cache items from data source.
        /// Cache items, failed to remove, will not be removed in cache
        /// </summary>
        /// <param name="keys">array of keys</param>
        /// <returns>dictionary of keys, that failed, and exception</returns>
       
        #endregion
    }
}
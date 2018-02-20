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
using System.Threading;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Web.Caching;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that implements Oracle dependency
    /// </summary>
	public class OracleDependency
	{
        private static Cache _cache;

        /// <summary>
        /// Executing this method will perform all the operations to configure oracle dependency
        /// </summary>
        public static void Run()
		{
			// Initialize cache
            InitializeCache();

            // Create and aply oracle dependency
            AddDependency();

			// Must dispose cache
			_cache.Dispose();
		}

        /// <summary>
        /// This method initializes the cache
        /// </summary>
        private static void InitializeCache()
        {
            string cache = ConfigurationManager.AppSettings["CacheID"];

            if (String.IsNullOrEmpty(cache))
            {
                Console.WriteLine("The Cache Name cannot be null or empty.");
                return;
            }

            // Initialize an instance of the cache to begin performing operations:
            _cache = NCache.Web.Caching.NCache.InitializeCache(cache);
        }

        /// <summary>
        /// This method configures oracle dependency
        /// </summary>
        private static void AddDependency()
        {
            // Change notification previleges must be granted for OracleDBDependency to work
            // -- run this script
            // -- 'grant change notification to username' without quotes where username 
            // is to replaced with one being used by the db instance

            // Notification based DBDependency
            // JDBC connectionString                         

            string connectionString = System.Configuration.ConfigurationManager.AppSettings["conn-string"];
            string commandText = "SELECT PRODUCTID, rowId, PRODUCTNAME FROM PRODUCTS WHERE PRODUCTID = 1";

            CacheDependency oracleDependency = new OracleCacheDependency(connectionString, commandText);

            CacheItem cacheItem = new CacheItem("OracleDependentValue");
            cacheItem.Dependency = oracleDependency;

            _cache.Add("Item:1", cacheItem);

            // Any record that is modified in the database is invalidated in cache and thus the cache item is removed.
            // To Verify modify the record in the database and uncomment the code below
            // The code checks if the record is removed form the cache after modification.
            
            //if (_cache.Contains("Item:1"))
            //{
            //    Console.WriteLine("Oracle dependency did not work as expected. " + "Check your connection string and sql command syntax.");
            //}
            //else
            //{
            //    Console.WriteLine("Item:1 removed due to oracle dependency.");
            //}
        }
	}
}
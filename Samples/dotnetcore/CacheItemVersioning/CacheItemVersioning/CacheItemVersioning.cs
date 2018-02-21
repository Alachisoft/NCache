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
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the sample
    /// </summary>
	public class CacheItemVersioning
	{
        private static Cache _cache;

        /// <summary>
        /// Executing this method will perform all the operations of the sample
        /// </summary>
		public static void Run()
		{
            // Initialize cache
            InitializeCache();

            // Generate instance of Customer that will be used in different operations in this sample
            Customer customer = CreateNewCustomer();
            string key = GetKey(customer);

			// Cache item versioning makes possible the concurrency checks on cacheitems.
			// Scenario:
			// User X and Y has fetched data from cache
			// User Y makes changes to some cache items and write back the changes to cache
			// Now the version of cache items with User X are obsolete and it can determine 
			// so by using cache item versioning.

            // Adding item
            CacheItemVersion itemversion = AddObjectToCache(key, customer);

            // Update an item 
            CacheItemVersion latestItemversion = UpdateObjectInCache(key, customer, itemversion);

            // Get an item with cache item version
            GetObjectWithItemVersion(key, itemversion, latestItemversion);

            // Remove an item if cache item 
            RemoveObjectWithItemVersion(key, latestItemversion);

            // Dispose the cache once done
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
                Console.WriteLine("The CacheID cannot be null or empty.");
                return;
            }

            // Initialize an instance of the cache to begin performing operations:
            _cache = NCache.Web.Caching.NCache.InitializeCache(cache);

            // Print output on console
            Console.WriteLine(string.Format("\nCache '{0}' is initialized.", cache));
        }

        /// <summary>
        /// This method adds object in the cache
        /// </summary>
        /// <param name="key"> String key to be added in cache</param>
        /// <param name="customer"> Instance of Customer that will be inserted in the cache</param>
        /// <returns>Returns cache item version</returns>
        private static CacheItemVersion AddObjectToCache(string key, Customer customer)
        {
            CacheItemVersion cacheItemVersion = _cache.Add(key, customer);

            // Print output on console
            Console.WriteLine("\nItem is added to cache.");

            return cacheItemVersion;
        }

        /// <summary>
        /// This method updates object in the cache if Cache Item Version matches 
        /// </summary>
        /// <param name="key"> String key to be updated in cache </param>
        /// <param name="customer"> Instance of Customer that will be updated in the cache </param>
        /// <param name="oldItemVersion"> Instance of old CacheItemVersion to verify item is updated in cache </param>
        /// <returns>Returns updated cache item version </returns>
        private static CacheItemVersion UpdateObjectInCache(string key, Customer customer, CacheItemVersion oldItemVersion)
        {
            // Updaing the customer object in cache;
            customer.CompanyName = "Gourmet Lanchonetes";

            // Create a CacheItem instance and assign Cache Item Version
            CacheItem cacheItem = new CacheItem(customer);
            cacheItem.Version = oldItemVersion;

            // Update item if version match
            CacheItemVersion updatedItemVersio = _cache.Insert(key, cacheItem);

            if (oldItemVersion.Version != updatedItemVersio.Version)
            {
                // Print output on console
                Console.WriteLine("\nItem has changed since last time it was fetched.");
            }

            return updatedItemVersio;
        }

        /// <summary>
        /// This method Gets an object from cache using cache item version
        /// </summary>
        /// <param name="key"> String key to be fetched from cache </param>
        /// <param name="oldCacheItemVersion"> Instance of CacheItemVersion to get item from cache if an updated item exist </param>
        /// <param name="latestCacheItemVersion"> Instance of CacheItemVersion to get item from cache </param>
        private static void GetObjectWithItemVersion(string key, CacheItemVersion oldCacheItemVersion, CacheItemVersion latestCacheItemVersion)
        {
            // Retrives item from cache based on CacheItemVersion
            // Get item only is version superior to old version
            Customer customer = (Customer)_cache.GetIfNewer(key, ref oldCacheItemVersion); //Version to be compared from cache

            if (customer == null)
            {
                // Print output on console
                Console.WriteLine("\nSpecified item version is latest.");
            }
            else
            {
                // Print output on console
                Console.WriteLine(string.Format("\nCurrent Version of {0} is: {1}", GetKey(customer), oldCacheItemVersion.Version));
                PrintCustomerDetails(customer);
            }

            // Get an item from cache from latest item version
            customer = (Customer)_cache.GetIfNewer(key, ref latestCacheItemVersion); //Version to be compared from cache

            if (customer == null)
            {
                // Print output on console
                Console.WriteLine("\nSpecified item version is latest.");
            }
            else
            {
                // Print output on console
                Console.WriteLine(string.Format("\nCurrent Version of {0} is: {1}", GetKey(customer), latestCacheItemVersion.Version));
                PrintCustomerDetails(customer);
            }
        }

        /// <summary>
        /// Delete an object in the cache using cache item version
        /// </summary>
        /// <param name="key"> String key to be removed from cache </param>
        /// <param name="cacheItemVersion"> Instance of CacheItemVersion to remove item from cache if item version is same in cache </param>
        private static void RemoveObjectWithItemVersion(string key, CacheItemVersion cacheItemVersion)
        {
            // Remove object from cache if cache item version matched in cache
            Customer customerRemoved = (Customer)_cache.Remove(key, cacheItemVersion);

            if (customerRemoved == null)
            {
                // Print output on console
                Console.WriteLine("\nFailed to remove item from cache. The newer version of item exists in cache.");
            }
            else
            {
                // Print output on console
                Console.WriteLine("\nFollowing Customer is removed from cache:");
                PrintCustomerDetails(customerRemoved);
            }
        }

        /// <summary>
        /// Generates instance of Customer to be used in this sample
        /// </summary>
        /// <returns> returns instance of Customer </returns>
        private static Customer CreateNewCustomer()
        {
            return new Customer
            {
                ContactName = "Scott Prince",
                CompanyName = "Lonesome Pine Restaurant",
			    ContactNo = "25632-5646",
                Address = "95-A Barnes Road Wallingford, CT",
            };
        }

        /// <summary>
        /// Generates a string key for specified customer
        /// </summary>
        /// <param name="customer"> Instance of Customer to generate a key</param>
        /// <returns> returns a key </returns>
        private static string GetKey(Customer customer)
        {
            return string.Format("Customer:{0}", customer.ContactName);
        }

        /// <summary>
        /// Method for printing details of customer type.
        /// </summary>
        /// <param name="customer"></param>
        private static void PrintCustomerDetails(Customer customer)
		 {
             Console.WriteLine();
             Console.WriteLine("Customer Details are as follows: ");
             Console.WriteLine("ContactName: " + customer.ContactName);
             Console.WriteLine("CompanyName: " + customer.CompanyName);
             Console.WriteLine("Contact No: " + customer.ContactNo);
             Console.WriteLine("Address: " + customer.Address);
             Console.WriteLine();
		 }
	}

}
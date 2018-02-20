// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Sample.Data;
using Alachisoft.NCache.Web.Caching;
using System;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the sample
    /// </summary>
    public class BasicOperations
    {
        private static Cache _cache;

        /// <summary>
        /// Executing this method will perform all the operations of the sample
        /// </summary>
        public static void Run()
        {
            // Initialize cache 
            InitializeCache();

            // Create a simple customer object
            Customer customer = CreateNewCustomer();
            string key = GetKey(customer);

            // Adding item synchronously
            AddObjectToCache(key, customer);

            // Get the object from cache
            customer = GetObjectFromCache(key);

            // Modify the object and update in cache
            UpdateObjectInCache(key, customer);

            // Delete the existing object
            DeleteObjectFromCache(key);

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
        /// This method adds object in the cache using synchronous api
        /// </summary>
        /// <param name="key"> String key to be added in cache </param>
        /// <param name="customer"> Instance of Customer that will be added to cache </param>
        private static void AddObjectToCache(string key, Customer customer)
        {
            DateTime expirationInterval = new DateTime();
            expirationInterval.AddMinutes(1);

            // Adding item with an absolute expiration of 1 minute
            _cache.Add(key, customer, expirationInterval, Cache.NoSlidingExpiration, CacheItemPriority.Normal);

            // Print output on console
            Console.WriteLine("\nObject is added to cache.");
        }

        /// <summary>
        /// This method gets an object from the cache using synchronous api
        /// </summary>
        /// <param name="key"> String key to get object from cache</param>
        /// <returns> returns instance of Customer retrieved from cache</returns>
        private static Customer GetObjectFromCache(string key)
        {
            Customer cachedCustomer = (Customer)_cache.Get(key);

            // Print output on console
            Console.WriteLine("\nObject is fetched from cache");

            PrintCustomerDetails(cachedCustomer);
            return cachedCustomer;
        }

        /// <summary>
        /// This method updates object in the cache using synchronous api
        /// </summary>
        /// <param name="key"> String key to be updated in cache</param>
        /// <param name="customer"> Instance of Customer that will be updated in the cache</param>
        private static void UpdateObjectInCache(string key, Customer customer)
        {
            // Update item with a sliding expiration of 30 seconds
            customer.CompanyName = "Gourmet Lanchonetes";
            _cache.Insert(key, customer, Cache.NoAbsoluteExpiration, new TimeSpan(0, 0, 30), CacheItemPriority.Normal);

            // Print output on console
            Console.WriteLine("\nObject is updated in cache.");
        }

        /// <summary>
        /// Delete an object in the cache using synchronous api
        /// </summary>
        /// <param name="key"> String key to be deleted from cache</param>
        private static void DeleteObjectFromCache(string key)
        {
            // Remove the existing customer
            _cache.Delete(key);

            // Print output on console
            Console.WriteLine("\nObject is deleted from cache.");
        }

        /// <summary>
        /// Generates instance of Customer to be used in this sample
        /// </summary>
        /// <returns> returns instance of Customer </returns>
        private static Customer CreateNewCustomer()
        {
            return new Customer
            {
                ContactName = "David Johnes",
                CompanyName = "Lonesome Pine Restaurant",
                ContactNo = "12345-6789",
                Address = "Silicon Valley, Santa Clara, California",
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
        /// This method prints detials of customer type.
        /// </summary>
        /// <param name="customer"></param>
        private static void PrintCustomerDetails(Customer customer)
        {
            if (customer == null) return;

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
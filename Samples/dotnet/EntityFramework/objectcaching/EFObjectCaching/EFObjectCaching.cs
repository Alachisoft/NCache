// ===============================================================================
// Alachisoft (R) NCache Sample Code
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Linq;
using Alachisoft.NCache.Web.Caching;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the sample
    /// </summary>
    class EFObjectCaching
    {
        private static Cache _cache;
        private static NorthwindEntities _database;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Run()
        {
            // Initialize cache 
            InitializeCache();

            // Initialize customer factory
            _database = new NorthwindEntities();

            string customerId = "Alfki";

            //Fetch customer; first time it will be loaded from database
            Customer customer = LoadCustomer(customerId);

            //this call will load customer from cache
            customer = LoadCustomer(customerId);

            //Let's update the customer
            customer.Country = "France";
            UpdateCustomer(customer);
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
        /// Loads customer for a given customer id. First it looks up from cache. if not found it fetches from database
        /// and puts it to the cache.
        /// </summary>
        /// <param name="customerId">customer's id</param>
        /// <returns></returns>
        private static Customer LoadCustomer(string customerId)
        {
            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentException("customerId is null or an empty string");

            Customer customer = null;

            //First try to fech item from cache
            string cacheKey = "Customer#" + customerId;
            customer = _cache.Get(cacheKey) as Customer;

            //if cache miss occurs, fetch from database and put into the cache; next calls to this method will be served from cache
            if (customer == null)
            {
                Console.WriteLine("Customer loaded from database sucessfully.");
                var query = from c in _database.Customers where c.CustomerID == customerId select c;

                customer = query.First();

                //Insert data into the cache
                if(customer != null)
                {
                    CacheItem cacheItem = new CacheItem(customer);
                    //set absolute expiration of 30 seconds. 
                    cacheItem.AbsoluteExpiration = DateTime.Now.Add(new TimeSpan(0,0,30));

                    _cache.Insert(cacheKey,cacheItem);
                }
            }
            else
            {

                Console.WriteLine("Customer loaded from cache sucessfully.");
                //add or attach customer object with database context
                if (!_database.Customers.Any(p => p.CustomerID == customer.CustomerID))
                    _database.Customers.Attach(customer);
            }

            return customer;
        }

        /// <summary>
        /// This method updates customer and puts it to the cache.
        /// </summary>
        /// <param name="customerId">customer instance</param>
        private static void UpdateCustomer(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            //update item into the database
            _database.SaveChanges();

            //now update customer object in cache
            string cacheKey = "Customer#" + customer.CustomerID;

            CacheItem cacheItem = new CacheItem(customer);
            //set absolute expiration of 30 seconds. 
            cacheItem.AbsoluteExpiration = DateTime.Now.Add(new TimeSpan(0, 0, 30));
            _cache.Insert(cacheKey, cacheItem);

            Console.WriteLine("Customer is successfully updated in cache.");
        }
    }
}






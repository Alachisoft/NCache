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
using System.Threading;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the sample
    /// </summary>
	public class AsynchronousOperations
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

            // Adding item asynchronously
            AddObjectToCacheAsynchronous(key, customer);

            // Modify the object and update in cache
            UpdateObjectInCacheAsynchronous(key, customer);

            // Remove the existing object asynchronously
            RemoveObjectFromCacheAsynchronous(key);

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
        /// This method adds object in the cache using async api
        /// </summary>
        /// <param name="key"> String key to be added in cache</param>
        /// <param name="customer"> Instance of Customer that will be added to cache</param>
        private static void AddObjectToCacheAsynchronous(string key, Customer customer)
        {
            //Adding item asynchronously
            //Another way is by creating a CacheItem  object
            _cache.AddAsync(key, customer, new AsyncItemAddedCallback(OnItemAdded), string.Empty, string.Empty);

            // Wait for the callback.
            Thread.Sleep(1000);
        }

        /// <summary>
        /// This method updates object in the cache using async api
        /// </summary>
        /// <param name="key"> String key to be updated in cache</param>
        /// <param name="customer"> Instance of Customer that will be updated in the cache</param>
        private static void UpdateObjectInCacheAsynchronous(string key, Customer customer)
        {
            // Updating and re-insert the item asynchronously
            customer.CompanyName = "Gourmet Lanchonetes";
            _cache.InsertAsync(key, customer, new AsyncItemUpdatedCallback(OnItemUpdate), string.Empty, string.Empty);
            
            // Wait for the callback.
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Delete an object in the cache using async api
        /// </summary>
        /// <param name="key"> String key to be removed from cache</param>
        private static void RemoveObjectFromCacheAsynchronous(string key) 
        {
            // Print the total count of items in cache
            long count = _cache.Count;

            // Remove the existing customer asynchronously
            _cache.RemoveAsync(key, new AsyncItemRemovedCallback(OnItemRemoved), DSWriteOption.None, null);
            
            // Wait for the callback.
            Thread.Sleep(1000);

            // Get the count again, it should be 0
            count = _cache.Count;
        }

        /// <summary>
        /// The callback method which will be executed each time an async add operation completes.
        /// </summary>
        /// <param name="eventArgs"> Async operation event arguments </param>
        private static void OnItemAdded(string key, object status)
        {
            if (status.ToString().Equals("Success"))
            {
                // Print output on console
                Console.WriteLine("\nObject is added to cache.");

                Customer cachedCustomer = (Customer)_cache.Get(key);
                PrintCustomerDetails(cachedCustomer);
            }
            if (status.ToString().Equals("Failure"))
            {
                Console.WriteLine(string.Format("Failed to add key '{0}' in cache.", key));
            }
            if(status is Exception)
            {
                Console.WriteLine(string.Format("Failed to add key '{0}' in cache due to {}.", key, status.ToString()));
            }
        }

        /// <summary>
        /// The callback method which will be executed each time an async update operation completes.
        /// </summary>
        /// <param name="eventArgs"> Async operation event arguments </param>
        public static void OnItemUpdate(string key, object status)
        {
            if (status.ToString().Equals("Success"))
            {
                // Print output on console
                Console.WriteLine("\nObject is updated in cache.");

                Customer cachedCustomer = (Customer)_cache.Get(key);
                PrintCustomerDetails(cachedCustomer);
            }
            if (status.ToString().Equals("Failure"))
            {
                Console.WriteLine(string.Format("Failed to update key '{0}' in cache.", key));
            }
            if (status is Exception)
            {
                Console.WriteLine(string.Format("Failed to update key '{0}' in cache due to {}.", key, status.ToString()));
            }
        }

        /// <summary>
        /// The callback method which will be executed each time an async remove operation completes.
        /// </summary>
        /// <param name="eventArgs"> Async operation event arguments </param>
        public static void OnItemRemoved(string key, object status)
        {
            if (status.ToString().Equals("Success"))
            {
                // Print output on console
                Console.WriteLine("\nObject is removed from cache.");

                Customer cachedCustomer = (Customer)_cache.Get(key);
                PrintCustomerDetails(cachedCustomer);
            }
            if (status.ToString().Equals("Failure"))
            {
                Console.WriteLine(string.Format("Failed to remove key '{0}' in cache.", key));
            }
            if (status is Exception)
            {
                Console.WriteLine(string.Format("Failed to remove key '{0}' in cache due to {}.", key, status.ToString()));
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
                ContactName = "David Johnes",
                CompanyName= "Lonesome Pine Restaurant",
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
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
    public class ItemLocking
    {
        private static Cache _cache;

        public static void Run()
        {
            // Initialize cache            
            InitializeCache();

            // Add item in cache
            AddItemInCache();

            // Create new lock handle to fetch Item usin locking
            LockHandle lockHandle = new LockHandle();

            // Timespan for which lock will be taken
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 20);

            // Get item from cache
            GetItemFromCache(lockHandle, timeSpan);

            // Lock Item in cache 
            LockItemInCache(lockHandle, timeSpan);

            // Unlock item in cache using multiple ways
            UnLockItemInCache(lockHandle);

            // Removing item from cache 
            RemoveItemFromCache();

            // Dispose cache once done
            _cache.Dispose();

        }


        /// <summary>
        /// This method initializes the cache.
        /// </summary>
        private static void InitializeCache()
        {
            string cache = ConfigurationManager.AppSettings["CacheId"];

            if (String.IsNullOrEmpty(cache))
            {
                Console.WriteLine("The Cache Name cannot be null or empty.");
                return;
            }

            // Initialize an instance of the cache to begin performing operations:
            _cache = NCache.Web.Caching.NCache.InitializeCache(cache);

            Console.WriteLine("Cache initialized successfully");
        }

        /// <summary>
        /// This method add items in the cache.
        /// </summary>
        private static void AddItemInCache()
        {
            //Adding an item the cache
            Customer customer = new Customer();
            customer.ContactName = "Kirsten Goli";
            customer.Address = "45-A West Boulevard, Cartago, Costa Rica";
            customer.ContactNo = "52566-1779";

            _cache.Add("Customer:KirstenGoli", customer);
        }

        private static void RemoveItemFromCache()
        {
            _cache.Delete("Customer:KirstenGoli");
        }

        /// <summary>
        /// This method fetches item from the cache.
        /// </summary>
        /// <param name="lockHandle"> An instance of lock handle that will be used for locking the fetched item. </param>
        /// <param name="timeSpan"> Time for which the lock will be held. </param>
        private static void GetItemFromCache(LockHandle lockHandle, TimeSpan timeSpan)
        {
            // Get
            Customer getCustomer = (Customer)_cache.Get("Customer:KirstenGoli", timeSpan, ref lockHandle, true);

            PrintCustomerDetails(getCustomer);
            Console.WriteLine("Lock acquired on " + lockHandle.LockId);
        }

        /// <summary>
        /// This method locks specified item in the cache
        /// </summary>
        /// <param name="lockHandle"> Handle of the lock. </param>
        /// <param name="timeSpan"> Time for which lock will be held. </param>
        private static void LockItemInCache(LockHandle lockHandle, TimeSpan timeSpan)
        {
            // Lock item in cache
            bool isLocked = _cache.Lock("Customer:KirstenGoli", timeSpan, out lockHandle);

            if (!isLocked)
            {
                Console.WriteLine("Lock acquired on " + lockHandle.LockId);
            }
        }

        /// <summary>
        /// This method unlocks the item in the cache using 2 different ways.
        /// </summary>
        /// <param name="lockHandle"> The lock handle that was used to lock the item. </param>
        private static void UnLockItemInCache(LockHandle lockHandle)
        {
            _cache.Unlock("Customer:KirstenGoli", lockHandle);

            //Forcefully unlock item in cache 
            //_cache.Unlock("Customer:KirstenGoli");
        }

        /// <summary>
        /// Method for printing customer type details.
        /// </summary>
        /// <param name="customer"></param>
        public static void PrintCustomerDetails(Customer customer)
        {
            Console.WriteLine();
            Console.WriteLine("Customer Details are as follows: ");
            Console.WriteLine("Name: " + customer.ContactName);
            Console.WriteLine("Contact No: " + customer.ContactNo);
            Console.WriteLine("Address: " + customer.Address);
        }

    }
}
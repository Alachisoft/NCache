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
using System.Configuration;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Sample.Data;

namespace Alachisoft.NCache.Samples
{
    public class ItemLocking
    {
        private static ICache _cache;
        private static string key="Customer:KirstenGoli";
        public static void Run()
        {
            // Initialize cache            
            InitializeCache();

            // Add item in cache
            AddItemInCache(key);

            // Create new lock handle to fetch Item usin locking
            LockHandle lockHandle = new LockHandle();

            // Timespan for which lock will be taken
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 20);

            // Get item from cache
            GetItemFromCache(key, lockHandle, timeSpan);

            // Lock Item in cache 
            LockItemInCache(key,lockHandle, timeSpan);

            // Unlock item in cache using multiple ways
            UnLockItemInCache(key,lockHandle);

            RemoveItemFromCache(key);

            // Dispose cache once done
            _cache.Dispose();
        }

        /// <summary>
        /// This method initializes the cache.
        /// </summary>
        private static void InitializeCache ()
        {
            string cache = ConfigurationManager.AppSettings["CacheId"];

            if (String.IsNullOrEmpty(cache))
            {
                Console.WriteLine("The Cache Name cannot be null or empty.");
                return;
            }

            // Initialize an instance of the cache to begin performing operations:
            _cache = CacheManager.GetCache(cache);
            Console.WriteLine("Cache initialized successfully");
        }
        /// <summary>
        /// This method add items in the cache.
        /// </summary>
        private static void AddItemInCache(string customerId)
        {
            //Adding an item the cache
            Customer customer = new Customer();
            customer.CustomerID = customerId;
            customer.ContactName = "Kirsten Goli";
            customer.Address = "45-A West Boulevard, Cartago, Costa Rica";
            customer.ContactNo = "52566-1779";

            _cache.Add(customer.CustomerID, customer);
        }

        private static void RemoveItemFromCache(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// This method fetches item from the cache.
        /// </summary>
        /// <param name="lockHandle"> An instance of lock handle that will be used for locking the fetched item. </param>
        /// <param name="timeSpan"> Time for which the lock will be held. </param>
        private static void GetItemFromCache(string key,LockHandle lockHandle, TimeSpan timeSpan)
        {            
            // GetT Get<T> (string key, bool acquireLock, TimeSpan lockTimeout, ref LockHandle lockHandle);
            Customer getCustomer = _cache.Get<Customer>(key, true,timeSpan, ref lockHandle);

            PrintCustomerDetails(getCustomer);
            Console.WriteLine("Lock acquired on " + lockHandle.LockId);
        }

        /// <summary>
        /// This method locks specified item in the cache
        /// </summary>
        /// <param name="lockHandle"> Handle of the lock. </param>
        /// <param name="timeSpan"> Time for which lock will be held. </param>
        private static void LockItemInCache(string key,LockHandle lockHandle, TimeSpan timeSpan)
        {
            // Lock item in cache
            bool isLocked = _cache.Lock(key, timeSpan, out lockHandle);

            if (!isLocked)
            {
                Console.WriteLine("Lock acquired on " + lockHandle.LockId);
            }
        }

        /// <summary>
        /// This method unlocks the item in the cache using 2 different ways.
        /// </summary>
        /// <param name="lockHandle"> The lock handle that was used to lock the item. </param>
        private static void UnLockItemInCache(string key,LockHandle lockHandle)
        {
            _cache.Unlock(key, lockHandle);

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
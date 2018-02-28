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
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Sample.Data;
using Alachisoft.NCache.Web.Caching;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    public class Events
    {
        private static Cache _cache;
        private static CacheEventDescriptor _eventDescriptor;

        public static void Run()
        {
            try
            {
                // Initialize cache
                InitializeCache();

                // Generate a new instance of product
                string key = "Item:1";
                Product product1 = GenerateProduct(1);

                // Add item in cache
                AddItem(key, product1);

                // Key based notifications allows item notifications for particular items in 
                CacheDataNotificationCallback myMethod = new CacheDataNotificationCallback(KeyNotificationMethod);
                _cache.RegisterCacheNotification("Item:1", myMethod, EventType.ItemAdded | EventType.ItemRemoved | EventType.ItemUpdated);
                
                // Update item to trigger key based notification.
                UpdateItem(key, product1);
                
                // Delete item to trigger key based notification.
                DeleteItem(key);
                
                // Dispose cache once done
                _cache.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


        /// <summary>
        /// This method generates a new instance of product class with some data.
        /// </summary>
        /// <param name="id"> This id will be set as productId</param>
        /// <returns> Returns a populated instance of a product. </returns>
        private static Product GenerateProduct(int id)
        {
            Product product = new Product();
            switch (id)
            {
                case 1:
                    product.Id = id;
                    product.Name = "product1";
                    product.Category = "category1";
                    break;
                case 2:
                    product.Id = id;
                    product.Name = "custom event product1";
                    product.Category = "custom category";
                    break;
                default:
                    product.Id = id;
                    product.Name = "product" + id;
                    product.Category = "category" + id;
                    break;
            }
            return product;
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
        /// This method adds the specified item in cache.
        /// </summary>
        /// <param name="key"> key against which item will be added. </param>
        /// <param name="product"> Item that will be added in the cache. </param>
        private static void AddItem(string key, Product product)
        {
            _cache.Add(key, product);
        }

        /// <summary>
        /// This method updates the specified item in the cache.
        /// </summary>
        /// <param name="key"> key against which item will be updated. </param>
        /// <param name="product"> Item that will be updated in the cache. </param>
        private static void UpdateItem(string key, Product product)
        {
            product.Name = "updatedProduct1";
            _cache.Insert(key, product);
        }

        /// <summary>
        /// This method deletes the specified item in the cache.
        /// </summary>
        /// <param name="key"> key against which item will be deleted. </param>
        private static void DeleteItem(string key)
        {
            _cache.Delete(key);
        }


        private static void KeyNotificationMethod(string key, CacheEventArg cacheEventArgs)
        {
            switch (cacheEventArgs.EventType)
            {
                case EventType.ItemAdded:
                    Console.WriteLine("Key: " + key + " is added to the cache");
                    break;
                case EventType.ItemRemoved:
                    Console.WriteLine("Key: " + key + " is removed from the cache");
                    break;
                case EventType.ItemUpdated:
                    Console.WriteLine("Key: " + key + " is updated in the cache");
                    break;
            }
        }
    }

}
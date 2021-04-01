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
using System.Threading;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Sample.Data;

namespace Alachisoft.NCache.Samples
{
    public class Events
    {
        private static ICache _cache;
        private static CacheEventDescriptor _eventDescriptor;

        public static void Run()
        {
            // Initialize cache
            InitializeCache();

            // Generate a new instance of product
            Product product1 = GenerateProduct(1);

            string key = GetKey(product1);

            // Add item in cache
            AddItem(key, product1);

            // Register Notification for given key
            AddNotificationOnKey(key);

            // Update item to trigger key based notification.
            UpdateItem(key, product1);

            // Delete item to trigger key based notification.
            DeleteItem(key);

            Thread.Sleep(5000);
          

            // Dispose cache once done
            _cache.Dispose();
        }

        private static string GetKey (Product product)
        {
            return "Item:" + product.Id.ToString();
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
            _cache = CacheManager.GetCache(cache); 
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
            Console.WriteLine("Object Added in Cache");
        }
        /// <summary>
        ///  This method register notification for given key. Event will be trigger on key updated and removed
        /// </summary>
        /// <param name="key"></param>
        private static void AddNotificationOnKey(string key)
        {
            CacheDataNotificationCallback notificationCallback = new CacheDataNotificationCallback(KeyNotificationMethod);            
            _cache.MessagingService.RegisterCacheNotification(key, notificationCallback, EventType.ItemAdded | EventType.ItemRemoved | EventType.ItemUpdated
                                            );
            Console.WriteLine("Event Register for Key:{0}",key);

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
            Console.WriteLine("Object Updated in Cache");
        }

        /// <summary>
        /// This method deletes the specified item in the cache.
        /// </summary>
        /// <param name="key"> key against which item will be deleted. </param>
        private static void DeleteItem(string key)
        {
            _cache.Remove(key);
            Console.WriteLine("Object Removed from Cache");
        }



        /// <summary>
        /// This method will be used as a callback for cache data notification.
        /// </summary>
        /// <param name="key"> key against which callback was fired. </param>
        /// <param name="cacheEventArgs"> Event arguments. </param>
        public static void CacheDataModified(string key, CacheEventArg cacheEventArgs)
        {
            Console.WriteLine("Cache data modification notification for the the item of the key : {0}", key); //To change body of generated methods, choose Tools | Templates.
        }

        /// <summary>
        /// This method will be used as a callback for cache data notification.
        /// </summary>
        /// <param name="key"> key against which callback was fired. </param>
        /// <param name="cacheEventArgs"> Event arguments. </param>
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

        /// <summary>
        /// This method will be used as a callback for custom events.
        /// </summary>
        /// <param name="notifId"> The key specified while raising the event will be received as notifId. </param>
        /// <param name="data"> The data specified while raising the event will be received sa data. </param>
        private static void CacheCustomEvent(object notifId, object data)
        {
            Console.WriteLine("The custom event has been raised");
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
                    product.ClassName = "class1";
                    product.Category = "category1";
                    break;
                case 2:
                    product.Id = id;
                    product.Name = "custom event product1";
                    product.ClassName = "custom class";
                    product.Category = "custom category";
                    break;
                default:
                    product.Id = id;
                    product.Name = "product" + id;
                    product.ClassName = "class" + id;
                    product.Category = "category" + id;
                    break;
            }
            return product;
        }
    }
}
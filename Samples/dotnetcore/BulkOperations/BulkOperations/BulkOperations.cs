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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Sample.Data;

namespace Alachisoft.NCache.Samples
{
    class BulkOperations
    {
        private static ICache _cache;

        /// <summary>
        /// Executing this method will perform all the operations of the sample
        /// </summary>
		public static void Run()
        {
            // Initialize cache 
            InitializeCache();

            // Generate multiple products that will be used in different operation in this sample
            Product[] products = CreateNewProducts();
            string[] Keys = GetKeys(products);

            // Generate hashmap of the keys and products
            IDictionary<string, CacheItem> productItems = GetCacheItemDictionary(Keys, products);

            // Adding multiple items in cache using bulk api
            AddMultipleObjectsToCache(productItems);

            // Update multiple items in cache using bulk api
            UpdateMultipleObjectsInCache(Keys, productItems);

            // Get multiple items from cache
            GetMultipleObjectsFromCache(Keys);

            // Remove the existing objects using bulk api and output removed objects
            OutRemoveMultipleObjectsFromCache(Keys);

            // Remove the existing objects using bulk api
            RemoveMultipleObjectsFromCache(Keys);

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
            _cache = NCache.Client.CacheManager.GetCache(cache);

            // Print output on console
            Console.WriteLine(string.Format("\nCache '{0}' is initialized.", cache));
        }

        /// <summary>
        /// This method adds multiple objects in the cache using bulk api
        /// </summary>
        /// <param name="keys"> Dictionary of keys and Cache Items </param>
        private static void AddMultipleObjectsToCache(IDictionary<string, CacheItem> items)
        {
            // Adding Bulk Items
            // Bulk operation returns 
            IDictionary<string, Exception> result = _cache.AddBulk(items);

            if (result.Count == 0)
            {
                // Print output on console
                Console.WriteLine("\nAll items are successfully added to cache.");
            }
            else
            {
                // Print output on console
                Console.WriteLine("\nOne or more items could not be added to the cache. Iterate hashmap for details.");
                // Iterate hashmap
                for (IEnumerator iter = result.Values.GetEnumerator(); iter.MoveNext();)
                {
                    Product product = (Product)iter;
                    PrintProductDetails(product);
                }
            }
        }

        /// <summary>
        /// This method updates multiple objects in the cache using bulk api
        /// </summary>
        /// <param name="keys"> Dictionary of keys and Cache Items </param>
        private static void UpdateMultipleObjectsInCache(string[] keys, IDictionary<string, CacheItem> items)
        {
            // Updating Bulk Items
            // Previous products have their product id, class and category changed.
            items[keys[0]].GetValue<Product>().ClassName = "ClassB";
            items[keys[1]].GetValue<Product>().ClassName = "ClassC";
            items[keys[2]].GetValue<Product>().ClassName = "ClassA";
            items[keys[3]].GetValue<Product>().ClassName = "ClassD";

            IDictionary<string, Exception> result = _cache.InsertBulk(items);

            if (result.Count == 0)
            {
                // Print output on console
                Console.WriteLine("\nAll items are successfully updated in cache.");
            }
            else
            {
                // Print output on console
                Console.WriteLine("\nOne or more items could not be added to the cache. Iterate hashmap for details.");
                // Iterate hashmap
                for (IEnumerator iter = result.Values.GetEnumerator(); iter.MoveNext();)
                {
                    Product product = (Product)iter;
                    PrintProductDetails(product);
                }
            }
        }

        /// <summary>
        /// This method reads multiple objects from the cache using bulk api
        /// </summary>
        /// <param name="keys"> List of string keys to fetch data from cache </param>
        private static void GetMultipleObjectsFromCache(string[] keys)
        {
            // Getting bulk items
            IDictionary<string, Product> items = _cache.GetBulk<Product>(keys);

            if (items.Count > 0)
            {
                // Print output on console
                Console.WriteLine("\nFollowing items are fetched from cache.");

                for (IEnumerator iter = items.Values.GetEnumerator(); iter.MoveNext();)
                {
                    Product product = (Product)iter.Current;
                    PrintProductDetails(product);
                }
            }
        }

        /// <summary>
        /// Remove multiple objects in the cache using bulk api
        /// </summary>
        /// <param name="keys"> List of string keys to be removed from cache </param>
        private static void RemoveMultipleObjectsFromCache(string[] keys)
        {
            // Remove bulk from cache and return removed objects as out parameter
            _cache.RemoveBulk(keys);

            // Print output on console
            Console.WriteLine("\nItems deleted from cache. ");
        }

        /// <summary>
        /// Remove multiple objects in the cache using bulk api and return removed objects as out parameters
        /// </summary>
        /// <param name="keys"> List of string keys to remove data from cache </param>
        private static void OutRemoveMultipleObjectsFromCache(string[] keys)
        {
            // Remove Bulk operation
            // Remove returns all the items removed from the cache in them form of Hashmap
            IDictionary<string, Product> result;

            _cache.RemoveBulk<Product>(keys, out result);

            if (result.Count == 0)
            {
                // Print output on console
                Console.WriteLine("\nNo items removed from the cache against the provided keys.");
            }
            else
            {
                // Print output on console
                Console.WriteLine("\nFollowing items have been removed from the cache:");
                // Iterate hashmap
                for (IEnumerator iter = result.Values.GetEnumerator(); iter.MoveNext();)
                {
                    Product product = (Product)iter.Current;
                    PrintProductDetails(product);
                }
            }
        }

        /// <summary>
        /// Creates list of products to be used in this sample
        /// </summary>
        /// <returns> returns list of Products </returns>
        private static Product[] CreateNewProducts()
        {
            Product[] products = new Product[4];

            products[0] = new Product() { Id = 1, Name = "Dairy Milk Cheese", ClassName = "ClassA", Category = "Edibles" };
            products[1] = new Product() { Id = 2, Name = "American Butter", ClassName = "ClassA", Category = "Edibles" };
            products[2] = new Product() { Id = 3, Name = "Walmart Delicious Cream", ClassName = "ClassA", Category = "Edibles" };
            products[3] = new Product() { Id = 4, Name = "Nestle Yogurt", ClassName = "ClassA", Category = "Edibles" };

            return products;
        }

        /// <summary>
        /// Generates list of string keys 
        /// </summary>
        /// <param name="customer"> List of products to generate keys</param>
        /// <returns> returns list of keys </returns>
        private static string[] GetKeys(Product[] products)
        {
            string[] keys = new string[products.Length];
            for (int i = 0; i < products.Length; i++)
            {
                keys[i] = string.Format("Customer:{0}", products[i].Name);
            }

            return keys;
        }

        /// <summary>
        /// Generates hashmap of Cache Items and keys 
        /// </summary>
        /// <param name="keys"> List of keys to generate hashmap</param>
        /// <param name="products"> List of products to generate hashmap</param>
        /// <returns> returns Dictionary of Cache Items and keys </returns>
        private static IDictionary<string, CacheItem> GetCacheItemDictionary(string[] keys, Product[] products)
        {
            IDictionary<string, CacheItem> items = new Dictionary<string, CacheItem>();
            CacheItem cacheItem = null;

            for (int i = 0; i < products.Length; i++)
            {
                cacheItem = new CacheItem(products[i]);
                items.Add(keys[i], cacheItem);
            }

            return items;
        }

        /// <summary>
        /// Method for printing detials of product type.
        /// </summary>
        /// <param name="product"></param>
        private static void PrintProductDetails(Product product)
        {
            Console.WriteLine("Id:       " + product.Id);
            Console.WriteLine("Name:     " + product.Name);
            Console.WriteLine("Class:    " + product.ClassName);
            Console.WriteLine("Category: " + product.Category);
            Console.WriteLine();
        }
    }
}

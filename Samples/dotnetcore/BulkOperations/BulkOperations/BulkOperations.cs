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
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the sample
    /// </summary>
	public class BulkOperations
    {
        private static Cache _cache;

        /// <summary>
        /// Executing this method will perform all the operations of the sample
        /// </summary>
        public static void Run()
        {
            // Initialize cache 
            InitializeCache();

            // Generate multiple products that will be used in different operation in this sample
            Product[] products = CreateNewProducts();
            string[] keys = GetKeys(products);

            // Adding multiple items in cache using bulk api
            AddMultipleObjectsToCache(keys, products);

            // Update multiple items in cache using bulk api
            UpdateMultipleObjectsInCache(keys, products);

            // Get multiple items from cache
            GetMultipleObjectsFromCache(keys);

            // Remove the existing objects using bulk api
            RemoveMultipleObjectsFromCache(keys);

            // Delete the existing objects using bulk api
            DeleteMultipleObjectsFromCache(keys);

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
        /// This method adds multiple objects in the cache using bulk api
        /// </summary>
        /// <param name="keys"> List of string keys that will be inserted in the cache </param>
        /// <param name="products"> products that will be inserted in the cache </param>
        private static void AddMultipleObjectsToCache(string[] keys, Product[] products)
        {
            // Adding Bulk Items
            // Bulk operation returns 
            IDictionary result = _cache.AddBulk(keys, new CacheItem[] { new CacheItem(products[0]), new CacheItem(products[1]), new CacheItem(products[2]), new CacheItem(products[3]) });

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
        /// <param name="keys"> List of string keys to update data in cache </param>
        /// <param name="products"> products that will be inserted in the cache </param>
        private static void UpdateMultipleObjectsInCache(string[] keys, Product[] products)
        {
            // Updating Bulk Items
            // Previous products have their UnitsAvailable changed.
            products[0].UnitsAvailable = 150;
            products[1].UnitsAvailable = 170;
            products[2].UnitsAvailable = 125;
            products[3].UnitsAvailable = 130;

            IDictionary result = _cache.InsertBulk(keys, new CacheItem[] { new CacheItem(products[0]), new CacheItem(products[1]), new CacheItem(products[2]), new CacheItem(products[3]) });

            if (result.Count == 0)
            {
                // Print output on console
                Console.WriteLine("\nAll items successfully added/updated in cache.");
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
            IDictionary items = _cache.GetBulk(keys);

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
        /// <param name="keys"> List of string keys to remove data from cache </param>
        private static void RemoveMultipleObjectsFromCache(string[] keys)
        {
            // Remove Bulk operation
            // Remove returns all the items removed from the cache in them form of Hashmap

            IDictionary result = _cache.RemoveBulk(keys);

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
        /// Delete multiple objects in the cache using bulk api
        /// </summary>
        /// <param name="keys"> List of string keys to be removed from cache </param>
        private static void DeleteMultipleObjectsFromCache(string[] keys)
        {
            // Delete Bulk from Cache is same as Remove
            // Delete bulk does not return anything 

            _cache.DeleteBulk(keys);

            // Print output on console
            Console.WriteLine("\nItems deleted from cache. ");
        }

        /// <summary>
        /// Creates list of products to be used in this sample
        /// </summary>
        /// <returns> returns list of Products </returns>
        private static Product[] CreateNewProducts()
        {
            Product[] products = new Product[4];

            products[0] = new Product() { Id = 1, Name = "Dairy Milk Cheese", UnitsAvailable = 200, Category = "Edibles" };
            products[1] = new Product() { Id = 2, Name = "American Butter", UnitsAvailable = 200, Category = "Edibles" };
            products[2] = new Product() { Id = 3, Name = "Walmart Delicious Cream", UnitsAvailable = 200, Category = "Edibles" };
            products[3] = new Product() { Id = 4, Name = "Nestle Yogurt", UnitsAvailable = 200, Category = "Edibles" };

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
        /// Method for printing detials of product type.
        /// </summary>
        /// <param name="product"></param>
        private static void PrintProductDetails(Product product)
        {
            Console.WriteLine("Id:       " + product.Id);
            Console.WriteLine("Name:     " + product.Name);
            Console.WriteLine("Units Available: " + product.UnitsAvailable);
            Console.WriteLine("Category: " + product.Category);
            Console.WriteLine();
        }
    }

}
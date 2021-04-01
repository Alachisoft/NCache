// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache Object Query Language sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using System;
using System.Collections;
using System.Configuration;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Sample.Data;
using System.Collections.Generic;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the object query language sample.
    /// </summary>
    public class SearchUsingSQL
    {
        private static ICache _cache;

        /// <summary>
        /// Executing this method will perform the operations fo the sample using object query language.
        /// </summary>
        public static void Run()
        {
            // Initialize Cache 
            InitializeCache();

            // Querying an item in NCache requires either NamedTags or
            // Indexes defined for objects or both.

            // Generate a new instance of Product
            IList<Product> products = GenerateProducts();

            // Insert Items in cache with Named Tags
            InsertItemsWithNamedTags(products);

            // Querying a simple cache item via named Tags
            QueryItemsUsingNamedTags();

            // Query Items with defined index
            QueryItemsUsingDefinedIndex();

            // Query Items with both named tags and predefined index
            QueryItemsUsingProjection();

            // Dispose the cache once done
            _cache.Dispose();
        }

        /// <summary>
        /// This method initializes the cache
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
        /// This method inserts items in cache with named tags.
        /// </summary>
        private static void InsertItemsWithNamedTags(IList<Product> products)
        {
            foreach(var product in products)
            {
                NamedTagsDictionary namedTagDictionary = GetNamedTagsDictionary(product);
                CacheItem cacheItem = new CacheItem(product);
                cacheItem.NamedTags = namedTagDictionary;
                _cache.Add($"Product:{product.Id}", cacheItem);
            }

            Console.WriteLine("Items added in cache with NamedTag 'Supplier'.");
        }

        /// <summary>
        /// This method queries Items from NCache using named tags.
        /// </summary>
        private static void QueryItemsUsingNamedTags()
        {
            string query = "SELECT $Value$ FROM Alachisoft.NCache.Sample.Data.Product WHERE Supplier = ?";
            QueryCommand queryCommand = new QueryCommand(query);
            // Defining Searching criteria
            queryCommand.Parameters.Add("Supplier", "Tokyo Traders");
            ICacheReader reader = _cache.SearchService.ExecuteReader(queryCommand);

            int counter = 0;
            if(reader.FieldCount > 0)
            {
                while (reader.Read())
                {
                    var product = reader.GetValue<Product>(1);
                    PrintProductDetails(product);
                    counter++;
                }
            }

            Console.WriteLine("{0} cache items fetched from cache using query on named tags.\n", counter);
        }

        /// <summary>
        /// This method queries items in the cache on which index was defined using NCache Manager.
        /// </summary>
        private static void QueryItemsUsingDefinedIndex()
        {
            //  Query can only be applied to C# Primitive data types:	
            //  and for those non primitive data types whose indexes are defined in NCache manager

            string query = "SELECT $Value$ FROM Alachisoft.NCache.Sample.Data.Product WHERE UnitPrice > ?";


            QueryCommand queryCommand = new QueryCommand(query);
            queryCommand.Parameters.Add("UnitPrice", Convert.ToDecimal(100));
            ICacheReader reader = _cache.SearchService.ExecuteReader(queryCommand);

            int counter = 0;
            if (reader.FieldCount > 0)
            {
                while (reader.Read())
                {
                    var product = reader.GetValue<Product>(1);
                    PrintProductDetails(product);
                    counter++;
                }
            }

            Console.WriteLine("{0} cache items fetched from cache using query on unit price.\n", counter);
        }

        /// <summary>
        /// This method queries items from the cache using projection attributes.
        /// </summary>
        private static void QueryItemsUsingProjection()
        {
            string query = "SELECT Name, Supplier FROM Alachisoft.NCache.Sample.Data.Product WHERE UnitPrice > ?";

            QueryCommand queryCommand = new QueryCommand(query);
            queryCommand.Parameters.Add("UnitPrice", Convert.ToDecimal(100));
            ICacheReader reader = _cache.SearchService.ExecuteReader(queryCommand);

            int counter = 0;
            if (reader.FieldCount > 0)
            {
                while (reader.Read())
                {
                    Console.WriteLine("Name:      " + reader.GetValue<string>("Name"));
                    Console.WriteLine("Supplier:  " + reader.GetValue<string>("Supplier"));
                    Console.WriteLine();
                    counter++;
                }
            }

            Console.WriteLine("{0} cache items fetched from cache using projection query on unit price.\n", counter);
        }

        /// <summary>
        /// This method generates a new instance of product with the specified key.
        /// </summary>
        /// <param name="key"> Key that will be set as productId. </param>
        /// <returns> returns a new instance of Product. </returns>
        private static IList<Product> GenerateProducts()
        {
            var productList = new List<Product>();

            productList.Add(new Product() { Id = 1, Name = "Chai", ClassName = "Electronics", Category = "Beverages", UnitPrice = 357 });
            productList.Add(new Product() { Id = 2, Name = "Chang", ClassName = "Electronics", Category = "Meat", UnitPrice = 188 });
            productList.Add(new Product() { Id = 3, Name = "Aniseed Syrup", ClassName = "Electronics", Category = "Beverages", UnitPrice = 258 });
            productList.Add(new Product() { Id = 4, Name = "IKura", ClassName = "Electronics", Category = "Produce", UnitPrice = 50 });
            productList.Add(new Product() { Id = 5, Name = "Tofu", ClassName = "Electronics", Category = "Seafood", UnitPrice = 78 });

            return productList;
        }

        private static NamedTagsDictionary GetNamedTagsDictionary(Product product)
        {
            NamedTagsDictionary nameTagDict = new NamedTagsDictionary();

            switch (product.Name)
            {
                case "Chai":
                case "Chang":
                case "Aniseed Syrup":
                    nameTagDict.Add("Supplier", "Exotic Liquids");
                    break;

                case "IKura":
                case "Tofu":
                    nameTagDict.Add("Supplier", "Tokyo Traders");
                    break;
            }

            return nameTagDict;
        }

        /// <summary>
        /// Method for printing details of product type.
        /// </summary>
        /// <param name="product"> Product whos details will be printed</param>
        internal static void PrintProductDetails(Product product)
        {
            Console.WriteLine("Id:        " + product.Id);
            Console.WriteLine("Name:      " + product.Name);
            Console.WriteLine("Class:     " + product.ClassName);
            Console.WriteLine("Category:  " + product.Category);
            Console.WriteLine("UnitPrice: " + product.UnitPrice);
            Console.WriteLine();
        }
    }

}
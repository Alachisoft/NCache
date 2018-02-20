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
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that demonstrates the usage of NamedTags in NCache.
    /// </summary>
    public class NamedTags
    {
        private static Cache _cache;

        /// <summary>
        /// Executing this method will perform the operations of the sample using Named tags.
        /// </summary>
        public static void Run()
        {
            // Initalize cache
            InitializeCache();

            // Creating named tag dictionary. 
            NamedTagsDictionary namedTagDict = new NamedTagsDictionary();
            namedTagDict.Add("Category", "Beverages");
            namedTagDict.Add("ProductName", "Coke");

            // Add Items in cache with named tags
            AddItems(namedTagDict);

            // Fetch Items from the cache
            GetItems();

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
        /// This method adds items in the cache along with namedTags.
        /// </summary>
        /// <param name="namedTagDict"> Named tags that will be added with the items. </param>
        private static void AddItems(NamedTagsDictionary namedTagDict)
        {
            _cache.Add("Order:1", new Order() { OrderID = 1, ShipName = "Andrew Ng", ShipCountry = "USA" }, namedTagDict);
            _cache.Add("Order:2", new Order() { OrderID = 2, ShipName = "Jhon Getsby", ShipCountry = "France" }, namedTagDict);

            Console.WriteLine("Items added in cache.");
        }

        /// <summary>
        /// This method fetches items from the cache using named tags.
        /// </summary>
        private static void GetItems()
        {
            string query = "SELECT Alachisoft.NCache.Sample.Data.Order WHERE this.Category = ? AND this.ProductName = ?";

            Hashtable values = new Hashtable();
            values["Category"] = "Beverages";
            values["ProductName"] = "Coke";

            ICacheReader result = _cache.ExecuteReader(query, values);

            if (!result.IsClosed)
            {
                while (result.Read())
                {
                    Console.WriteLine(result[0].ToString());
                }
            }
        }
    }
}
// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Sample.Data;
using Alachisoft.NCache.Web.Caching;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that provides the functionality of the sample
    /// </summary>
    public class CustomDependencyUsage
    {
        private static Cache _cache;
        private static OleDbConnection _connection = null;
        private static string _connectionString;
        
        /// <summary>
        /// Executing this method will perform all the operations of the sample
        /// </summary>
        public static void Run()
        {
            // Initialize cache 
            InitializeCache();

            // Initialize database connection
            InitializeDatabaseConnection();

            // Add data to cache with custom dependency
            string[] keys = AddDataIntoCache();

            // Update data in database and wait for clean interval
            UpdateUnitsInStockInDatabase(keys);

            // Get objects from cache
            GetObjectsFromCache(keys);
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
        /// This method initializes the database connection
        /// </summary>
        private static void InitializeDatabaseConnection()
        {
            _connectionString = ConfigurationManager.AppSettings["connectionString"];

            if (String.IsNullOrEmpty(_connectionString))
            {
                Console.WriteLine("The connectionString cannot be null or empty.");
                return;
            }

            _connection = new OleDbConnection(_connectionString);
            _connection.Open();

            // Print output on console
            Console.WriteLine("\nDatabase connection is initialized.");
        }

        /// <summary>
        /// This method Adds objects to cache
        /// </summary>
        /// <returns>List of keys added to cache </returns>
        private static string[] AddDataIntoCache()
        {
            List<Product> products = LoadProductsFromDatabase();
            CacheItem[] items = new CacheItem[products.Count];
            string[] keys = new String[products.Count];

            for (int i = 0; i < products.Count; i++)
            {
                keys[i] = products[i].Id.ToString();
                CacheItem item = new CacheItem(products[i]);
                item.Dependency = new CacheDependency(new Dependency(products[i].Id, _connectionString));
                items[i] = item;
            }

            _cache.AddBulk(keys, items, DSWriteOption.None, null);

            // Print output on console
            Console.WriteLine("Items are added to Cache.");

            return keys;
        }

        /// <summary>
        /// Loads objects from the data-source.
        /// </summary>
        /// <returns>List of products </returns>
        private static List<Product> LoadProductsFromDatabase()
        {
            OleDbDataReader reader = null;
            List<Product> products = new List<Product>();
            OleDbCommand cmd = _connection.CreateCommand();
            cmd.CommandText = String.Format(CultureInfo.InvariantCulture,
                "Select ProductID, ProductName, UnitsInStock" +
                " From Products where ProductID < 16");

            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Product product = new Product();

                product.Id = (int)reader["ProductID"];
                product.Name = reader["ProductName"].ToString();

                string units = reader["UnitsInStock"].ToString();
                units = (units.Equals(string.Empty)) ? "0" : units;
                product.UnitsAvailable =Convert.ToInt32(units) ;

                products.Add(product);
            }
            reader.Close();

            return products;
        }

        /// <summary> 
        /// Updates the quantity in the database 
        /// </summary>
        /// <param name="productID"></param>
        private static void UpdateUnitsInStockInDatabase(string[] keys)
        {
            int quantity = 0;

            for (int i = 0; i < keys.Length; i++)
            {
                OleDbCommand cmd = _connection.CreateCommand();

                cmd.CommandText = String.Format(CultureInfo.InvariantCulture,
                    "Update Products " +
                    "Set UnitsInStock = {0} " +
                    "Where ProductID = {1}",
                    quantity, keys[i]);

                cmd.ExecuteNonQuery();
            }

            // Print output on console
            Console.WriteLine("\nItems are modified in database.");

            // Wait for clean interval 
            Thread.Sleep(15000);
        }

        /// <summary>
        /// This method gets objects from the cache
        /// </summary>
        /// <param name="key"> String keys to get objects from cache </param>
        private static void GetObjectsFromCache(string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                Customer cachedCustomer = (Customer)_cache.Get(keys[i]);
                if (cachedCustomer == null)
                {
                    Console.WriteLine(string.Format("Item with key {0} is removed from cache ", keys[i]));
                }
            }
        }
    }
}

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
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using Alachisoft.NCache.Runtime.Dependencies;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;

namespace SQLDependency
{
    /// <summary>
    /// Class that implements sql dependency
    /// </summary>
    public class SQLServerDependencyDemo
    {
        private static string _connectionString;
        private static Cache _cache;

        /// <summary>
        /// Demonstarates the use of SQL Dependency with NCache
        /// </summary>
        public static void Run()
        {
            try
            {
                // Initialize cache
                InitializeCache();

                // Change notification previleges must be granted for SQL Server Dependency to work                
                // -- 'grant change notification to username' without quotes where username 
                // is to replaced with one being used by the db instance

                // Get SQL Server connectionString specified in appsettings    
                _connectionString = ConfigurationManager.AppSettings["conn-string"];

                if (string.IsNullOrEmpty(_connectionString))
                {
                    Console.WriteLine("Connection string is not specified in application configuration.");
                    return;
                }

                int productId = 1;

                // Fetch a sampple product from the database 
                Product product = LoadProductFromDatabase(productId);

                // Add product to the cache with SQL Dependency
                AddProductToCacheWithDependency(product);

                // Any item that is modified in the database will be invalidated in the cache.
                // Thus the item will be removed from the cache.
                // To verify this behavior uncomment the following code.

                //// Fetching cached item to verify successfull insertion
                //string cacheKey = GenerateCacheKey(product);
                //product = _cache.Get(cacheKey) as Product;

                //if (product != null)
                //{
                //    Console.WriteLine("Product fetched from cache:");
                //}
                //else
                //{
                //    Console.WriteLine("Product not found in cache");
                //}


                //Console.WriteLine("Press any key to update record in database; this will invalidate data in cache");
                //Console.ReadKey();

                //// Let's modiefy the product
                //product.UnitPrice += 10;


                //// Update Product in northwind db to trigger sql server dependency
                //UpdateProductsInDatabase(product);

                //// Verify dependency been triggered
                //product = _cache.Get(cacheKey) as Product;

                //if (product != null)
                //{
                //    Console.WriteLine("SQL Server dependency did not work as expected. " + "Check your connection string and sql command syntax.");
                //}
                //else
                //{
                //    Console.WriteLine("product object removed from cache due to sql dependency.");
                //}

                // Must dispose cache
                _cache.Dispose();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

        /// <summary>
        /// This method initializes the cache
        /// </summary>
        private static void InitializeCache()
        {
            string cache = ConfigurationManager.AppSettings["CacheID"];

            if (String.IsNullOrEmpty(cache))
            {
                Console.WriteLine("The Cache Name cannot be null or empty.");
                return;
            }

            // Initialize an instance of the cache to begin performing operations:
            _cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cache);
        }

        /// <summary>
        /// This method adds products in the cache with sql dependency.
        /// </summary>
        /// <param name="product"> Product that is to be added in the cache. </param>
        private static void AddProductToCacheWithDependency(Product product)
        {
            // Any change to the resulset of the query will cause cache to invalidate the dependent data
            string queryText = String.Format("SELECT ProductID, ProductName, QuantityPerUnit, UnitPrice FROM dbo.PRODUCTS WHERE PRODUCTID = {0}", product.Id);

            // Let's create SQL depdenency
            CacheDependency sqlServerDependency = new SqlCacheDependency(_connectionString, queryText);

            CacheItem cacheItem = new CacheItem(product);
            cacheItem.Dependency = sqlServerDependency;

            // Inserting Loaded product into cache with key: [item:1]
            string cacheKey = GenerateCacheKey(product);
            _cache.Add(cacheKey, cacheItem);
        }

        /// <summary>
        /// This method generates the cache key for the specified product.
        /// </summary>
        /// <param name="product"> Product whos cache key is to be generated. </param>
        /// <returns> Returns the cache key. </returns>
        private static string GenerateCacheKey(Product product)
        {
            string cacheKey = "Product#" + product.Id;
            return cacheKey;
        }

        /// <summary>
        /// This method fetches products from the database.
        /// </summary>
        /// <param name="productId"> Id of the product to be fetched. </param>
        /// <returns> returns the populated instance of the product. </returns>
        private static Product LoadProductFromDatabase(int productId)
        {
            string queryText = String.Format("SELECT ProductID, ProductName, QuantityPerUnit, UnitPrice FROM dbo.PRODUCTS WHERE PRODUCTID = {0}", productId);

            Product product = null;

            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader = null;

                cmd.CommandText = queryText;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection;

                sqlConnection.Open();

                try
                {
                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        // Populate product
                        product = new Product();
                        product.Id = (int)reader["ProductID"];
                        product.Name = reader["ProductName"] as String;
                        product.QuantityPerUnit = reader["QuantityPerUnit"] as String;
                        product.UnitPrice = (Decimal)reader["UnitPrice"];

                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(String.Format("An error occured while fetching product. Error {0} ", ex));
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }

            return product;
        }

        /// <summary>
        /// This method updates product in the database.
        /// </summary>
        /// <param name="product"> Product instance that is to be updated in the database. </param>
        /// <returns> Returns the count of the effected rows. </returns>
        private static int UpdateProductsInDatabase(Product product)
        {
            int rowsEffected = 0;
            string queryText = string.Format("UPDATE Products SET UnitPrice= {0} WHERE PRODUCTID = {1}", product.UnitPrice, product.Id);


            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand();

                cmd.CommandText = queryText;
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection;

                sqlConnection.Open();

                rowsEffected = cmd.ExecuteNonQuery();

            }

            return rowsEffected;
        }
    }
}

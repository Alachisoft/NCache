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
using Alachisoft.NCache.Samples.NCacheLINQBusiness;
using System.Collections;
using System.Linq;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;
using Alachisoft.NCache.Linq;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// Class that demonstrates the functionality of NCache LINQ
    /// </summary>
    class NCacheLINQ
    {
        private static Datasource _db = new Datasource();
        private static Cache _cache;
        private static string _error = "Some error has occured while executing the query" +
                                       "\nPossible reason might be that the query indexes are not defined" +
                                       "\nFor help see readme!.txt given with this sample";

        /// <summary>
        /// Executing this method will perform the operations of the sample using NCache LINQ
        /// </summary>
        public static void Run()
        {
            Console.WindowWidth = 100;

            string select;

            // Load data in cache
            if (!LoadCache())
            {
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Cache Loaded !");

            IQueryable<Product> products = new NCacheQuery<Product>(_cache);

            var result1 = from product in products
                          where product.Id > 10
                          select product;
            if (result1 != null)
            {
                PrintHeader();
                foreach (Product p in result1)
                {
                    Console.WriteLine("ProductID : " + p.Id);
                }
            }
            else
            {
                Console.WriteLine("No record found.");
            }

            var result2 = from product in products
                          where product.Category == "4"
                          select product;
            if (result2 != null)
            {
                PrintHeader();
                foreach (Product p in result2)
                {
                    Console.WriteLine("ProductID : " + p.Id);
                }
            }
            else
            {
                Console.WriteLine("No record found.");
            }

            var result3 = from product in products
                          where product.Id < 10
                          && product.UnitPrice == 1
                          select product;
            if (result3 != null)
            {
                PrintHeader();
                foreach (Product p in result3)
                {
                    Console.WriteLine("ProductID : " + p.Id);
                }
            }
            else
            {
                Console.WriteLine("No record found.");
            }
            // Dispose cache once done.
            _cache.Dispose();
        }

        /// <summary>
        /// This method initalizes the cache.
        /// </summary>
        /// <returns></returns>
        private static bool InitializeCache()
        {
            string cacheName = ConfigurationSettings.AppSettings["CacheId"].ToString();
            try
            {
                _cache = Alachisoft.NCache.Web.Caching.NCache.InitializeCache(cacheName);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured while trying to initialize Cache named: [" + cacheName + "] \n"
                                    + "Exception: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// This method populates data into the cache.
        /// </summary>
        /// <returns> Returns true when successful. </returns>
        private static bool LoadCache()
        {
            try
            {
                if (InitializeCache())
                {
                    Console.WriteLine("Loading products into " + _cache.ToString() + "...");

                    Hashtable keyVals = _db.LoadProducts();
                    IDictionaryEnumerator ide = keyVals.GetEnumerator();
                    while (ide.MoveNext())
                    {
                        _cache.Add(ide.Key.ToString(), (Product)ide.Value);
                    }
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : " + ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// This method prints the header for displaying query results.
        /// </summary>
        private static void PrintHeader()
        {
            Console.WriteLine();
            Console.WriteLine("Cache contains following records.");
            Console.WriteLine("---------------------------------");
            Console.WriteLine();
        }
    }
}

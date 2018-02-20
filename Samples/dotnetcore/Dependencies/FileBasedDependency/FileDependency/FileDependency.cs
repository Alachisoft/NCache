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
using System.Threading;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Sample.Data;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Runtime.Dependencies;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// <summary>
    /// This class implements file based dependency
    /// </summary>
	public class FileBasedDependency
	{
        private static Cache _cache;

        /// <summary>
        /// Executing this method will perform all the operations to use file based dependency
        /// </summary>
		public static void Run()
		{
			// Initialize cache
            InitializeCache();

            // Add file based dependency 
            AddFileBasedDependency();

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
        /// This method adds file based dependency 
        /// </summary>
        private static void AddFileBasedDependency()
        {
            string dependecyfile = Environment.GetEnvironmentVariable("NCHOME") + @"\Samples\dotnet\Dependencies\FileBasedDependency\DependencyFile\foobar.txt";

            // Generate a new instance of product 
            Product product = new Product { Id = 52, Name = "Filo Mix", Category = "Grains/Cereals", UnitPrice = 46 };

            // Adding item dependent on file 
            _cache.Add("Product:52", product, new FileDependency(dependecyfile), Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Normal);

            Console.WriteLine("\nItem 'Product:52' added to cache with file dependency. ");

            // To verify that the dependency is working, uncomment the following code.
            // Any change in the file will cause invalidation of cache item and thus the item will be removed from cache.
            // Following code modifies the file and then verifies the existence of item in cache.

            //// Modify file programmatically
            //ModifyDependencyFile(dependecyfile);
            
            ////... and then check for its existence
            //object item = _cache.Get("Product:52");
            //if (item == null)
            //{
            //    Console.WriteLine("Item has been removed due to file dependency.");
            //}
            //else
            //{
            //    Console.WriteLine("File based dependency did not work. Dependency file located at " + dependecyfile + " might be missing or file not changed within the given interval.");
            //}

            // Remove sample data from Cache to 
            _cache.Delete("Product:52");
        }

        /// <summary>
        /// This method modifies dependency file programmatically
        /// </summary>
        /// <param name="path"> Absolute path of dependency file </param>
        private static void ModifyDependencyFile(string path)
        {
            using (System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(path, true))
            {
                streamWriter.WriteLine(string.Format("\n{0}\tFile is modifed. ", DateTime.Now));
            }

            Console.WriteLine(string.Format("File '{0}' is modified programmatically.", path));
        }
	}
}
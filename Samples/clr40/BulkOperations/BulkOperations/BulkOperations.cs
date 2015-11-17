using System;
using System.Collections;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;

// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache Bulk Operations sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================



namespace BulkOperations
{
	public class BulkOperations
	{
		public static void Main(string[] args)
		{
			try
			{
				Cache cache;
				cache = NCache.InitializeCache("mypartitionedcache");
				cache.Clear();

                string[] keysToAdd = new string[] { "Product:Cheese", "Product:Butter", "Product:Cream", "Product:Yogurt" };

				Product[] products = new Product[4];

				products[0] = new Product(1, "Dairy Milk Cheese", "ClassA", 1);
				products[1] = new Product(2, "American Butter", "ClassA", 1);
				products[2] = new Product(3, "Walmart Delicious Cream", "ClassA", 2);
                products[3] = new Product(4, "Nestle Yogurt", "ClassA", 2);

				//Adding Bulk Items
				//Bulk operation returns 
                IDictionary result = cache.AddBulk(keysToAdd, new CacheItem[] { new CacheItem(products[0]), new CacheItem(products[1]), new CacheItem(products[2]), new CacheItem(products[3]) });

				if (result.Count == 0)
				{
					Console.WriteLine("All items are successfully added to cache.");
				}
				else
				{
					Console.WriteLine("One or more items could not be added to the cache. Iterate hashmap for details.");
					//This is how to iterate the hashmap
					for (IEnumerator iter = result.Values.GetEnumerator(); iter.MoveNext();)
					{
					    Product product = (Product) iter;
						PrintProductDetails(product);
					}
				}

				//Getting bulk items
				
                IDictionary items = cache.GetBulk(keysToAdd);

				if (items.Count > 0)
				{
					for (IEnumerator iter = items.Values.GetEnumerator(); iter.MoveNext();)
					{
						Product product = (Product) iter.Current;
						PrintProductDetails(product);
					}
				}

				//Updating Bulk Items
				//Previous products have their product id, class and category changed.
				products[0].ClassName = "ClassB";
				products[1].ClassName = "ClassC";
				products[2].ClassName = "ClassA";
				products[3].ClassName = "ClassD";

                result = cache.InsertBulk(keysToAdd, new CacheItem[] { new CacheItem(products[0]), new CacheItem(products[1]), new CacheItem(products[2]), new CacheItem(products[3]) });
                
				if (result.Count == 0)
				{
					Console.WriteLine("All items successfully added/updated in cache.");
				}
				else
				{
					Console.WriteLine("One or more items could not be added to the cache. Iterate hashmap for details.");
					//This is how to iterate the hashmap
					for (IEnumerator iter = result.Values.GetEnumerator(); iter.MoveNext();)
					{
						Product product = (Product) iter;
						PrintProductDetails(product);
					}
				}

                items = cache.GetBulk(keysToAdd);

                if (items.Count > 0)
				{
					for (IEnumerator iter = items.Values.GetEnumerator(); iter.MoveNext();)
					{
						Product product = (Product) iter.Current;
						PrintProductDetails(product);
					}
				}

				//Remove Bulk operation
				//Remove returns all the items removed from the cache in them form of Hashmap

                result = cache.RemoveBulk(keysToAdd);

				if (result.Count == 0)
				{
					Console.WriteLine("No items removed from the cache against the provided keys.");
				}
				else
				{
					Console.WriteLine("Following products have been removed from the cache:");
					//This is how to iterate the hashmap
					for (IEnumerator iter = result.Values.GetEnumerator(); iter.MoveNext();)
					{
						Product product = (Product) iter.Current;
						PrintProductDetails(product);
					}
				}

				//Delete Bulk from Cache is same as Remove
				//Delete bulk does not return anything 
                
                cache.DeleteBulk(keysToAdd);

                Console.WriteLine("Cache contains " + cache.Count + " items.");

				//Must dispose cache 
				cache.Dispose();

				Environment.Exit(0);

	}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Environment.Exit(0);
			}
}
        /// <summary>
        /// Method for printing detials of product type.
        /// </summary>
        /// <param name="product"></param>
		private static void PrintProductDetails(Product product)
		{
			Console.WriteLine("Id:       " + product.ProductID);
			Console.WriteLine("Name:     " + product.Name);
			Console.WriteLine("Class:    " + product.ClassName);
			Console.WriteLine("Category: " + product.Category);
			Console.WriteLine();
		}
	}

}
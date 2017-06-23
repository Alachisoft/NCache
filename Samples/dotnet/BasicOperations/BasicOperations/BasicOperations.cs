// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache Basic Operations sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================


using System;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;

namespace BasicOperations
{
	public class BasicOperations
	{
		public static void Main(string[] args)
		{
			try
			{
				//Initialize cache via 'initializeCache' using 'Cache Name'
				//to be initialized. 
				Cache cache = NCache.InitializeCache("mypartitionedcache");

				//Another method to add item(s) to cache is via CacheItem  object
				Customer customer = new Customer();
				customer.Name = "David Johnes";
				customer.Age = 23;
				customer.Gender = "Male";
				customer.ContactNo = "12345-6789";
				customer.Address = "Silicon Valley, Santa Clara, California";

				DateTime calendar = new DateTime();
				calendar.AddMinutes(1);

				//Adding item with an absolute expiration of 1 minute
				cache.Add("Customer:DavidJohnes", customer, calendar, Cache.NoSlidingExpiration, CacheItemPriority.Normal);
				Customer cachedCustomer = (Customer) cache.Get("Customer:DavidJohnes");
                
                printCustomerDetails(cachedCustomer);

				//updating item with a sliding expiration of 30 seconds
				customer.Age = 50;
				cache.Insert("Customer:DavidJohnes", customer, Cache.NoAbsoluteExpiration, new TimeSpan(0, 0, 30), CacheItemPriority.Normal);

				//get customer from the cache
				cachedCustomer = (Customer) cache.Get("Customer:DavidJohnes");
                printCustomerDetails(cachedCustomer);
              

				//Total item count in the cache
				long count = cache.Count;
                Console.WriteLine("Cache Count: " +count);

				//remove the existing customer
				cache.Remove("Customer:DavidJohnes");

				//try to get the customer again, getting non-existing items returns null
				cachedCustomer = (Customer) cache.Get("Customer:DavidJohnes");
                printCustomerDetails(cachedCustomer);

				//get count again, item count should be 0
				count = cache.Count;
                Console.WriteLine("Cache Count: " + count);

				//Dispose the cache once done
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
        /// Method for printing detials of customer type.
        /// </summary>
        /// <param name="customer"></param>
		private static void printCustomerDetails(Customer customer)
        {
            if (customer == null) return;
            
            Console.WriteLine();
			Console.WriteLine("Customer Details are as follows: ");
			Console.WriteLine("Name: " + customer.Name);
			Console.WriteLine("Age: " + customer.Age);
			Console.WriteLine("Gender: " + customer.Gender);
			Console.WriteLine("Contact No: " + customer.ContactNo);
			Console.WriteLine("Address: " + customer.Address);
			Console.WriteLine();
		}
	}
}
using System;

// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache ItemLocking sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================
using Alachisoft.NCache.Web.Caching;
using Alachisoft.NCache.Sample.Data;

namespace ItemLocking
{
	public class ItemLocking
	{

		public static void Main(string[] args)
		{

			try
			{
				//Initialize cache            
				Cache cache;
				cache = NCache.InitializeCache("mypartitionedcache");
				cache.Clear();

				//Locking prevents multiple clients from updating the same data simultaneously
				//and also provides the data consistency.

				//Adding an item the cache
				Customer customer = new Customer();
				customer.Name = "Kirsten Goli";
				customer.Age = 40;
				customer.Address = "45-A West Boulevard, Cartago, Costa Rica";
				customer.Gender = "Female";
				customer.ContactNo = "52566-1779";

				cache.Add("Customer:KirstenGoli", customer);

				//Get     
				TimeSpan timeSpan = new TimeSpan(0,0,0,20);
				LockHandle lockHandle = new LockHandle();
                Customer getCustomer = (Customer) cache.Get("Customer:KirstenGoli", timeSpan, ref lockHandle, true);

				PrintCustomerDetails(getCustomer);

				Console.WriteLine("Lock acquired on " + lockHandle.LockId);

				//Lock item in cache
				bool isLocked = cache.Lock("Customer:KirstenGoli", timeSpan, out lockHandle);

				if (!isLocked)
				{
					Console.WriteLine("Lock acquired on " + lockHandle.LockId);
				}

				//Unlock item in cache
				cache.Unlock("Customer:KirstenGoli");

				//Unlock via lockhandle
				cache.Unlock("Customer:KirstenGoli", lockHandle);

				//Must dispose cache
				cache.Dispose();

				Environment.Exit(0);

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Environment.Exit(0);
			}
		}

        /// <summary>
        /// Method for printing customer type details.
        /// </summary>
        /// <param name="customer"></param>
		public static void PrintCustomerDetails(Customer customer)
		{
			Console.WriteLine();
			Console.WriteLine("Customer Details are as follows: ");
			Console.WriteLine("Name: " + customer.Name);
			Console.WriteLine("Age: " + customer.Age);
			Console.WriteLine("Gender: " + customer.Gender);
			Console.WriteLine("Contact No: " + customer.ContactNo);
			Console.WriteLine("Address: " + customer.Address);
		}
	}

}
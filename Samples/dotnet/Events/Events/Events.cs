// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// NCache Events sample
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================


using System;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Sample.Data;
using Alachisoft.NCache.Web.Caching;

namespace Events
{
	public class Events
	{

		public static void Main(string[] args)
		{
			try
			{
				//Initialize cache
				Cache cache = NCache.InitializeCache("mypartitionedcache");
                
                Customer customer1 = new Customer();
                customer1.Name = "john viking";
                customer1.Address = "10 downing streat";
			    customer1.ContactNo = "25-555-265";
			    customer1.Gender = "male";
			    customer1.Age = 59;
                cache.Add("Item:1", customer1);

				//Key based notifications allows item notifications for particular
				//items in cache
				
                CacheDataNotificationCallback myMethod = new CacheDataNotificationCallback(KeyNotificationMethod);
                cache.RegisterCacheNotification("Item:1", myMethod, EventType.ItemAdded | EventType.ItemRemoved | EventType.ItemUpdated);


                //value for customer changed for notifications
			    customer1.name = "sophie silver";
                //See output

				cache.Insert("Item:1", customer1);

				//See output

				cache.Delete("Item:1");

                Console.WriteLine();

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

        public static void CacheDataModified(string @string, CacheEventArg cacheEventArgs)
        {
            Console.WriteLine("Cache data modification notification for the the item of the key : {0}", @string); //To change body of generated methods, choose Tools | Templates.
        }

        private static void KeyNotificationMethod(string key, CacheEventArg cacheEventArgs)
        {
            switch (cacheEventArgs.EventType)
            {
                case EventType.ItemAdded:
                    Console.WriteLine("Key: " + key + " is added to the cache");
                    break;
                case EventType.ItemRemoved:
                    Console.WriteLine("Key: " + key + " is removed from the cache");
                    break;
                case EventType.ItemUpdated:
                    Console.WriteLine("Key: " + key + " is updated in the cache");
                    break;
            }
        }

        private static void CacheCustomEvent(object notifId, object data)
        {
            Console.WriteLine("The custom event has been raised");
        } 
	}

}
// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using Alachisoft.NCache.Client;
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Samples
{
    class Publisher
    {
        private static ICache _cache = null;
        private ITopic _topic;

        public static void Run()
        {
            try
            {
                Console.WriteLine("Starting 2 Publishers... Publishers will stop after publishing 30 messages each.");

                new Thread(() => RunPublisher<ElectronicsOrder>("ElectronicsOrders")).Start();
                new Thread(() => RunPublisher<GarmentsOrder>("GarmentsOrders")).Start();

                Console.WriteLine("Publishers Started.");


                Console.WriteLine("Press any key to delete the topics");
                Console.ReadLine();
                //Deletes the topic.
                DeleteTopic("ElectronicsOrders");
                DeleteTopic("GarmentsOrders");

                Console.WriteLine("TOPICs Deleted.");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void RunPublisher<T>(string topicName) where T : Order, new()
        {
            InitializeCache();

            var publisher = new Publisher();
            publisher.PublishMessages<T>(topicName);
        }

        private void PublishMessages<T>(string topicName) where T : Order, new()
        {
            _topic=_cache.MessagingService.GetTopic(topicName);

            if (_topic == null)
                _topic = _cache.MessagingService.CreateTopic(topicName);

            //Generate 30 messages at the interval of 5 seconds.
            for(int i=0; i< 30; i++)
            {
                T order = Order.GenerateOrder<T>();

                // Publishes the message with expiry.
                Message message = new Message(order, new TimeSpan(0, 0, 15));
                _topic.Publish(message, DeliveryOption.All, true);
                Console.WriteLine($"Message for {typeof(T).Name} OrderId: {order.OrderId} generated ");

                Thread.Sleep(5 * 1000);//Sleep for 5 seconds.
            }
        }

        public static void InitializeCache()
        {
            if (_cache != null)
                return;

            string cache = ConfigurationManager.AppSettings["CacheID"];

            if (String.IsNullOrEmpty(cache))
            {
                Console.WriteLine("The CacheID cannot be null or empty.");
                return;
            }

            // Initialize an instance of the cache to begin performing operations:
            _cache = CacheManager.GetCache(cache);

            // Print output on console
            Console.WriteLine(string.Format("\nCache '{0}' is initialized.", cache));
        }

        /// <summary>
        /// This method deletes the specified 'Topic' in the argumnet.
        /// </summary>
        /// <param name="topicName">Name of the topic to be deleted.</param>
        public static void DeleteTopic(string topicName)
        {
            if (topicName == null) throw new ArgumentNullException("topicName");

            // Deleting the topic.
            _cache.MessagingService.DeleteTopic(topicName);
        }

    }
}

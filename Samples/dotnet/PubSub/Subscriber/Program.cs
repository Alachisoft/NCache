// ===============================================================================
// Alachisoft (R) NCache Sample Code.
// ===============================================================================
// Copyright © Alachisoft.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
// ===============================================================================

using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Configuration;

namespace Alachisoft.NCache.Samples
{
    /// ******************************************************************************
    /// <summary>
    /// A sample program that demonstrates how to use the messaging api in NCache.
    /// 
    /// Requirements:
    ///     1. A running NCache cache
    ///     2. Connection attributes in app.config
    /// </summary>
    /// 
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string cacheId = ConfigurationManager.AppSettings["CacheId"];
                string topicName = "mytopic";

                // Creates a subscriber's instance.
                using (Subscriber subscriber = new Subscriber())
                {
                    // Subscribes on it, using the provided cache-Id and the topic-name.
                    subscriber.Subscribe(cacheId, topicName, MessageReceivedCallback);                    
                    subscriber.OnTopicDeleted = TopicDeletedCallback;

                    Console.WriteLine("Subscriber Started. Press enter to exit.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// This method will get invoked if a message is recieved by the subscriber.
        /// </summary>
        static void MessageReceivedCallback(object sender, MessageEventArgs args)
        {
            Console.Write("Message Recieved:");
            Console.WriteLine(args.Message.Payload);
        }

        /// <summary>
        /// This method will get invoked if the topic of the subscriber gets deleted.
        /// </summary>
        static void TopicDeletedCallback(object sender, TopicDeleteEventArgs args)
        {
            Console.WriteLine("Topic '{0}' deleted.", args.TopicName);
        }
    }
}

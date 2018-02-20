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
                int messagesCount = 10;

                // Creates a publisher's instance.
                using (Publisher publisher = new Publisher())
                {
                    // Starts the publisher on the topic.
                    publisher.Start(cacheId, topicName);
                    publisher.MessageDeliveryFailure += MessageDeliveryFailure;

                    Console.WriteLine("Publisher Started...");
                    for (int i = 1; i <= messagesCount; i++)
                    {
                        string payload = string.Format("Message {0}", i);
                      
                        if (i % 2 == 0)
                        {
                            // Publishes the message with expiry.
                            publisher.Publish(payload, TimeSpan.FromMinutes(5));
                            Console.WriteLine("Message published with the expiry of 5 minutes.");
                        }
                        else
                        {
                            // Publishes the message.
                            publisher.Publish(payload);
                            Console.WriteLine("Message published without expiry.");
                        }
                    }

                    Console.WriteLine("Press enter to continue...");
                    Console.ReadLine();

                    //Deletes the topic.
                    publisher.DeleteTopic(topicName);
                    Console.WriteLine("Topic Deleted.");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// This method will be invoked in the case of any failure in a message's delivery.
        /// </summary>
        static void MessageDeliveryFailure(object sender, MessageFailedEventArgs args)
        {
            Console.WriteLine("Message not delivered. Reason: {0}", args.MessgeFailureReason);
        }
    }
}

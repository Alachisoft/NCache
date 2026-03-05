using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common
{
    public class ClientCounters
    {
        private static string categoryNameNCache = "NCache Client";
        public static System.Diagnostics.CounterCreationData[] GetCounterCreationData()
        {
#if NETCORE
            System.Diagnostics.CounterCreationDataCollection counterCreationDataColl;
            try
            {
              counterCreationDataColl = new System.Diagnostics.CounterCreationDataCollection();
            }
            catch(Exception e)
            {
                throw e;
            }
            
#endif
            System.Diagnostics.CounterCreationData[] counterCreationData = new System.Diagnostics.CounterCreationData[]
            {
            new System.Diagnostics.CounterCreationData("Fetches/sec", "Number of Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Additions/sec", "Number of Add operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Updates/sec", "Number of Insert operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Deletes/sec", "Number of Remove operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Read Operations/sec", "Number of Read operations per second", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Write Operations/sec", "Number of Write operations per second", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average us/fetch", "Average time in microseconds (us), taken to complete one fetch operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/fetch base", "Base counter for average microseconds (us)/fetch", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/add", "Average time in microseconds (us), taken to complete one add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/add base", "Base counter for average microseconds (us)/add", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/insert", "Average time in microseconds, taken to complete one insert operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/insert base", "Base counter for average microseconds (us) F/insert", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/remove", "Average time in microseconds (us), taken to complete one remove operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/remove base", "Base counter for average microseconds (us)/remove", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Request queue size", "Total number of requests from all clients on a single machine waiting for response from cache server", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Compression/sec", "Rate of compression/decompression i.e. how many items are compressed/decompression during one second interval.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average Item Size", "Average size of the item added to/fetched from the cache by the client. Average size is calculated before compression/after decompression is applied.", System.Diagnostics.PerformanceCounterType.AverageCount64),
            new System.Diagnostics.CounterCreationData("Average Item Size base", "Base counter for average size of the item added to the cache by the client. Average size is calculated before compression is applied.", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/event", "Average time in microseconds (us), taken in single event processing on the client.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/event base", " Average time in microseconds (us), taken in single event proccesing on the clients.", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Events Processed/sec", "Number of events processed per sec on client.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Events Triggered/sec", "Number of events triggered and received by client per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average us/compression", "Average time in microseconds (us), taken to compress one object.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/compression base", "Base counter for Average microseconds (us)/compression", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/decompression", "Average time in microseconds (us), taken to decompress one object.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/decompression base", "Base counter for Average microseconds (us)/decompression", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/encryption", "Average time in microseconds (us), taken to encrypt one object.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/encryption base", "Base counter for Average microseconds (us)/encryption", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/decryption", "Average time in microseconds (us), taken to decrypt one object.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/decryption base", "Base counter for Average microseconds (us)/decryption", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average Compressed Item Size", "Average size in bytes, item size after compression on add or item size before decompression on fetch.", System.Diagnostics.PerformanceCounterType.AverageCount64),
            new System.Diagnostics.CounterCreationData("Average Compressed Item Size base", "Base counter for Average Compressed Item Size.", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/serialization", "Average time in microseconds (us), taken to serialize one object.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/serialization base", "Base counter for Average microseconds (us)/serialization.", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/deserialization", "Average time in microseconds (us), taken to deserialize one object.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/deserialization base", "Base counter for Average microseconds (us)/deserialization.", System.Diagnostics.PerformanceCounterType.AverageBase),
            
            //Bulk Counters

            new System.Diagnostics.CounterCreationData("Average us/addbulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/addbulk base", "Base counter for Average microseconds (us)/addbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/fetchbulk", "Average time in microseconds (us), taken to complete bulk get operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/fetchbulk base", "Base counter for Average microseconds (us)/fetchbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/insertbulk", "Average time in microseconds (us), taken to complete bulk insert operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/insertbulk base", "Base counter for Average microseconds (us)/insertbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/removebulk", "Average time in microseconds (us), taken to complete bulk remove operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/removebulk base", "Base counter for Average microseconds (us)/removebulk", System.Diagnostics.PerformanceCounterType.AverageBase),

        // Poll Counters
            new System.Diagnostics.CounterCreationData("# of Sync Poll Requests", "Number of Sync Poll requests sent to the server.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("# of Last Sync Poll Updates", "Number of Updates resulted in last Sync Poll.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("# of Last Sync Poll Removes", "Number of Removes resulted in last Sync Poll.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData(CounterNames.AvgPublishMessage, "Average time in microseconds, taken to complete publish messages operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData(CounterNames.AvgPublishMessageBase, "Base counter for Average us/publish messages", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData(CounterNames.MessagePublishPerSec, "Number of messages published per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData(CounterNames.MessageDeliveryPerSec, "Number of messages delivered to subsribers per second.", System.Diagnostics.PerformanceCounterType.SampleCounter)
            };
#if NETCORE
            foreach (var count in counterCreationData)
            {
                counterCreationDataColl.Add(count);
            }

            if (!System.Diagnostics.PerformanceCounterCategory.Exists(categoryNameNCache))
                System.Diagnostics.PerformanceCounterCategory.Create(categoryNameNCache, "Visit Documentation", System.Diagnostics.PerformanceCounterCategoryType.MultiInstance, counterCreationDataColl);


#endif
            return counterCreationData;

        }

#if NETCORE
        public static Alachisoft.NCache.Common.Caching.Statistics.CustomCounters.CounterCreationData[] GetCustomCounterCreationData()
        {
            return new CounterCreationData[]
            {
            new CounterCreationData("Fetches/sec", "Number of Get operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Additions/sec", "Number of Add operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Updates/sec", "Number of Insert operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Deletes/sec", "Number of Remove operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Read Operations/sec", "Number of Read operations per second", CounterType.SampleCounter),
            new CounterCreationData("Write Operations/sec", "Number of Write operations per second", CounterType.SampleCounter),
            new CounterCreationData("Average us/fetch", "Average time in microseconds (us), taken to complete one fetch operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/fetch base", "Base counter for average microseconds (us)/fetch", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/add", "Average time in microseconds (us), taken to complete one add operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/add base", "Base counter for average microseconds (us)/add", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/insert", "Average time in microseconds, taken to complete one insert operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/insert base", "Base counter for average microseconds (us) F/insert", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/remove", "Average time in microseconds (us), taken to complete one remove operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/remove base", "Base counter for average microseconds (us)/remove", CounterType.AverageCounter, true),
            new CounterCreationData("Request queue size", "Total number of requests from all clients on a single machine waiting for response from cache server", CounterType.NumberOfItemCounter),
            new CounterCreationData("Compression/sec", "Rate of compression/decompression i.e. how many items are compressed/decompression during one second interval.", CounterType.SampleCounter),
            new CounterCreationData("Average Item Size", "Average size of the item added to/fetched from the cache by the client. Average size is calculated before compression/after decompression is applied.", CounterType.AverageCounter),
            new CounterCreationData("Average Item Size base", "Base counter for average size of the item added to the cache by the client. Average size is calculated before compression is applied.", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/event", "Average time in microseconds (us), taken in single event processing on the client.", CounterType.AverageCounter),
            new CounterCreationData("Average us/event base", " Average time in microseconds (us), taken in single event proccesing on the clients.", CounterType.AverageCounter, true),
            new CounterCreationData("Events Processed/sec", "Number of events processed per sec on client.", CounterType.SampleCounter),
            new CounterCreationData("Events Triggered/sec", "Number of events triggered and received by client per second.", CounterType.SampleCounter),
            new CounterCreationData("Average us/compression", "Average time in microseconds (us), taken to compress one object.", CounterType.AverageCounter),
            new CounterCreationData("Average us/compression base", "Base counter for Average microseconds (us)/compression", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/decompression", "Average time in microseconds (us), taken to decompress one object.", CounterType.AverageCounter),
            new CounterCreationData("Average us/decompression base", "Base counter for Average microseconds (us)/decompression", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/encryption", "Average time in microseconds (us), taken to encrypt one object.", CounterType.AverageCounter),
            new CounterCreationData("Average us/encryption base", "Base counter for Average microseconds (us)/encryption", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/decryption", "Average time in microseconds (us), taken to decrypt one object.", CounterType.AverageCounter),
            new CounterCreationData("Average us/decryption base", "Base counter for Average microseconds (us)/decryption", CounterType.AverageCounter, true),
            new CounterCreationData("Average Compressed Item Size", "Average size in bytes, item size after compression on add or item size before decompression on fetch.", CounterType.AverageCounter),
            new CounterCreationData("Average Compressed Item Size base", "Base counter for Average Compressed Item Size.", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/serialization", "Average time in microseconds (us), taken to serialize one object.", CounterType.AverageCounter),
            new CounterCreationData("Average us/serialization base", "Base counter for Average microseconds (us)/serialization.", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/deserialization", "Average time in microseconds (us), taken to deserialize one object.", CounterType.AverageCounter),
            new CounterCreationData("Average us/deserialization base", "Base counter for Average microseconds (us)/deserialization.", CounterType.AverageCounter, true),
            
            //Bulk Counters

            new CounterCreationData("Average us/addbulk", "Average time in microseconds (us), taken to complete bulk add operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/addbulk base", "Base counter for Average microseconds (us)/addbulk", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/fetchbulk", "Average time in microseconds (us), taken to complete bulk get operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/fetchbulk base", "Base counter for Average microseconds (us)/fetchbulk", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/insertbulk", "Average time in microseconds (us), taken to complete bulk insert operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/insertbulk base", "Base counter for Average microseconds (us)/insertbulk", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/removebulk", "Average time in microseconds (us), taken to complete bulk remove operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/removebulk base", "Base counter for Average microseconds (us)/removebulk", CounterType.AverageCounter, true),

        // Poll Counters
            new CounterCreationData("# of Sync Poll Requests", "Number of Sync Poll requests sent to the server.", CounterType.NumberOfItemCounter),
            new CounterCreationData("# of Last Sync Poll Updates", "Number of Updates resulted in last Sync Poll.", CounterType.NumberOfItemCounter),
            new CounterCreationData("# of Last Sync Poll Removes", "Number of Removes resulted in last Sync Poll.", CounterType.NumberOfItemCounter),
            new CounterCreationData(CounterNames.AvgPublishMessage, "Average time in microseconds, taken to complete publish messages operation.", CounterType.AverageCounter),
            new CounterCreationData(CounterNames.AvgPublishMessageBase, "Base counter for Average us/publish messages", CounterType.AverageCounter, true),
            new CounterCreationData(CounterNames.MessagePublishPerSec, "Number of messages published per second.", CounterType.SampleCounter),
            new CounterCreationData(CounterNames.MessageDeliveryPerSec, "Number of messages delivered to subsribers per second.", CounterType.SampleCounter)
            };
        } 
#endif
    }
}

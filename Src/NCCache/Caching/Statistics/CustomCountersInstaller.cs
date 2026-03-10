using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.Collections;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Caching.Statistics
{
    internal class CustomCountersInstaller: ICustomCountersInstaller
    {
        public CounterCreationData[] CounterData { get; set; }

        public CustomCountersInstaller()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            CounterData = new CounterCreationData[]
            {
                new CounterCreationData("# Clients", "Number of connected clients to an instance of cache.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Count", "Number of items in the cache.", CounterType.NumberOfItemCounter),
            new CounterCreationData("CacheLastAccessCount", "Number of items which are older then the access interval specified in the service config file.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Fetches/sec", "Number of Get operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Additions/sec", "Number of Add operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Updates/sec", "Number of Insert operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Deletes/sec", "Number of Remove operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Average us/fetch", "Average time in microseconds (us), taken to complete one fetch operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/fetch base", "Base counter for average microseconds(us)/fetch", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/add", "Average time in microseconds (us), taken to complete one add operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/add base", "Base counter for average microseconds (us)sec/add", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/insert", "Average time in microseconds (us), taken to complete one insert operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/insert base", "Base counter for average microseconds (us)/insert", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/remove", "Average time in microseconds (us), taken to complete one remove operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/remove base", "Base counter for average microseconds (us)/remove", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/cache operation", "Average time in microseconds (us), taken to complete one cache-operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/cache operation base", "Base counter for average microseconds (us) /cache-operation", CounterType.AverageCounter, true),
            new CounterCreationData("Expirations/sec", "Number of items being expired currently per second", CounterType.SampleCounter),
            new CounterCreationData("Evictions/sec", "Number of items evicted per second.", CounterType.SampleCounter),
            new CounterCreationData("State Transfer/sec", "Number of items this node is either reading from other nodes or sending to other nodes during a state transfer mode.", CounterType.SampleCounter),
            new CounterCreationData("Mirror queue size", "Number of items in the Mirror queue.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Sliding Index queue size", "Number of items in the Sliding-Index queue.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Hits/sec", "Number of successful Get operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Misses/sec", "Number of failed Get operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Hits ratio/sec (%)", "Ratio of number of successful Get operations per second and total number of Get operations per second ", CounterType.SampleCounter),
            new CounterCreationData("Hits ratio/sec base", "Base counter for Hits ratio/sec", CounterType.SampleCounter),
            //Moiz: perfmon description task 29-10-13
            //previous description (till 4.1 sp3) for DispatchEnter,TcpDown,Clustered opsent,clusters oprecv, reponse sent (Number of clustered operations sent to other nodes in cluster per second.)
            new CounterCreationData("Data balance/sec", "Number of items this node is either reading from other nodes or sending to other nodes during a Data Load Balancing mode.", CounterType.SampleCounter),
            new CounterCreationData("Cluster ops/sec", "Number of clustered operations performed per second.", CounterType.SampleCounter),
            new CounterCreationData("DispatchEnter/sec", "", CounterType.SampleCounter),
            new CounterCreationData("TcpdownEnter/sec", "", CounterType.SampleCounter),
            new CounterCreationData("Clustered opssent/sec", "Number of clustered operations sent to other nodes in cluster per second.", CounterType.SampleCounter),
            new CounterCreationData("Clustered opsrecv/sec", "Number of clustered operations received from other nodes in cluster per second.", CounterType.SampleCounter),
            new CounterCreationData("Response sent/sec", "Number of responses sent to other nodes in cluster per second.", CounterType.SampleCounter),
            new CounterCreationData("Bytes sent/sec", "Number of bytes sent per second to other nodes of the cluster.", CounterType.SampleCounter),
            new CounterCreationData("Bytes received/sec", "Number of bytes received per second from other nodes of the cluster.", CounterType.SampleCounter),
            new CounterCreationData("Requests/sec", "Number of requests received (meaning cache commands like add, get, insert, remove etc.) from all clients to this cache server.", CounterType.RateOfCounter),
            new CounterCreationData("Responses/sec", "Number of responses sent (meaning cache response for commands like add, get, insert, remove etc.) to all clients by this cache server.", CounterType.RateOfCounter),
            new CounterCreationData("Client Requests/sec", "Number of requests sent by all clients to the cache server.", CounterType.RateOfCounter),
            new CounterCreationData("Client Responses/sec", "Number of responses received by all clients from the cache server.", CounterType.RateOfCounter),
            new CounterCreationData("Client bytes sent/sec", "Bytes being sent from cache server to all its clients.", CounterType.RateOfCounter),
            new CounterCreationData("Client bytes received/sec", "Bytes being received by cache server from all its clients.", CounterType.RateOfCounter),
            new CounterCreationData("TcpUpQueueCount", "Number of items in TCP up-queue.", CounterType.NumberOfItemCounter),
            new CounterCreationData("TcpDownQueueCount", "Number of items in TCP down-queue.", CounterType.NumberOfItemCounter),
            new CounterCreationData("BcastQueueCount", "Number of items in BCast queue waiting to be processed on sequence.", CounterType.NumberOfItemCounter),
            new CounterCreationData("McastQueueCount", "Number of items in MCast queue waiting to be processed on sequence.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Socket send time (msec)", "Time in milli seconds it took for the last message to be sent over the socket.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Socket send size (bytes)", "How much data was sent in the last message.", CounterType.NumberOfItemCounter),
            new CounterCreationData("General Notifications Queue Size", "Number of general notification events in queue.", CounterType.NumberOfItemCounter),
            new CounterCreationData("NaglingMsgCount", "Time in milli seconds for which a sequenced messages waits before it is processed.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Socket recv time (msec)", "Time in milli seconds it took to receive the last message.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Socket recv size (bytes)", "How much data was received in the last message.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Read-thru/sec", "Number of Read-thru operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Write-thru/sec", "Number of Write-thru operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Average us/Write-thru", "Average time in microseconds, taken to complete write thru operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/Write-thru base", "Base counter for average ?sec/Write-thru", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/Write-behind", "Average time in microseconds, taken to complete write behind operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/Write-behind base", "Base counter for average ?sec/Write-behind", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/Read-thru", "Average time in microseconds, taken to complete Read thru operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/Read-thru base", "Base counter for average ?sec/Read-thru", CounterType.AverageCounter, true),

            new CounterCreationData("Response Queue Count", "Number of items in response queue.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Event Queue Count", "Number of items in event queue.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Response Queue Size", "Size of response queue specified in bytes.", CounterType.NumberOfItemCounter),

            new CounterCreationData("Cache Size", "Size of the cache in bytes, including cache store meta info and all other indices.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Query Index Size", "Size of query indices in bytes, defined on the cache.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Expiration Index Size", "Size of expiration in bytes, indices defined on the cache.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Eviction Index Size", "Size of eviction indices in bytes, define on the cache.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Group Index Size", "Size of group and sub group indices in bytes defined on this cache.", CounterType.NumberOfItemCounter),


            //request logging counters
            new CounterCreationData("Logged Request Count", "Total number of currently logged requests.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Requests Logged/sec", "Average number of requests logged in a second.", CounterType.SampleCounter),
            new CounterCreationData("Request Log Ledger Size", "Total in-memory size of the log ledger, which stores logged requests.", CounterType.NumberOfItemCounter),
            
            //MapReduce Counters
            new CounterCreationData("MapReduce Running Tasks", "Number of MapReduce tasks running.", CounterType.NumberOfItemCounter),
            new CounterCreationData("MapReduce Waiting Tasks", "Number of MapReduce tasks waiting to be executed.", CounterType.NumberOfItemCounter),
            new CounterCreationData("MapReduce Mapped/sec", "Number of records Mapped per sec.", CounterType.RateOfCounter),
            new CounterCreationData("MapReduce Reduced/sec", "Number of records Reduced per sec.", CounterType.RateOfCounter),
            new CounterCreationData("MapReduce Combined/sec", "Number of records Combined per sec.", CounterType.RateOfCounter),

            //write behind counters
            new CounterCreationData("Write-behind queue count", "Number of operations in Write-behind queue.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Write-behind/sec", "Number of Write-behind operations per second.", CounterType.SampleCounter),
            new CounterCreationData("Average us/datasource write", "Average time, in microseconds (us), taken to complete one datasource write operation. Datasource write operations include both write-thru and write-behind operations.", CounterType.AverageCounter),
            new CounterCreationData("Average us/datasource write base", "Base counter for Average microseconds (us) /datasource write.", CounterType.AverageCounter, true),
            new CounterCreationData("Write-behind failure retry count", "Number of operations failed enqueued for retry. Data source write operation returning FailureRetry as status are also enqueued for retry.", CounterType.NumberOfItemCounter),
            new CounterCreationData("Write-behind evictions/sec", "Number of items evicted per second from write-behind queue. Only failed operation are evicted which are enqueued for operation retry.", CounterType.SampleCounter),
            new CounterCreationData("Datasource updates/sec", "Number of update operations per second in cache after datasource write operations.", CounterType.SampleCounter),
            new CounterCreationData("Average us/datasource update", "Average time, in microseconds (us), taken to complete one datasource update cache operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/datasource update base", "Base counter for Average microseconds (us)/datasource update", CounterType.AverageCounter, true),
            new CounterCreationData("Datasource failed operations/sec", "Number of datasource write operations failed per second. Write operations performed on datasource provider returning Failure/FailureRetry/FailureDontRemove as status of OperationResult are counted.", CounterType.SampleCounter),
            new CounterCreationData("Write-behind batch count", "Number of operations selected in current batch interval for execution. For write-behind, if batching is enabled, number of operations dequeued in current batch interval for execution is displayed by this counter.", CounterType.NumberOfItemCounter),
            

            //Bulk Counters
            new CounterCreationData("Average us/addbulk", "Average time in microseconds (us), taken to complete bulk add operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/addbulk base", "Base counter for Average microseconds (us)/addbulk", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/fetchbulk", "Average time in microseconds (us), taken to complete bulk add operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/fetchbulk base", "Base counter for Average microseconds (us)/fetchbulk", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/insertbulk", "Average time in microseconds (us), taken to complete bulk add operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/insertbulk base", "Base counter for Average microseconds (us)/insertbulk", CounterType.AverageCounter, true),
            new CounterCreationData("Average us/removebulk", "Average time in microseconds (us), taken to complete bulk add operation.", CounterType.AverageCounter),
            new CounterCreationData("Average us/removebulk base", "Base counter for Average microseconds (us)/removebulk", CounterType.AverageCounter, true),       

             #region Pub_Sub
            new CounterCreationData(CounterNames.MessageCount, "Number of messages in the cache.", CounterType.NumberOfItemCounter),
            new CounterCreationData(CounterNames.TopicCount, "Number of Topics in the cache.", CounterType.NumberOfItemCounter),
            new CounterCreationData(CounterNames.MessageStoreSize, "Size of message store in bytes, including message store meta info.", CounterType.NumberOfItemCounter),
            new CounterCreationData(CounterNames.MessagePublishPerSec, "Number of messages published per second.", CounterType.SampleCounter),
            new CounterCreationData(CounterNames.MessageDeliveryPerSec, "Number of messages delivered to subsribers per second.", CounterType.SampleCounter),
            new CounterCreationData(CounterNames.MessageExpiredPerSec, "Number of messages expired per second.", CounterType.SampleCounter),

            #endregion

        
            };
        }
    }
}

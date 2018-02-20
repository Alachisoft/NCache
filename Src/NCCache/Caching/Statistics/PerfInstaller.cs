using System.ComponentModel;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Statistics
{
	/// <summary>
	/// Summary description for PerfInstaller.
	/// </summary>
	[RunInstaller(true)]
	public class PerfInstaller : System.Configuration.Install.Installer
	{
		private System.Diagnostics.PerformanceCounterInstaller pcInstaller;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public PerfInstaller()
		{
			// This call is required by the Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}


		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.pcInstaller = new System.Diagnostics.PerformanceCounterInstaller();
			// 
			// pcInstaller
			//

            this.pcInstaller.CategoryName = "NCache";
            this.pcInstaller.Counters.AddRange(new System.Diagnostics.CounterCreationData[] {
            new System.Diagnostics.CounterCreationData("Count", "Number of items in the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("CacheLastAccessCount", "Number of items which are older then the access interval specified in the service config file.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Fetches/sec", "Number of Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Additions/sec", "Number of Add operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Updates/sec", "Number of Insert operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Deletes/sec", "Number of Remove operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average us/fetch", "Average time in microseconds (us), taken to complete one fetch operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/fetch base", "Base counter for average microseconds(us)/fetch", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/add", "Average time in microseconds (us), taken to complete one add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/add base", "Base counter for average microseconds (us)sec/add", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/insert", "Average time in microseconds (us), taken to complete one insert operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/insert base", "Base counter for average microseconds (us)/insert", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/remove", "Average time in microseconds (us), taken to complete one remove operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/remove base", "Base counter for average microseconds (us)/remove", System.Diagnostics.PerformanceCounterType.AverageBase),                                                                                       
            new System.Diagnostics.CounterCreationData("Average us/cache operation", "Average time in microseconds (us), taken to complete one cache-operation.", System.Diagnostics.PerformanceCounterType.AverageCount64),
            new System.Diagnostics.CounterCreationData("Average us/cache operation base", "Base counter for average microseconds (us) /cache-operation", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Expirations/sec", "Number of items being expired currently per second", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Evictions/sec", "Number of items evicted per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
#if !(CLIENT)
            new System.Diagnostics.CounterCreationData("State Transfer/sec", "Number of items this node is either reading from other nodes or sending to other nodes during a state transfer mode.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Mirror queue size", "Number of items in the Mirror queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Sliding Index queue size", "Number of items in the Sliding-Index queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
#endif
            new System.Diagnostics.CounterCreationData("Hits/sec", "Number of successful Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),            
            new System.Diagnostics.CounterCreationData("Misses/sec", "Number of failed Get operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Hits ratio/sec (%)", "Ratio of number of successful Get operations per second and total number of Get operations per second ", System.Diagnostics.PerformanceCounterType.SampleFraction),
            new System.Diagnostics.CounterCreationData("Hits ratio/sec base", "Base counter for Hits ratio/sec", System.Diagnostics.PerformanceCounterType.SampleBase),
#if !(CLIENT)
            //Moiz: perfmon description task 29-10-13
            //previous description (till 4.1 sp3) for DispatchEnter,TcpDown,Clustered opsent,clusters oprecv, reponse sent (Number of clustered operations sent to other nodes in cluster per second.)
            new System.Diagnostics.CounterCreationData("Cluster ops/sec", "Number of clustered operations performed per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("DispatchEnter/sec", "", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("TcpdownEnter/sec", "", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Clustered opssent/sec", "Number of clustered operations sent to other nodes in cluster per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Clustered opsrecv/sec", "Number of clustered operations received from other nodes in cluster per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Response sent/sec", "Number of responses sent to other nodes in cluster per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
#endif
            new System.Diagnostics.CounterCreationData("Bytes sent/sec", "Number of bytes sent per second to other nodes of the cluster.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Bytes received/sec", "Number of bytes received per second from other nodes of the cluster.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Requests/sec", "Number of requests received (meaning cache commands like add, get, insert, remove etc.) from all clients to this cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Responses/sec", "Number of responses sent (meaning cache response for commands like add, get, insert, remove etc.) to all clients by this cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Client Requests/sec", "Number of requests sent by all clients to the cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Client Responses/sec", "Number of responses received by all clients from the cache server.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Client bytes sent/sec", "Bytes being sent from cache server to all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("Client bytes received/sec", "Bytes being received by cache server from all its clients.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("TcpUpQueueCount", "Number of items in TCP up-queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("TcpDownQueueCount", "Number of items in TCP down-queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
#if !(CLIENT)
            new System.Diagnostics.CounterCreationData("BcastQueueCount", "Number of items in BCast queue waiting to be processed on sequence.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("McastQueueCount", "Number of items in MCast queue waiting to be processed on sequence.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
#endif
            new System.Diagnostics.CounterCreationData("Socket send time (msec)", "Time in milli seconds it took for the last message to be sent over the socket.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Socket send size (bytes)", "How much data was sent in the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("General Notifications Queue Size", "Number of general notification events in queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Socket recv time (msec)", "Time in milli seconds it took to receive the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Socket recv size (bytes)", "How much data was received in the last message.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Read-thru/sec", "Number of Read-thru operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Write-thru/sec", "Number of Write-thru operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Response Queue Count", "Number of items in response queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Event Queue Count", "Number of items in event queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Response Queue Size", "Size of response queue specified in bytes.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
           
            new System.Diagnostics.CounterCreationData("Cache Size", "Size of the cache in bytes, including cache store meta info and all other indices.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Query Index Size", "Size of query indices in bytes, defined on the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Expiration Index Size", "Size of expiration in bytes, indices defined on the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Eviction Index Size", "Size of eviction indices in bytes, define on the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Group Index Size", "Size of group and sub group indices in bytes defined on this cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Queries/sec", "Number of queries executing per sec on the cache.", System.Diagnostics.PerformanceCounterType.SampleCounter),

            new System.Diagnostics.CounterCreationData("Average us/Query Execution", "Average time in microseconds(us) query takes while executing.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/Query Execution base", "Average number of items returned by queries in a microsecond (us).", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average Query Size", "Average number of items returned by queries in a second.", System.Diagnostics.PerformanceCounterType.AverageCount64),
            new System.Diagnostics.CounterCreationData("Average Query Size base", "Average number of items returned by queries in a second.", System.Diagnostics.PerformanceCounterType.AverageBase),

            //request logging counters
            new System.Diagnostics.CounterCreationData("Logged Request Count", "Total number of currently logged requests.", System.Diagnostics.PerformanceCounterType.NumberOfItems32),
            new System.Diagnostics.CounterCreationData("Requests Logged/sec", "Average number of requests logged in a second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Request Log Ledger Size", "Total in-memory size of the log ledger, which stores logged requests.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            
            //MapReduce Counters
            new System.Diagnostics.CounterCreationData("MapReduce Running Tasks", "Number of MapReduce tasks running.", System.Diagnostics.PerformanceCounterType.NumberOfItems32),
            new System.Diagnostics.CounterCreationData("MapReduce Waiting Tasks", "Number of MapReduce tasks waiting to be executed.", System.Diagnostics.PerformanceCounterType.NumberOfItems32),
            new System.Diagnostics.CounterCreationData("MapReduce Mapped/sec", "Number of records Mapped per sec.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("MapReduce Reduced/sec", "Number of records Reduced per sec.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),
            new System.Diagnostics.CounterCreationData("MapReduce Combined/sec", "Number of records Combined per sec.", System.Diagnostics.PerformanceCounterType.RateOfCountsPerSecond64),


            //write behind counters
            new System.Diagnostics.CounterCreationData("Write-behind queue count", "Number of operations in Write-behind queue.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Write-behind/sec", "Number of Write-behind operations per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average us/datasource write", "Average time, in microseconds (us), taken to complete one datasource write operation. Datasource write operations include both write-thru and write-behind operations.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/datasource write base", "Base counter for Average microseconds (us) /datasource write.", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Write-behind failure retry count", "Number of operations failed enqueued for retry. Data source write operation returning FailureRetry as status are also enqueued for retry.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData("Write-behind evictions/sec", "Number of items evicted per second from write-behind queue. Only failed operation are evicted which are enqueued for operation retry.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Datasource updates/sec", "Number of update operations per second in cache after datasource write operations.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Average us/datasource update","Average time, in microseconds (us), taken to complete one datasource update cache operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/datasource update base", "Base counter for Average microseconds (us)/datasource update", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Datasource failed operations/sec", "Number of datasource write operations failed per second. Write operations performed on datasource provider returning Failure/FailureRetry/FailureDontRemove as status of OperationResult are counted.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData("Current batch operations count", "Number of operations selected in current batch interval for execution. For write-behind, if batching is enabled, number of operations dequeued in current batch interval for execution is displayed by this counter.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            

            //Bulk Counters
            new System.Diagnostics.CounterCreationData("Average us/addbulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/addbulk base", "Base counter for Average microseconds (us)/addbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/fetchbulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/fetchbulk base", "Base counter for Average microseconds (us)/fetchbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/insertbulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/insertbulk base", "Base counter for Average microseconds (us)/insertbulk", System.Diagnostics.PerformanceCounterType.AverageBase),
            new System.Diagnostics.CounterCreationData("Average us/removebulk", "Average time in microseconds (us), taken to complete bulk add operation.", System.Diagnostics.PerformanceCounterType.AverageTimer32),
            new System.Diagnostics.CounterCreationData("Average us/removebulk base", "Base counter for Average microseconds (us)/removebulk", System.Diagnostics.PerformanceCounterType.AverageBase),

            new System.Diagnostics.CounterCreationData(CounterNames.RUNNINGREADERS, "Number of running readers.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),

            #region Pub_Sub
            new System.Diagnostics.CounterCreationData(CounterNames.MessageCount, "Number of messages in the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems32),
            new System.Diagnostics.CounterCreationData(CounterNames.TopicCount, "Number of Topics in the cache.", System.Diagnostics.PerformanceCounterType.NumberOfItems32),
            new System.Diagnostics.CounterCreationData(CounterNames.MessageStoreSize, "Size of message store in bytes, including message store meta info.", System.Diagnostics.PerformanceCounterType.NumberOfItems64),
            new System.Diagnostics.CounterCreationData(CounterNames.MessagePublishPerSec, "Number of messages published per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
            new System.Diagnostics.CounterCreationData(CounterNames.MessageDeliveryPerSec, "Number of messages delivered to subsribers per second.", System.Diagnostics.PerformanceCounterType.SampleCounter),
             new System.Diagnostics.CounterCreationData(CounterNames.MessageExpiredPerSec, "Number of messages expired per second.", System.Diagnostics.PerformanceCounterType.SampleCounter)

#endregion

            });

            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.pcInstaller});
          

		}
		#endregion
	}
}

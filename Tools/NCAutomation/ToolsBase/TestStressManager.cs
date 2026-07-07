
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Automation.ToolsOutput;
using System.Management.Automation;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Caching;
using Message = Alachisoft.NCache.Runtime.Caching.Message;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    public sealed class TestStressManager : PSCmdlet
    {
        string _cacheId = "";
        int _totalLoopCount = 0;
        int _testCaseIterations = 10;
        int _testCaseIterationDelay = 0;
        int _getsPerIteration = 1;
        int _updatesPerIteration = 1;
        int _dataSize = 1024;
        int _expiration = 300;
        int _threadCount = 1;
        int _reportingInterval = 5000;
        string _servers;
        IOutputConsole _outputProvider;
        IList<StressThreadTask> _tasks = new List<StressThreadTask>();
        ICache _cache;
        PowerShellAdapter _adapter;
        internal IOutputConsole OutputProvider { get { return _outputProvider; } }
        internal string CacheName { get { return _cacheId; } }
        internal int ItemsCount { get { return _totalLoopCount; } }
        internal int TestCaseIterations { get { return _testCaseIterations; } }
        internal int TestCaseIterationDelay { get { return _testCaseIterationDelay; } }
        internal int GetsPerIteration { get { return _getsPerIteration; } }
        internal int UpdatesPerIteration { get { return _updatesPerIteration; } }
        internal int DataSize { get { return _dataSize; } }
        internal int ThreadCount { get { return _threadCount; } }
        internal int? SlidingExpiration { get { return _expiration; } }
        internal int ReportingInterval { get { return _reportingInterval; } }

        public TestStressManager(string cacheId, int totalLoopCount, int testCaseIterations, int testCaseIterationDelay, int getsPerIteration, int updatesPerIteration, int dataSize, int expiration, int threadCount, int reportingInterval, string servers, bool noLogo, IOutputConsole outputProvider, PowerShellAdapter adapter)
        {
            _cacheId = cacheId;
            _totalLoopCount = totalLoopCount;
            _testCaseIterations = testCaseIterations;
            _testCaseIterationDelay = testCaseIterationDelay;
            _getsPerIteration = getsPerIteration;
            _updatesPerIteration = updatesPerIteration;
            _dataSize = dataSize;
            _expiration = expiration;
            _threadCount = threadCount;
            _reportingInterval = reportingInterval;
            _servers = servers;
            _outputProvider = outputProvider;
            _adapter = adapter;
        }

        public void StartTasks()
        {
            try
            {
                Thread[] threads = new Thread[_threadCount];
                CacheConnectionOptions parameters = new CacheConnectionOptions();
                parameters = ToolsUtil.AddServersInCacheConnectionOptions(_servers, parameters);
                _cache = CacheManager.GetCache(_cacheId, parameters);
                var storeType = Broker.CacheStoreType;
                if (storeType == null)
                    storeType = StoreTypeUtil.DISTRIBUTED_CACHE;
                //_cache.ExceptionsEnabled = true;

                string pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();

                for (int threadIndex = 0; threadIndex < _threadCount; threadIndex++)
                {
                    StressThreadTask threadTask = GetCacheStressThreadTaskInstance(threadIndex, storeType);
                    PrintHeaderBaseOnStoreType(storeType);
                    threadTask.CacheId = _cacheId;
                    _tasks.Add(threadTask);
                    threadTask.Start();

                }
                _adapter.Listen();
            }
            catch (Exception e)
            {

                _outputProvider.WriteErrorLine("Error: " + e.Message);
            }
        }
        private void PrintHeaderBaseOnStoreType(string storeType)
        {
            string output = $"Simulating stress on cache {CacheName} (Distributed Cache)\n";
            switch (storeType.ToLower())
            { 
                case StoreTypeUtil.DISTRIBUTED_CACHE:
                    {
                        OutputProvider.WriteLine(output);
                        OutputProvider.WriteLine("cacheId = {0}, total-loop-count = {1}, test-case-iterations = {2}, testCaseIterationDelay = {3}, gets-per-iteration = {4}, updates-per-iteration = {5}, data-size = {6}, expiration = {7}, thread-count = {8}, reporting-interval = {9}.",
                            CacheName, ItemsCount, TestCaseIterations, TestCaseIterationDelay, GetsPerIteration, UpdatesPerIteration, DataSize, SlidingExpiration, ThreadCount, ReportingInterval);
                        break;
                    }
                case StoreTypeUtil.PUB_SUB_MESSAGING:
                    {
                        storeType = $"Simulating stress on cache {CacheName} (Pub/Sub Store)\n";
                        OutputProvider.WriteLine(storeType);
                        OutputProvider.WriteLine("cacheId = {0}, total-loop-count = {1}, test-case-iterations = {2}, testCaseIterationDelay = {3}, data-size = {4}, expiration = {5}, thread-count = {6}, reporting-interval = {7}.",
                       CacheName, ItemsCount, TestCaseIterations, TestCaseIterationDelay, DataSize, SlidingExpiration, ThreadCount, ReportingInterval);
                        break;
                    }
                default:
                    {
                        OutputProvider.WriteLine(output);
                        OutputProvider.WriteLine("cacheId = {0}, total-loop-count = {1}, test-case-iterations = {2}, testCaseIterationDelay = {3}, gets-per-iteration = {4}, updates-per-iteration = {5}, data-size = {6}, expiration = {7}, thread-count = {8}, reporting-interval = {9}.",
                        CacheName, ItemsCount, TestCaseIterations, TestCaseIterationDelay, GetsPerIteration, UpdatesPerIteration, DataSize, SlidingExpiration, ThreadCount, ReportingInterval);
                        break;
                    }
            }
            OutputProvider.WriteLine("-------------------------------------------------------------------\n");
        }

        public void StopTasks(bool forcefully)
        {
            try
            {
                foreach (var task in _tasks)
                {
                    task.Stop(forcefully);
                }

            }
            catch (Exception ex)
            {
                _outputProvider.WriteLine(ex.ToString());
            }
            finally
            {
                if (_cache != null)
                    _cache.Dispose();
            }
        }

        private StressThreadTask GetCacheStressThreadTaskInstance(int threadIndex, string configstoreType)
        {
            switch (configstoreType.ToLower())
            {
                case StoreTypeUtil.DISTRIBUTED_CACHE:
                    return new CacheStressThreadTask(_cache, _totalLoopCount, _testCaseIterations, _testCaseIterationDelay, _getsPerIteration, _updatesPerIteration, _dataSize, _expiration, _threadCount, _reportingInterval, threadIndex, _outputProvider, _adapter);
                case StoreTypeUtil.PUB_SUB_MESSAGING:
                    return new PubSubStressThreadTask(_cache, _totalLoopCount, _testCaseIterations, _testCaseIterationDelay, _getsPerIteration, _updatesPerIteration, _dataSize, _expiration, _threadCount, _reportingInterval, threadIndex, _outputProvider, _adapter);
               default:
                    return new CacheStressThreadTask(_cache, _totalLoopCount, _testCaseIterations, _testCaseIterationDelay, _getsPerIteration, _updatesPerIteration, _dataSize, _expiration, _threadCount, _reportingInterval, threadIndex, _outputProvider, _adapter);
            }
        }
    }

        public abstract class StressThreadTask : PSCmdlet
        {
            internal ICache _cache = null;
            internal int _totalLoopCount = 0;
            internal int _testCaseIterations = 10;
            internal int _testCaseIterationDelay = 0;
            internal int _getsPerIteration = 1;
            internal int _updatesPerIteration = 1;
            internal int _dataSize = 1024;
            internal int _expiration = 300;
            internal int _threadCount = 1;
            internal int _reportingInterval = 5000;
            internal int _threadIndex = 0;
            internal int _pid = 0;
            internal int numErrors = 0;
            internal int maxErrors = 1000;
            internal IOutputConsole _outputProvider;
            internal Thread thread;
            internal PowerShellAdapter _adapter;
            internal ITopic topic = null;

            internal string CacheId { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            public StressThreadTask(ICache cache, int totalLoopCount, int testCaseIterations, int testCaseIterationDelay, int getsPerIteration, int updatesPerIteration, int dataSize, int expiration, int threadCount, int reportingInterval, int threadIndex, IOutputConsole outputProvider, PowerShellAdapter adapter)
            {
                _cache = cache;
                _totalLoopCount = totalLoopCount;
                _testCaseIterations = testCaseIterations;
                _testCaseIterationDelay = testCaseIterationDelay;
                _getsPerIteration = getsPerIteration;
                _updatesPerIteration = updatesPerIteration;
                _dataSize = dataSize;
                _expiration = expiration;
                _threadCount = threadCount;
                _reportingInterval = reportingInterval;
                _threadIndex = threadIndex;
                _pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                CreateThread();
                _outputProvider = outputProvider;
                _adapter = adapter;
            }

            private void CreateThread()
            {
                ThreadStart threadDelegate = new ThreadStart(DoTest);
                thread = new Thread(threadDelegate);

                thread.Name = "ThreadIndex: " + _threadIndex;
            }
            /// <summary>
            /// Test starting call
            /// </summary>
            public abstract void DoTest();

            public void Start()
            {
                if (thread == null)
                    CreateThread();
                thread.Start();

            }

            public void Stop(bool forcefully)
            {
                try
                {
                    if (thread.IsAlive)
                    {
                        if (forcefully)
                        {
#if NETCORE
                            thread.Interrupt();
#else
                            thread.Abort();
#endif
                            _adapter.Finished = true;
                            return;
                        }
                        else
                        {
                            thread.Join();

                        }
                    }
                    Dispose();

                }
                catch (ThreadAbortException e)
                {
                    Thread.ResetAbort();
                }
                catch (ThreadInterruptedException e)
                {

                }
                catch (Exception e)
                {
                    if (e.Message != null && e.Message.Contains("The WriteObject and WriteError methods cannot be called from outside the overrides of the BeginProcessing, ProcessRecord, and EndProcessing methods, and they can only be called from within the same thread. Validate that the cmdlet makes these calls correctly, or contact Microsoft Customer Support Services."))
                    {
                        _adapter.TerminateThreads = true;
                    }

                    else
                    {
                        _adapter.WriteObject("DoTest() Exception: " + e.ToString() + "\n");
                        _adapter.TerminateThreads = true;
                    }

                }

                _adapter.Finished = true;
            }

            /// <summary>
            /// Perform Get/Insert operations on cache, bsed on user given input.
            /// </summary>
            private void DoGetInsert()
            {

                byte[] data = new byte[_dataSize];

                if (_totalLoopCount <= 0)
                {
                    // this means an infinite loop. user will have to do Ctrl-C to stop the program
                    for (long totalIndex = 0; ; totalIndex++)
                    {
                        ProcessGetInsertIteration(data);
                        if (totalIndex >= _reportingInterval)
                        {
                            try
                            {
                                long count = _cache.Count;
                                _adapter.WriteObject(DateTime.Now.ToString() + ": Cache count: " + count);
                                totalIndex = 1;
                            }
                            catch (Exception e)
                            {
                                _adapter.WriteObject("DoGetInsert() Exception: " + e.ToString() + "\n");
                                numErrors++;
                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (long totalIndex = 0; totalIndex < _totalLoopCount; totalIndex++)
                    {
                        ProcessGetInsertIteration(data);
                        if (totalIndex >= _reportingInterval)
                        {
                            try
                            {
                                long count = _cache.Count;
                                _adapter.WriteObject(DateTime.Now.ToString() + ": Cache count: " + count);
                            }
                            catch (Exception e)
                            {
                                _adapter.WriteObject("DoGetInsert() Exception: " + e.ToString() + "\n");
                                numErrors++;
                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;
                                }
                            }

                        }
                    }
					_adapter.Finished = true;
                }
            }

            /// <summary>
            /// Perform Get/Insert task on cache.
            /// Called by DoGetInsert method
            /// </summary>
            private void ProcessGetInsertIteration(byte[] data)
            {
                string guid = System.Guid.NewGuid().ToString(); //create a unique key to be inserted in store.

                for (long testCaseIndex = 0; testCaseIndex < _testCaseIterations; testCaseIndex++)
                {
                    string key = guid;

                    for (int getsIndex = 0; getsIndex < _getsPerIteration; getsIndex++)
                    {
                        try
                        {
                            object obj = _cache.Get<object>(key);
                        }
                        catch (Exception e)
                        {

                            _adapter.WriteObject("GET Error: Key: " + key + ", Exception: " + e.ToString() + "\n");
                            numErrors++;


                            if (this.numErrors > this.maxErrors)
                            {
                                _adapter.TerminateThreads = true;
                            }

                        }

                    }

                    for (int updatesIndex = 0; updatesIndex < _updatesPerIteration; updatesIndex++)
                    {
                        try
                        {
                            CacheItem item = new CacheItem();
                            item.SetValue(data);
                            item.Expiration = new Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Sliding, new TimeSpan(0, 0, 0, _expiration));
                            item.Priority = CacheItemPriority.Default;

                            _cache.Insert(key, item);
                        }
                        catch (Exception e)
                        {

                            _adapter.WriteObject("INSERT Error: Key: " + key + ", Exception: " + e.ToString() + "\n");
                            numErrors++;

                            if (numErrors > this.maxErrors)
                            {
                                _adapter.TerminateThreads = true;
                            }

                        }
                    }

                    if (_testCaseIterationDelay > 0)
                    {
                        // Sleep for this many seconds
                        Thread.Sleep(_testCaseIterationDelay * 1000);
                    }

                }
            }
            public abstract void Dispose();
        }
        class CacheStressThreadTask : StressThreadTask
        {
            public CacheStressThreadTask(ICache cache, int totalLoopCount, int testCaseIterations, int testCaseIterationDelay, int getsPerIteration, int updatesPerIteration, int dataSize, int expiration, int threadCount, int reportingInterval, int threadIndex, IOutputConsole outputProvider, PowerShellAdapter adapter) :
                base(cache, totalLoopCount, testCaseIterations, testCaseIterationDelay, getsPerIteration, updatesPerIteration, dataSize, expiration, threadCount, reportingInterval, threadIndex, outputProvider, adapter)
            {


            }

            /// <summary>
            /// Test starting call
            /// </summary>
            public override void DoTest()
            {
                try
                {
                    DoGetInsert();
                }
                catch (Exception e)
                {
                    if (e.Message != null && e.Message.Contains("The WriteObject and WriteError methods cannot be called from outside the overrides of the BeginProcessing, ProcessRecord, and EndProcessing methods, and they can only be called from within the same thread. Validate that the cmdlet makes these calls correctly, or contact Microsoft Customer Support Services."))
                    {
                        _adapter.TerminateThreads = true;
                    }

                    else
                    {
                        _adapter.WriteObject("DoTest() Exception: " + e.ToString() + "\n");
                        _adapter.TerminateThreads = true;

                    }

                }
            }


            /// <summary>
            /// Perform Get/Insert operations on cache, bsed on user given input.
            /// </summary>
            private void DoGetInsert()
            {

                byte[] data = new byte[_dataSize];

                if (_totalLoopCount <= 0)
                {
                    // this means an infinite loop. user will have to do Ctrl-C to stop the program
                    for (long totalIndex = 0; ; totalIndex++)
                    {
                        ProcessGetInsertIteration(data);
                        if (totalIndex >= _reportingInterval)
                        {
                            try
                            {
                                long count = _cache.Count;
                                _adapter.WriteObject(DateTime.Now.ToString() + ": Cache count: " + count);
                                totalIndex = 1;
                            }
                            catch (Exception e)
                            {
                                _adapter.WriteObject("DoGetInsert() Exception: " + e.ToString() + "\n");
                                numErrors++;
                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (long totalIndex = 0; totalIndex < _totalLoopCount; totalIndex++)
                    {
                        ProcessGetInsertIteration(data);
                        if (totalIndex >= _reportingInterval)
                        {
                            try
                            {
                                long count = _cache.Count;
                                _adapter.WriteObject(DateTime.Now.ToString() + ": Cache count: " + count);
                            }
                            catch (Exception e)
                            {
                                _adapter.WriteObject("DoGetInsert() Exception: " + e.ToString() + "\n");
                                numErrors++;
                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;
                                }
                            }

                        }
                    }
                }
            }

            /// <summary>
            /// Perform Get/Insert task on cache.
            /// Called by DoGetInsert method
            /// </summary>
            private void ProcessGetInsertIteration(byte[] data)
            {
                string guid = System.Guid.NewGuid().ToString(); //create a unique key to be inserted in store.

                for (long testCaseIndex = 0; testCaseIndex < _testCaseIterations; testCaseIndex++)
                {
                    string key = guid;

                    for (int getsIndex = 0; getsIndex < _getsPerIteration; getsIndex++)
                    {
                        try
                        {
                            object obj = _cache.Get<byte[]>(key);
                        }
                        catch (Exception e)
                        {

                            _adapter.WriteObject("GET Error: Key: " + key + ", Exception: " + e.ToString() + "\n");
                            numErrors++;


                            if (this.numErrors > this.maxErrors)
                            {
                                _adapter.TerminateThreads = true;
                            }

                        }

                    }

                    for (int updatesIndex = 0; updatesIndex < _updatesPerIteration; updatesIndex++)
                    {
                        try
                        {
                            Client.CacheItem item = new Client.CacheItem(data);
                            item.Expiration = new Runtime.Caching.Expiration(Runtime.Caching.ExpirationType.Sliding, new TimeSpan(0, 0, 0, _expiration));
                            item.Priority = CacheItemPriority.Default;

                            _cache.Insert(key, item);
                        }
                        catch (Exception e)
                        {

                            _adapter.WriteObject("INSERT Error: Key: " + key + ", Exception: " + e.ToString() + "\n");
                            numErrors++;

                            if (numErrors > this.maxErrors)
                            {
                                _adapter.TerminateThreads = true;
                            }

                        }
                    }

                    if (_testCaseIterationDelay > 0)
                    {
                        // Sleep for this many seconds
                        Thread.Sleep(_testCaseIterationDelay * 1000);
                    }

                }
            }

            public override void Dispose()
            {
                _cache.Dispose();
            }

        }
        class PubSubStressThreadTask : StressThreadTask
        {
            private static long _receivedMessages;
            private static long _publishedMessages;
            private object _mutex = new object();
            private string _topicName = System.Guid.NewGuid().ToString("N");
            private int _reportingIntervalPubSub = 50000;
            public PubSubStressThreadTask(ICache cache, int totalLoopCount, int testCaseIterations, int testCaseIterationDelay, int getsPerIteration, int updatesPerIteration, int dataSize, int expiration, int threadCount, int reportingInterval, int threadIndex, IOutputConsole outputProvider, PowerShellAdapter adapter) :
                base(cache, totalLoopCount, testCaseIterations, testCaseIterationDelay, getsPerIteration, updatesPerIteration, dataSize, expiration, threadCount, reportingInterval, threadIndex, outputProvider, adapter)
            {

            }


            public override void DoTest()
            {
                try
                {
                    DoPubSub();
                }
                catch (OperationFailedException ex)
                {
                    if (ex.ErrorCode == NCacheErrorCodes.TOPIC_NOT_FOUND)
                        DoPubSub();

                    if (ex.ErrorCode == NCacheErrorCodes.NO_SERVER_AVAILABLE)
                        throw;
                }
                catch (Exception e)
                {
                    if (e.Message != null && e.Message.Contains("The WriteObject and WriteError methods cannot be called from outside the overrides of the BeginProcessing, ProcessRecord, and EndProcessing methods, and they can only be called from within the same thread. Validate that the cmdlet makes these calls correctly, or contact Microsoft Customer Support Services."))
                    {
                        _adapter.TerminateThreads = true;
                    }

                    else
                    {
                        _adapter.WriteObject("Pub\\SubTest() Exception: " + e.ToString() + "\n");
                        _adapter.TerminateThreads = true;
                    }
                }
            }

            private void DoPubSub()
            {
                ITopic topic = _cache.MessagingService.GetTopic(_topicName);
                byte[] data = new byte[_dataSize];
                if (topic == null)
                {
                    topic = _cache.MessagingService.CreateTopic(_topicName);
                }
                topic.CreateSubscription(MessageReceivedCallback);
                if (_reportingInterval == 5000)
                    _reportingInterval = _reportingIntervalPubSub;
                if (_totalLoopCount <= 0)
                {
                    for (long totalIndex = 0; ; totalIndex++)
                    {
                        PublishMessages(topic, data);
                        if (totalIndex >= _reportingInterval)
                        {
                            try
                            {
                                long count = topic.MessageCount;
                                _adapter.WriteObject(DateTime.Now.ToString() + ": Message count: " + count + ": Message Received: " + _receivedMessages + ": Published Messages: " + _publishedMessages);
                                totalIndex = 1;
                            }
                            catch (Exception e)
                            {
                                _adapter.WriteObject("Pub\\SubTest() Exception: " + e.ToString() + "\n");
                                numErrors++;
                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;
                                }

                            }
                        }
                    }
                }
                else
                {
                    for (long totalIndex = 0; totalIndex < _totalLoopCount; totalIndex++)
                    {
                        PublishMessages(topic, data);
                        if (totalIndex >= _reportingInterval)
                        {
                            try
                            {
                                long count = topic.MessageCount;
                                _adapter.WriteObject(DateTime.Now.ToString() + ": Message count: " + count + ": Message Received: " + _receivedMessages + ": Published Messages: " + _publishedMessages);

                            }
                            catch (Exception e)
                            {
                                _adapter.WriteObject("Pub\\SubTest() Exception: " + e.ToString() + "\n");
                                numErrors++;
                                if (this.numErrors > this.maxErrors)
                                {
                                    _adapter.TerminateThreads = true;
                                }

                            }
                        }
                    }
                }
            }

            private void MessageDeliveryFailure(object sender, MessageFailedEventArgs args)
            {
            }
            private void MessageReceivedCallback(object sender, MessageEventArgs args)
            {
                lock (_mutex)
                {
                    _receivedMessages++;
                }
            }

            private void PublishMessages(ITopic topic, byte[] data)
            {
                Message message = new Message(data, new TimeSpan(0, 0, 0, _expiration));
                try
                {
                    topic.Publish(message, DeliveryOption.All, true);
                    lock (_mutex)
                    {
                        _publishedMessages++;
                    }

                }
                catch (Exception e)
                {

                    _adapter.WriteObject("Publish Messages Error: Message Id:" + message.MessageId + ", Exception: " + e.ToString() + "\n");
                    numErrors++;

                    if (this.numErrors > this.maxErrors)
                    {
                        _adapter.TerminateThreads = true;
                    }
                }
            }

            public override void Dispose()
            {
                try
                {
                    topic.Dispose();
                }
                catch (Exception)
                {
                }
                _cache.MessagingService.DeleteTopic(_topicName);
                _cache.Dispose();
            }
        }
        sealed class Product
        {
            public int ProductID { get; set; }

            public string Name { get; set; }

            public string Category { get; set; }

            public string Description { get; set; }

            public string InstanceId { get; set; }
        }
        class DataBuilder
        {
            readonly string[] ProductNames = new string[] { "", "" };
            string _instanceId;
            public DataBuilder(string instanceId)
            {
                this._instanceId = instanceId;
            }
        /// <summary>
        /// Populates and returns the Products.
        /// </summary>
        /// <param name="count">Number of product objects to fetch.</param>
        /// <returns></returns>
        internal IEnumerable<Product> FetchProducts(int count)
        {
            Random rnd = new Random();


            for (int i = 0; i < count; i++)
            {
                Product product = new Product();

                // 25% document add in each category
                if (i < (count * 0.25))
                {
                    product.Name = ProductNameData.ProductNames[rnd.Next(0, 16)];
                    product.Category = $" {product.ToString()} Beverages";
                    product.Description = "This category contains products like Soft Drinks, Coffees, Teas, Beers and Ales. Price ranges vary from product to product. Other products lie in different Product Categories. ";
                    product.InstanceId = "This document is created by instance " + _instanceId + " of Test-Stress";

                }
                else if (i >= (count * .25) && i < (count * 0.5))
                {
                    product.Name = ProductNameData.ProductNames[rnd.Next(16, 31)];
                    product.Category = $" {product.ToString()} Seafood";
                    product.Description = "This category contains products like Seaweed and Fish. Price ranges vary from product to product. Other products lie in different Product Categories. ";
                    product.InstanceId = "This document is created by instance " + _instanceId + " of Test-Stress";

                }
                else if (i >= (count * 0.5) && i < (count * 0.75))
                {
                    product.Name = ProductNameData.ProductNames[rnd.Next(30, 45)];
                    product.Category = $" {product.ToString()} Meat";
                    product.Description = "This category contains products like Chicken, Lamb and Beef.  Price ranges vary from product to product. Other products lie in different Product Categories. ";
                    product.InstanceId = "This document is created by instance " + _instanceId + " of Test-Stress";

                }

                else
                {
                    product.Name = ProductNameData.ProductNames[rnd.Next(30, 45)];
                    product.Category = $" {product.ToString()} Other Category";
                    product.Description = "This category contains products like Dried Fruits and Packed Foods. Price ranges vary from product to product. Other products lie in different Product Categories. ";
                    product.InstanceId = "This document is created by instance " + _instanceId + " of Test-Stress";

                }

                yield return product;
            }
        }
        }
        class ProductNameData
        {
            public static readonly string[] ProductNames = new string[]
            {
            "Chai",
            "Chang",
            "Aniseed Syrup",
            "Chef Anton's Cajun Seasoning",
            "Grandma's Boysenberry Spread",
            "Uncle Bob's Organic Dried Pears",
            "Northwoods Cranberry Sauce",
            "Ikura12",
            "Queso Cabrales",
            "Queso Manchego La Pastora",
            "Konbu",
            "Tofu",
            "Genen Shouyu",
            "Pavlova",
            "Carnarvon Tigers",
            "Teatime Chocolate Biscuits",
            "Sir Rodney's Marmalade",
            "Sir Rodney's Scones",
            "Gustaf's Knäckebröd",
            "Tunnbröd",
            "NuNuCa Nuß-Nougat-Creme",
            "Gumbär Gummibärchen",
            "Schoggi Schokolade",
            "Nord-Ost Matjeshering",
            "Gorgonzola Telino",
            "Mascarpone Fabioli",
            "Geitost",
            "Sasquatch Ale",
            "Steeleye Stout",
            "Inlagd Sill",
            "Gravad lax",
            "Côte de Blaye",
            "Chartreuse verte",
            "Boston Crab Meat",
            "Jack's New England Clam Chowder",
            "Ipoh Coffee",
            "Gula Malacca",
            "Rogede sild",
            "Spegesild",
            "Zaanse koeken",
            "Chocolade",
            "Maxilaku",
            "Valkoinen suklaa",
            "Manjimup Dried Apples",
            "Filo Mix",
            "Tourtière",
            "Pâté chinois",
            "Gnocchi di nonna Alice",
            "Ravioli Angelo",
            "Escargots de Bourgogne"
            };
        }

    
}

// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif
using Alachisoft.NCache.MapReduce.Notifications;
using Alachisoft.NCache.Runtime.MapReduce;
using Alachisoft.NCache.Caching;
using System.Collections;
using Alachisoft.NCache.Common.Net;
using System.Threading;
using Alachisoft.NCache.MapReduce.OutputProviders;
using Alachisoft.NCache.Common.MapReduce;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Topologies.Local;

namespace Alachisoft.NCache.MapReduce
{
    internal class MapReduceTask : TaskBase
    {
        private const int INMEMORY = 0;
        private CacheBase _parent = null;
        private event TaskCallback Callback = null;
        private CacheRuntimeContext _context = null;

        private int maxExceptions = 10;
        private int exceptionCount = 0;
        private bool ReducerConfigured = false;
        private bool CombinerConfigured = false;

        private Hashtable participants = null;
#if !CLIENT
        private DistributionManager distributionMgr = null;
#endif
        MapperTask mapperTask = null;
        CombinerTask combinerTask = null;
        ReducerTask reducerTask = null;

        private Queue reducerDataQueue = new Queue();

        private ClusteredArrayList localOutput = new ClusteredArrayList();

        private MapReduceThrottlingManager throttlingMgr = null;
        private MapReduceOutput outputProvider = null;
        private int outputOption = INMEMORY;

        private bool isLocal = false;

        private object mutex = new object();

        public MapReduceTask(CacheBase parent, TaskCallback callback, 
            IMapper mapper, 
            ICombinerFactory combiner, 
            IReducerFactory reducer, 
            MapReduceInput inputProvider,
            MapReduceOutput outputProvider,
            Filter filter, CacheRuntimeContext context, int chunkSize, int maxExceps)
        {

            this._parent = parent;
            this.Callback = callback;
            this._context = context;
            this.maxExceptions = maxExceps;

            this.mapperTask = new MapperTask(mapper, inputProvider, filter != null ? filter.KeyFilter : null, this);

            if (reducer != null)
            {
                ReducerConfigured = true;
                this.reducerTask = new ReducerTask(reducer, this);
            }
            else
            {
                ReducerConfigured = true;
                this.reducerTask = new ReducerTask(new IdentityReducerFactory(), this);
            }
            if (combiner != null)
            {
                CombinerConfigured = true;
                this.combinerTask = new CombinerTask(combiner, this);
            }

            throttlingMgr = new MapReduceThrottlingManager(chunkSize, this);
            this.outputProvider = outputProvider;

            if (_parent != null && _parent is LocalCacheImpl)
            {
                isLocal = true;
                participants = new Hashtable();
                participants.Add(new Address(_parent.Context.Render.IPAddress, _parent.Context.Render.Port), new NodeTaskStatus());
            }
#if !CLIENT
            if (_parent != null && _parent is ClusterCacheBase)
            {
                ArrayList list = ((ClusterCacheBase)_parent).ActiveServers;
                IEnumerator it = list.GetEnumerator();
                participants = new Hashtable();
                while (it.MoveNext()) {
                    participants.Add(it.Current, new NodeTaskStatus());
                }

                if (parent is PartitionedServerCache) {
                    distributionMgr = ((PartitionedServerCache) parent).DistributionMgr;
                }
            }
#endif

            //Queue itialization and synchronization
            Queue syncQueue = Queue.Synchronized(reducerDataQueue);
            this.reducerDataQueue = syncQueue;


        }

        
        public override void Dispose()
        {
            localOutput.Clear();
            reducerDataQueue.Clear();
        }

        internal CacheRuntimeContext Context
        {
            get { return _context; }
            set { _context = value; }
        }
        internal int ExceptionCount
        {
            get { return exceptionCount; }
            set { exceptionCount = value; }
        }
        internal int MaxExceptions
        {
            get { return maxExceptions; }
            set { maxExceptions = value; }
        }
        public Queue ReducerDataQueue
        {
            get { return reducerDataQueue; }
            set { reducerDataQueue = value; }
        }

        public override void StartTask()
        {
            if (Context != null)
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").StartTask", "MapReduce task is starting.");
            }

            if (mapperTask != null)
                mapperTask.StartTask();

            if (combinerTask != null)
                combinerTask.StartTask();

            if (reducerTask != null)
                reducerTask.StartTask();

            if (Context != null)
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").StartTask", "MapReduce task is started.");
            }
        }

        public override void StopTask()
        {
            if (Context != null)
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").StopTask", "MapReduce task is stopping.");
            }

            if (mapperTask != null)
                mapperTask.StopTask();

            if (combinerTask != null && CombinerConfigured)
                combinerTask.StopTask();

            if (reducerTask != null && ReducerConfigured)
                reducerTask.StopTask();

            mapperTask = null;
            combinerTask = null;
            reducerTask = null;

            if (Context != null)
            {
                if (Context.NCacheLog.IsInfoEnabled)
                    Context.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").StopTask", "MapReduce task has been stopped.");
            }
        }

        private void Throttle()
        {
            if (CombinerConfigured) {
                combinerTask.CombinerInputQueue.Enqueue(true);
            } else {
                SendReducerData();
            }
        }

        internal void LocalMapperFailed()
        {
            SendParticipantMapperFailedMessage();
        }

        internal void LocalCombinerFailed()
        {
            SendParticipantMapperFailedMessage();
        }

        internal void LocalMapperCompleted()
        {
            if (!CombinerConfigured)
            {
                SendReducerData();

                if (!ReducerConfigured)
                {
                    PersistOutput(localOutput);
                    if (_parent.NCacheLog.IsInfoEnabled)
                    {
                        _parent.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").LocalMapperCompleted", "Local Mapper Completed, output persisted.");
                    }
                }

                SendParticipantMapperCompletedMessage();
            }
            else
            {
                this.combinerTask.IsMapperAlive = false;
            }
        }

        internal void LocalCombinerCompleted()
        {
            if (!ReducerConfigured)
            {
                PersistOutput(localOutput);
            }
            SendParticipantMapperCompletedMessage();
        }

        private void SendParticipantMapperCompletedMessage()
        {
            MapReduceOperation operation = new MapReduceOperation();
            operation.TaskID = this.TaskId;
            operation.OpCode = MapReduceOpCodes.MapperCompleted;

            try
            {
                if (!isLocal && participants != null)
                    _parent.SendMapReduceOperation(new System.Collections.ArrayList(participants.Keys), operation);
            }
            catch (Exception ex)
            {
                _parent.Context.NCacheLog.Error("MapReduceTask(" + this.TaskId + ").SendParticipantMapperCompletedMessage", "Failed: " + ex.Message);
            }
            MapperCompleted(operation);
        }

        private void SendParticipantMapperFailedMessage()
        {
            MapReduceOperation operation = new MapReduceOperation();
            operation.TaskID = this.TaskId;
            operation.OpCode = MapReduceOpCodes.MapperFailed;

            try
            {
                if(!isLocal && participants != null)
                    _parent.SendMapReduceOperation(new System.Collections.ArrayList(participants.Keys), operation);
            }
            catch (Exception ex)
            {
                _parent.Context.NCacheLog.Error("MapReduceTask(" + this.TaskId + ").SendParticipantMapperFailedMessage", "Failed: " + ex.Message);
            }
            MapperFailed(operation);
        }

        public void MapperFailed(MapReduceOperation op)
        {
            Address source = op.Source != null ? op.Source : new Address(_parent.Context.Render.IPAddress, _parent.Context.Render.Port);
            if (participants != null)
            {
                NodeTaskStatus status = (NodeTaskStatus)participants[source];
                status.MapperStatus = MapperStatus.Failed;
            }
            if (_context.NCacheLog.IsInfoEnabled)
                _context.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").MappedFailed", "Mapper is failed on '" + source.IpAddress.ToString() + "'");
            this.Callback.Invoke(new CallbackResult(this, Notifications.TaskStatus.Failure, "Mapper Failed on " + source.IpAddress.ToString()));
            this.StopTask();
        }

        public void MapperCompleted(MapReduceOperation operation) {
            lock(mutex){
                Address source = operation.Source != null ? operation.Source : new Address(_parent.Context.Render.IPAddress, _parent.Context.Render.Port);
                if (participants != null) {
                    NodeTaskStatus status = (NodeTaskStatus) participants[source];
                    if (status != null) {
                        status.MapperStatus = MapperStatus.Completed;   
                    }
                }
                if (_context.NCacheLog.IsInfoEnabled)
                    _context.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").MapperCompleted", "Mapped is completed on '" + source.IpAddress.ToString() + "'");
                CheckMappersCompleted();
            }
        }

        private void CheckMappersCompleted()
        {
            bool mCompleted = true;
            if (participants != null)
            {
                IEnumerator it = participants.GetEnumerator();
                while (it.MoveNext())
                {
                    object entry = it.Current;
                    if (entry is DictionaryEntry)
                    {
                        if (((NodeTaskStatus)((DictionaryEntry)entry).Value).MapperStatus == MapperStatus.Completed)
                            continue;
                        else
                        {
                            mCompleted = false;
                            break;
                        }
                    }
                }
            }
            if (mCompleted)
            {
                if (ReducerConfigured)
                {
                    if (reducerTask != null)
                        reducerTask.SetMappersCompleted(mCompleted);
                }
                else
                {
                    if (_parent.NCacheLog != null)
                    {
                        if (_parent.NCacheLog.IsInfoEnabled)
                            _parent.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").TaskCompleted", "Task Completed, triggering callback.");
                    }
                    this.Callback.Invoke(new CallbackResult(this, Notifications.TaskStatus.Success, null));
                }
            }
        }

        public void EnqueueMapperOutput(OutputMap output) {
            if (CombinerConfigured) {
                {
                    IEnumerator it = output.MapperOutput.GetEnumerator();
                    while (it.MoveNext())
                    {
                        KeyValuePair<object, List<object>> curr = (KeyValuePair<object, List<object>>)it.Current;

                        IList mapValues = (IList)curr.Value;
                        foreach (object val in mapValues)
                        {
                            combinerTask.CombinerInputQueue.Enqueue(new DictionaryEntry(curr.Key, val));
                            throttlingMgr.IncrementChunkSize();
                        }
                        lock (combinerTask.CombinerInputQueue)
                        {
                            Monitor.PulseAll(combinerTask.CombinerInputQueue);
                        }
                    }
                }
            } else {
                lock(ReducerDataQueue) {
                    IEnumerator it = output.MapperOutput.GetEnumerator();
                    while (it.MoveNext()) 
                    {
                        KeyValuePair<object, List<object>> curr = (KeyValuePair<object, List<object>>)it.Current;

                        IList mapValues = (IList)curr.Value;
                        foreach (object val in mapValues)
                            ReducerDataQueue.Enqueue(new DictionaryEntry(curr.Key, val));
                    }                   
                }
            }
        }

        public void EnqueueReducerInput(Common.MapReduce.TaskOutputPair reducerInput)
        {
            lock (reducerTask.ReducerInputQueue)
            {
                reducerTask.ReducerInputQueue.Enqueue(new Alachisoft.NCache.Runtime.MapReduce.KeyValuePair(reducerInput.Key, reducerInput.Value));
                Monitor.PulseAll(reducerTask.ReducerInputQueue);
            }
        }
        


        public void LocalReducerFailed()
        {
            SendParticipantReducerFailedMessage();
        }

        public void LocalReducerCompleted()
        {
            SendParticipantReducerCompletedMessage();
        }

        private void SendParticipantReducerCompletedMessage()
        {
            MapReduceOperation operation = new MapReduceOperation();
            operation.TaskID = this.TaskId;
            operation.OpCode = MapReduceOpCodes.ReducerCompleted;

            try
            {
                if(!isLocal)
                    _parent.SendMapReduceOperation(new System.Collections.ArrayList(participants.Keys), operation);
            }
            catch (Exception ex)
            {
                _parent.Context.NCacheLog.Error("MapReduceTask(" + this.TaskId + ").SendParticipantReducerCompletedMessage", "Failed: " + ex.Message);
            }
            ReducerCompleted(operation);
        }

        private void SendParticipantReducerFailedMessage()
        {
            MapReduceOperation operation = new MapReduceOperation();
            operation.TaskID = this.TaskId;
            operation.OpCode = MapReduceOpCodes.ReducerFailed;

            try
            {
                if (!isLocal)
                    _parent.SendMapReduceOperation(new System.Collections.ArrayList(participants.Keys), operation);
            }
            catch (Exception ex)
            {
                _parent.Context.NCacheLog.Error("MapReduceTask(" + this.TaskId + ").SendParticipantReducerFailedMessage", "Failed: " + ex.Message);
            }
            ReducerFailed(operation);
        }

        public void ReducerCompleted(MapReduceOperation operation) {
            lock(mutex){
                Address source = operation.Source != null ? operation.Source : new Address(_parent.Context.Render.IPAddress, _parent.Context.Render.Port);
                if (participants != null) {
                    NodeTaskStatus status = (NodeTaskStatus) participants[source];
                    if(status!=null)
                    {
                        status.ReducerStatus = ReducerStatus.Completed;        
                    }                       
                }
                if(_context.NCacheLog.IsInfoEnabled)
                    _context.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").ReducerCompleted", "Reducer is completed on '" + source.IpAddress.ToString() + "'");
            
                CheckReducersCompleted();
            }
        }

        public void ReducerFailed(MapReduceOperation operation) {
            Address source = operation.Source != null ? operation.Source : new Address(_parent.Context.Render.IPAddress, _parent.Context.Render.Port);
            if (participants != null) {
                NodeTaskStatus status = (NodeTaskStatus) participants[source];
                status.ReducerStatus = ReducerStatus.Failed;
            }
            if (_context.NCacheLog.IsInfoEnabled)
                _context.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").ReducerFailed", "Reducer is failed on '" + source.IpAddress.ToString() + "'");            
            this.Callback.Invoke(new CallbackResult(this, Notifications.TaskStatus.Failure, "Reducer Failed on" + source.IpAddress.ToString()));
            this.StopTask();
        }
        
        void CheckReducersCompleted() {
            bool rCompleted = true;
            if(participants != null)
            {
                IEnumerator it=participants.GetEnumerator();
                while(it.MoveNext())
                {
                    object entry = it.Current;
                    if (((NodeTaskStatus)((DictionaryEntry)entry).Value).ReducerStatus == ReducerStatus.Completed)
                        continue;
                    else
                    {
                        rCompleted=false;
                        break;
                    }
                }
            }
            if (rCompleted)
            {
                if (_parent.NCacheLog != null)
                {
                    if (_parent.NCacheLog.IsInfoEnabled)
                        _parent.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").TaskCompleted", "Task Completed, triggering callback.");
                }
                this.Callback.Invoke(new CallbackResult(this, Notifications.TaskStatus.Success, null));
            }
        }

        public void PersistOutput(ClusteredArrayList output)
        {
            if (outputProvider != null)
            {
                if (_parent.NCacheLog.IsInfoEnabled)
                    _parent.NCacheLog.Info("MapReduceTask(" + this.TaskId + ").PersistOutput", "output persisted. Count: " + (output).Count);

                if (outputOption == INMEMORY)
                    outputProvider.Persist(this.TaskId, new TaskOutput(this.TaskId, output, this.CallbackListeners));
            }
        }

        public void SendReducerData()
        { 
            TaskOutputPair entry = null;
            while (ReducerDataQueue.Count > 0) {
                try {
                    lock(ReducerDataQueue) {
                        object ent = ReducerDataQueue.Dequeue();
                        if(ent is DictionaryEntry)
                            entry = new TaskOutputPair(((DictionaryEntry)ent).Key, ((DictionaryEntry)ent).Value);
                    }
                    if(entry != null) {
                        if(!ReducerConfigured)
                        {
                            localOutput.Add(entry);
                        }
                        else 
                        {
                            MapReduceOperation op = new MapReduceOperation();
                            op.Data = entry;
                            op.OpCode = MapReduceOpCodes.ReceiveReducerData;
                            op.TaskID = this.TaskId;
#if !CLIENT
                            if (distributionMgr != null) 
                            {
                                Address target = distributionMgr.SelectNode((string)entry.Key, "");

                                if (target.Equals(((ClusterCacheBase)_parent).Cluster.LocalAddress))
                                    _parent.InternalCache.TaskOperationReceived(op); 
                                else
                                    _parent.SendMapReduceOperation(target, op);
                                
                            }
                            else
#endif
                                _parent.InternalCache.TaskOperationReceived(op);
                        }
                    }
                } catch (Exception ex) {
                    _parent.Context.NCacheLog.Error("MapReduceTask(" + this.TaskId + ").SendToReducers", "Exception: " + ex.Message);
                }
                //Get Target Node check if local enqueue into reducer queue otherwise send to target node
            }
        }


        public Runtime.MapReduce.TaskStatus TaskStatus
        {
            get
            {
                Runtime.MapReduce.TaskStatus taskStatus = null;
                if (participants != null)
                {
                    IEnumerator it = participants.GetEnumerator();
                    while (it.MoveNext())
                    {
                        DictionaryEntry entry = (DictionaryEntry)it.Current;
                        Address address = (Address)entry.Key;
                        NodeTaskStatus status = (NodeTaskStatus)entry.Value;

                        if (status.MapperStatus == MapperStatus.Running)
                        {
                            taskStatus = new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.InProgress);
                            break;
                        }
                        else if (status.MapperStatus == MapperStatus.Failed)
                        {
                            taskStatus = new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.Failed);
                            break;
                        }
                        else if (status.MapperStatus == MapperStatus.Completed)
                        {
                            taskStatus = new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.InProgress);
                        }

                        if (status.ReducerStatus == ReducerStatus.Running)
                        {
                            taskStatus = new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.InProgress);
                            break;
                        }
                        else if (status.ReducerStatus == ReducerStatus.Failed)
                        {
                            taskStatus = new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.Failed);
                            break;
                        }
                        else if (status.ReducerStatus == ReducerStatus.Completed)
                        {
                            taskStatus = new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.Completed);
                        }

                        if (taskStatus == null)
                        {
                            taskStatus = new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.Waiting);
                        }
                    }
                }
                return taskStatus;
            }
        }


        /////////////////////////////////////////////////////////////
        ///////////      Throttling Management      ////////////////
        ///////////////////////////////////////////////////////////
        class MapReduceThrottlingManager
        {
            private int size;
            private int chunkSize;
            private MapReduceTask t;

            public MapReduceThrottlingManager(int chunkSize, MapReduceTask tt)
            {
                this.chunkSize = chunkSize;
                this.t = tt;
            }

            public void IncrementChunkSize()
            {
                if (++size >= chunkSize)
                {
                    t.Throttle();
                    this.size = 0;
                }
            }

            public void IncrementChunkSizeBy(int chunkSize)
            {
                this.size += size;
                if (this.size >= chunkSize)
                {
                    t.Throttle();
                    this.size = 0;
                }
            }

        }

    }
}

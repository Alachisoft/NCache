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
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Net;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.MapReduce;
using Alachisoft.NCache.MapReduce.OutputProviders;
using Alachisoft.NCache.Caching.Topologies;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.MapReduce.Notifications;


namespace Alachisoft.NCache.MapReduce
{
    internal class TaskTracker : IDisposable
    {
        private int _chunkSize = 100;
        private int _maxTasks = 10;
        private bool _communicateStats = false;
        private int _queueSize = 10;
        private int _maxExceptions = 10;

        private CacheRuntimeContext _context = null;

        private Address clusterAddress;
        private Address clientAddress;

        private Hashtable submittedTasks = null;
        private Hashtable runningTasks = null;
        private Hashtable waitingTasks = null;
        private List<string> cancelledTask = null;

        public event TaskCallback Callback = null;

        private long sequenceId = 0;
        private Dictionary<long, string> taskSequenceList = null;
        private int lastRunningTaskId = 0;

        private TaskOutputStore taskOutputStore = null;

        private object _lock = new object();


        public TaskTracker(IDictionary properties, CacheRuntimeContext context, TaskOutputStore store)
        {

            this.submittedTasks = new Hashtable();
            this.runningTasks = new Hashtable();
            this.waitingTasks = new Hashtable();

            this.cancelledTask = new List<string>();

            this._context = context;
            this.taskSequenceList = new Dictionary<long, string>();
            taskOutputStore = store;
            this.Callback += new TaskCallback(OnTaskCallback);

            if (_context.PerfStatsColl != null)
            {
                this._context.PerfStatsColl.RunningTasksCount(0);
                this._context.PerfStatsColl.WaitingTasksCount(0);
            }
            Initialize(properties);
        }

        private bool SlotAvailable
        {
            get { return runningTasks.Count < MaxTasks; }
        }

        private void Initialize(IDictionary properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");

        }

        #region Public Properties

        /// <summary>
        /// Maximum number of avoidable exception during the task.
        /// </summary>
        public int MaxExceptions
        {
            get { return _maxExceptions; }
            set { _maxExceptions = value; }
        }

        /// <summary>
        /// the ChunkSize
        /// </summary>
        public int ChunkSize
        {
            get { return _chunkSize; }
            set { _chunkSize = value; }
        }

        /// <summary>
        /// Maximum number of running tasks at a time.
        /// </summary>
        public int MaxTasks
        {
            get { return _maxTasks; }
            set { _maxTasks = value; }
        }

        /// <summary>
        /// Statistics communication check.
        /// </summary>
        public bool CommunicateStats
        {
            get { return _communicateStats; }
            set { _communicateStats = value; }
        }

        /// <summary>
        /// Maximum number of tasks waiting to be run.
        /// </summary>
        public int QueueSize
        {
            get { return _queueSize; }
            set { _queueSize = value; }
        }

        #endregion

        public void Dispose()
        {
            CancelAllTasks(false);
            this.cancelledTask.Clear();
        }

        public void OnTaskCallback(CallbackResult callbackInfo)
        {
            if (callbackInfo != null)
            {
                TaskBase task = callbackInfo.Task;
                if (runningTasks.ContainsKey(task.TaskId))
                {
                    if (_context.CacheImpl != null && _context.CacheInternal != null)
                    {
                        EventContext eventContext = new EventContext();
                        eventContext.TaskStatus = callbackInfo.Status;
                        eventContext.TaskFailureReason = callbackInfo.FailureReason;
                        _context.CacheInternal.NotifyTaskCallback(task.TaskId, task.CallbackListeners, false, null, eventContext);
                    }

                    TaskBase runningT = (TaskBase)runningTasks[task.TaskId];
                    if(runningT != null)
                        runningT.StopTask();

                    runningTasks.Remove(task.TaskId);
                    // decrementing running tasks
                    if (_context.PerfStatsColl != null)
                        _context.PerfStatsColl.DecrementRunningTasks();

                    // Additional set of counter
                    if (runningTasks.Count == 0 && _context.PerfStatsColl != null)
                        _context.PerfStatsColl.RunningTasksCount(0);
                    
                    if (taskSequenceList.Count > 0)
                    {
                        KeyValuePair<long, string> entry = new KeyValuePair<long, string>();
                        try
                        {
                            foreach (KeyValuePair<long, string> kvp in taskSequenceList)
                            {
                                entry = kvp;
                                break;
                            }


                            if (!entry.Key.Equals(null) && !entry.Value.Equals(null))
                            {
                                TaskBase t = (TaskBase)waitingTasks[entry.Value];
                                if (t != null)
                                    t.StartTask();
                                if (!runningTasks.ContainsKey(t.TaskId))
                                    runningTasks.Add(t.TaskId, t);
                                // incrementing running tasks
                                if (_context.PerfStatsColl != null)
                                    _context.PerfStatsColl.IncrementRunningTasks();
                                lastRunningTaskId++;

                                // decrementing waiting tasks.
                                if (_context.PerfStatsColl != null)
                                    _context.PerfStatsColl.DecrementWaitingTasks();
                                taskSequenceList.Remove(entry.Key); // remove the entry
                                waitingTasks.Remove(t.TaskId);

                                if (waitingTasks.Count == 0)
                                {
                                    if (_context.PerfStatsColl != null)
                                        _context.PerfStatsColl.WaitingTasksCount(0);
                                }
                            }
                        }
                        catch (Exception ex) { }
                    }
                }
            }

        }

        public object TaskOperationRecieved(MapReduceOperation operation)
        {
            MapReduceOpCodes opCode = operation.OpCode;
            switch (opCode)
            {
                case MapReduceOpCodes.GetTaskSequence:
                    return GetTaskSequence();
                case MapReduceOpCodes.SubmitMapReduceTask:
                    return SubmitMapReduceTask(operation);
                case MapReduceOpCodes.StartTask:
                    return StartMapReduceTask(operation);
                case MapReduceOpCodes.CancelTask:
                    return CancelTask(operation);
                case MapReduceOpCodes.CancellAllTasks:
                    return CancelAllTasks(true);
                case MapReduceOpCodes.GetRunningTasks:
                    return GetRunningTasks();
                case MapReduceOpCodes.GetTaskEnumerator:
                    return GetTaskEnumerator(operation);
                case MapReduceOpCodes.GetNextRecord:
                    return NextRecord(operation);
                case MapReduceOpCodes.RegisterTaskNotification:
                    return RegisterTaskNotification(operation);
                case MapReduceOpCodes.UnregisterTaskNotification:
                    return UnregisterTaskNotification(operation);
                case MapReduceOpCodes.GetTaskStatus:
                    return GetTaskProgress(operation);
                case MapReduceOpCodes.RemoveFromRunningList:
                    return RemoveFromRunning(operation);
                case MapReduceOpCodes.RemoveFromSubmittedList:
                    return RemoveFromSubmitted(operation);
                case MapReduceOpCodes.MapperCompleted:
                    return MapperCompleted(operation);
                case MapReduceOpCodes.MapperFailed:
                    return MapperFailed(operation);
                case MapReduceOpCodes.ReducerCompleted:
                    return ReducerCompleted(operation);
                case MapReduceOpCodes.ReducerFailed:
                    return ReducerFailed(operation);
                case MapReduceOpCodes.ReceiveReducerData:
                    return RecievedReducerData(operation);


            }
            return null;
        }
        

        private object GetTaskSequence()
        {
            return ++sequenceId;
        }

        private TaskExecutionStatus SubmitTask(TaskBase task)
        {
            if (_context.NCacheLog.IsInfoEnabled)
                _context.NCacheLog.Info("TaskTracker.SubmitTask", "Task with TaskId '" + task.TaskId + "' is submitted successfully.");

            lock (_lock)
            {
                if (submittedTasks != null)
                {
                    if(!submittedTasks.ContainsKey(task.TaskId))
                        submittedTasks.Add(task.TaskId, task);
                    return TaskExecutionStatus.Submitted;
                }
                else
                {
                    return TaskExecutionStatus.Failure;
                }
            }
        }

        private object SubmitMapReduceTask(MapReduceOperation operation)
        {
            try
            {
                _context.NCacheLog.CriticalInfo("MapReduce.TaskTracker ",
                    "MapReduce task with taskId '" + operation.TaskID + "' is submitted");

                CacheBase p = (CacheBase)((_context.CacheImpl != null && _context.CacheImpl is CacheBase) ? _context.CacheImpl : null);

                Runtime.MapReduce.MapReduceTask userTask = (Runtime.MapReduce.MapReduceTask)operation.Data;
                Filter filter = operation.Filter;

                if (userTask != null)
                {
                    MapReduceTask serverTask = new MapReduceTask(p, Callback, userTask.Mapper,
                                                    userTask.Combiner, userTask.Reducer, 
                                                    userTask.InputProvider == null ? new InputProviders.CacheInputProvider(p.InternalCache, filter != null ? filter.QueryFilter : null, this._context) : userTask.InputProvider, 
                                                    new InMemoryOutpuProvider(taskOutputStore),
                                                    filter,
                                                    _context, this.ChunkSize, this.MaxExceptions);
                    serverTask.TaskId = operation.TaskID;
                    serverTask.AddTaskCallbackInfo(operation.CallbackInfo);
                    return this.SubmitTask(serverTask);
                }
            }
            catch (Exception ex)
            {
                throw new OperationFailedException(ex.Message);
            }
            return null;
        }

        private TaskExecutionStatus StartTask(string taskId, long sequenceId)
        {
            lock (_lock)
            {
                TaskBase task = (TaskBase)submittedTasks[taskId];
                if (task != null)
                {
                    submittedTasks.Remove(taskId);
                    if (SlotAvailable)
                    {
                        if (sequenceId == lastRunningTaskId + 1)
                        {
                            try
                            {
                                if(!runningTasks.ContainsKey(task.TaskId))
                                    runningTasks.Add(task.TaskId, task);
                                if(_context.PerfStatsColl != null)
                                    _context.PerfStatsColl.IncrementRunningTasks();
                                lastRunningTaskId++;
                                task.StartTask();
                            }
                            catch (Exception exx)
                            {
                                task.StopTask();
                                task.Dispose();
                                if (runningTasks.ContainsKey(task.TaskId))
                                {
                                    runningTasks.Remove(task.TaskId);
                                    if (_context.PerfStatsColl != null)
                                        _context.PerfStatsColl.DecrementRunningTasks();
                                }
                                throw new OperationFailedException(exx.Message);
                            }

                            while (taskSequenceList.Count != 0 && SlotAvailable)
                            {
                                long seqID = 0;
                                foreach (long key in taskSequenceList.Keys)
                                {
                                    seqID = key;
                                    break;
                                }

                                if (seqID == lastRunningTaskId + 1)
                                {
                                    if (waitingTasks.Count > 0)
                                    {
                                        TaskBase t = (TaskBase)waitingTasks[taskSequenceList[seqID]];
                                        try
                                        {
                                            if(!runningTasks.ContainsKey(t.TaskId))
                                                runningTasks.Add(t.TaskId, t);
                                            if (_context.PerfStatsColl != null)
                                                _context.PerfStatsColl.IncrementRunningTasks();
                                            lastRunningTaskId++;
                                            taskSequenceList.Remove(seqID);
                                            waitingTasks.Remove(t.TaskId);
                                            if (_context.PerfStatsColl != null)
                                                _context.PerfStatsColl.DecrementWaitingTasks();

                                            t.StartTask();

                                        }
                                        catch (Exception exx)
                                        {
                                            t.StopTask();
                                            if (runningTasks.ContainsKey(t.TaskId))
                                            {
                                                runningTasks.Remove(t.TaskId);
                                                if (_context.PerfStatsColl != null)
                                                    _context.PerfStatsColl.DecrementRunningTasks();
                                            }
                                            throw new OperationFailedException(exx.Message);
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            return TaskExecutionStatus.Running;
                        }
                        else
                        {
                            taskSequenceList.Add(sequenceId, task.TaskId);
                            if(!waitingTasks.ContainsKey(task.TaskId))
                                waitingTasks.Add(task.TaskId, task);
                            if (_context.PerfStatsColl != null)
                                _context.PerfStatsColl.IncrementWaitingTasks();
                            if (_context.NCacheLog.IsInfoEnabled)
                                _context.NCacheLog.Info("TaskTracker.StartTask", "MapReduce task with task ID '" + task.TaskId.ToUpper() + "' is in the waiting queue.");
                            return TaskExecutionStatus.Waiting;
                        }
                    }
                    else if (taskSequenceList.Count < _queueSize)
                    {
                        taskSequenceList.Add(sequenceId, task.TaskId);
                        if(!waitingTasks.ContainsKey(task.TaskId))
                            waitingTasks.Add(task.TaskId, task);
                        if (_context.PerfStatsColl != null)
                            _context.PerfStatsColl.IncrementWaitingTasks();
                        if (_context.NCacheLog.IsInfoEnabled)
                            _context.NCacheLog.Info("TaskTracker.StartTask", "MapReduce task with task ID '" + task.TaskId.ToUpper() + "' is in the waiting queue.");
                        return TaskExecutionStatus.Waiting;
                    }
                    else
                    {
                        throw new Exception("No more task can be submitted");
                    }
                }
            }
            return TaskExecutionStatus.Failure;
        }

        private object StartMapReduceTask(MapReduceOperation operation)
        {
            return StartTask(operation.TaskID, operation.SequenceID);
        }

        private object CancelTask(MapReduceOperation operation)
        {
            string taskID = (string)operation.TaskID;
            lock (_lock)
            {
                if (runningTasks.ContainsKey(taskID))
                {
                    TaskBase t = (TaskBase)runningTasks[taskID];
                    t.StopTask();
                    t.Dispose();

                    this.cancelledTask.Add(taskID);

                    if (_context.CacheImpl != null && _context.CacheInternal != null)
                    {
                        EventContext eventContext = new EventContext();
                        eventContext.TaskStatus = TaskStatus.Cancelled;
                        eventContext.TaskFailureReason = "Task was Cancelled by user.";
                        _context.CacheInternal.NotifyTaskCallback(t.TaskId, t.CallbackListeners, false, null, eventContext);
                    }
                    runningTasks.Remove(taskID);
                    if (_context.PerfStatsColl != null)
                        _context.PerfStatsColl.DecrementRunningTasks();
                    if (_context.NCacheLog.IsInfoEnabled)
                        _context.NCacheLog.Info("TaskTracker.CancelTask", "Task with task ID '" + taskID.ToUpper() + "' has been cancelled.");
                }
                else
                {
                    _context.NCacheLog.Error("TaskTracker.CancelTask", "Task with task ID '" + taskID.ToUpper() + "' does not exist.");
                }
            }
            return true;
        }

        private object CancelAllTasks(bool topologyChanged)
        {
            //Stop and Remove all running/waiting tasks
            lock (_lock)
            {
                foreach (object taskID in runningTasks.Keys)
                {
                    TaskBase t = (TaskBase)runningTasks[taskID];
                    if (t != null)
                    {
                        t.StopTask();
                        t.Dispose();

                        this.cancelledTask.Add(taskID.ToString());

                        if (_context.CacheImpl != null && _context.CacheInternal != null)
                        {
                            EventContext eventContext = new EventContext();
                            eventContext.TaskStatus = TaskStatus.Cancelled;
                            if (topologyChanged)
                                eventContext.TaskFailureReason = "Task was cancelled because Toplogy has been change.";
                            _context.CacheInternal.NotifyTaskCallback(t.TaskId, t.CallbackListeners, false, null, eventContext);
                        }

                    }
                }
            }

            runningTasks.Clear();
            if(_context.PerfStatsColl != null)
                _context.PerfStatsColl.RunningTasksCount(0);

            lock (_lock)
            {
                foreach (object taskID in waitingTasks.Keys)
                {
                    TaskBase t = (TaskBase)waitingTasks[taskID];
                    if (t != null)
                    {
                        t.StopTask();
                        t.Dispose();

                        if (_context.CacheImpl != null && _context.CacheInternal != null)
                        {
                            EventContext eventContext = new EventContext();
                            eventContext.TaskStatus = TaskStatus.Cancelled;
                            if (topologyChanged)
                                eventContext.TaskFailureReason = "Task was cancelled because of Toplogy change.";
                            _context.CacheInternal.NotifyTaskCallback(t.TaskId, t.CallbackListeners, false, null, eventContext);
                        }
                    }
                }
            }
            waitingTasks.Clear();

            if(_context.PerfStatsColl != null)
                _context.PerfStatsColl.WaitingTasksCount(0);
            //Reset the sequenceID and lastRunningID

            sequenceId = 0;
            lastRunningTaskId = 0;

            if (taskOutputStore != null)
                taskOutputStore.Dispose();

            return true;
        }


        private object MapperCompleted(MapReduceOperation operation) {
            lock (_lock) {
                string taskID = (string) operation.TaskID;
                if (runningTasks.ContainsKey(taskID)) {
                    MapReduceTask t = (MapReduceTask) runningTasks[taskID];
                    t.MapperCompleted(operation);
                }
                return true;
            }
        }

        private Object MapperFailed(MapReduceOperation operation)
        {
            string taskID = (string)operation.TaskID;
            if (runningTasks.ContainsKey(taskID))
            {
                MapReduceTask t = (MapReduceTask)runningTasks[taskID];
                t.MapperFailed(operation);
            }
            return true;

        }

        private object RecievedReducerData(MapReduceOperation operation) {
            string taskID = (string) operation.TaskID;
            if (runningTasks.ContainsKey(taskID)) {
                MapReduceTask t = (MapReduceTask) runningTasks[taskID];
                t.EnqueueReducerInput((Common.MapReduce.TaskOutputPair)operation.Data);
            } else {
                if(_context.NCacheLog.IsInfoEnabled)
                    _context.NCacheLog.Info("TaskTracker.recievedReducerData", "task does not exist in running tasks " + taskID);
            }
            return true;
        }

        private object ReducerCompleted(MapReduceOperation operation) {
            lock (_lock) {
                string taskID = (string) operation.TaskID;
                if (runningTasks.ContainsKey(taskID)) {
                    MapReduceTask t = (MapReduceTask) runningTasks[taskID];
                    t.ReducerCompleted(operation);
                }
                return true;
            }
        }
        
        private Object ReducerFailed(MapReduceOperation operation) {
            string taskID = (string) operation.TaskID;
            if (runningTasks.ContainsKey(taskID)) {
                MapReduceTask t = (MapReduceTask) runningTasks[taskID];
                t.ReducerFailed(operation);
            }
            return true;

        }


        private object RegisterTaskNotification(MapReduceOperation operation)
        {
            string taskID = (string)operation.TaskID;
            if (taskID == null || string.IsNullOrEmpty(taskID))
            {
                throw new OperationFailedException("Task can not be null or empty");
            }

            TaskBase t = null;
            if (runningTasks.ContainsKey(taskID))
            {
                t = (TaskBase)runningTasks[taskID];
                t.AddTaskCallbackInfo((TaskCallbackInfo)operation.CallbackInfo);
            }
            else if (waitingTasks.ContainsKey(taskID))
            {
                t = (TaskBase)waitingTasks[taskID];
                t.AddTaskCallbackInfo((TaskCallbackInfo)operation.CallbackInfo);
            }
            else if (taskOutputStore != null && taskOutputStore.TaskOutputExists(taskID))
            {
                //Put listener info into taskoutput and then call notify client async
                TaskOutput output = taskOutputStore.TaskOutput(taskID);
                if(output != null)
                    output.AddToListeners(operation.CallbackInfo);

                if (_context.CacheImpl != null && _context.CacheInternal != null)
                {
                    EventContext eventContext = new EventContext();
                    eventContext.TaskStatus = TaskStatus.Success;

                    IList listeners=new ArrayList();
                    listeners.Add((TaskCallbackInfo)operation.CallbackInfo);

                    _context.CacheInternal.NotifyTaskCallback(taskID,listeners, true, null, eventContext);
                }
            }
            else if (this.cancelledTask.Contains(taskID))
            {
                throw new OperationFailedException("Task with Specified TaskId was cancelled.");
            }
            else
            {
                throw new OperationFailedException("Task with Specified Task id does not exists");
            }

            return true;
        }

        private object UnregisterTaskNotification(MapReduceOperation operation)
        {
            String taskID = (String)operation.TaskID;
            if (taskID == null || string.IsNullOrEmpty(taskID))
            {
                throw new OperationFailedException("Task can not be null or empty");
            }

            TaskBase t = null;
            if (runningTasks.ContainsKey(taskID))
            {
                t = (TaskBase)runningTasks[taskID];
                t.RemoveTaskCallbackInfo((TaskCallbackInfo)operation.CallbackInfo);
            }
            else if (waitingTasks.ContainsKey(taskID))
            {
                t = (TaskBase)waitingTasks[taskID];
                t.RemoveTaskCallbackInfo((TaskCallbackInfo)operation.CallbackInfo);
            }
            else if (taskOutputStore != null && taskOutputStore.TaskOutputExists(taskID))
            {
                //remove listener info from taskoutput
                TaskOutput output = taskOutputStore.TaskOutput(taskID);
                if(output != null)
                    output.RemoveFromListeners(operation.CallbackInfo);
            }
            else if (this.cancelledTask.Contains(taskID))
            {
                throw new OperationFailedException("Task with Specified TaskId was cancelled.");
            }
            else
            {
                throw new OperationFailedException("Task with Specified Task id does not exists");
            }
            
            return true;
        }

        private object GetRunningTasks()
        {
            return new ArrayList(runningTasks.Keys);
        }

        private object GetTaskProgress(MapReduceOperation operation) {
            String taskId = (String) operation.TaskID;
            
            if (waitingTasks.ContainsKey(taskId))
            {
                return new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.Waiting);
            }
            else if (taskOutputStore.TaskOutputExists(taskId))
            {
                return new Runtime.MapReduce.TaskStatus(Runtime.MapReduce.TaskStatus.Status.Completed);
            }
            if (runningTasks.ContainsKey(taskId)) {
                Task t = (Task) runningTasks[taskId];
                MapReduceTask mrt = (MapReduceTask) t;
                return mrt.TaskStatus;
            }
            else
            {
                if(this.cancelledTask.Contains(taskId))
                    throw new OperationFailedException("Task with specified task ID was Cancelled.");
                else
                    throw new OperationFailedException("Task with specified task ID does not exist.");
            }
        }

        private object GetTaskEnumerator(MapReduceOperation operation) {
            TaskEnumeratorPointer pointer = (TaskEnumeratorPointer) operation.Data;
            if (clientAddress == null) {
                try {
                    if (_context.Render != null) {
                        clientAddress = new Address(_context.Render.IPAddress, _context.Render.Port);
                    }
                } catch (Exception ex) {
                    throw new InvalidTaskEnumeratorException(ex.Message, ex);
                }
            }
            pointer.ClientAddress = clientAddress;

            if (clusterAddress == null)
            {
#if !CLIENT
                if(_context.CacheImpl is ClusterCacheBase)
                    clusterAddress = ((ClusterCacheBase)_context.CacheImpl).Cluster.LocalAddress;
                else if(_context.CacheImpl is LocalCacheImpl)
                    clusterAddress = new Address(_context.Render.IPAddress, _context.Render.Port);
#else
                clusterAddress = new Address(_context.Render.IPAddress, _context.Render.Port);
#endif
            }

            pointer.ClusterAddress = clusterAddress;

            // Task Cancellation check.
            if (this.cancelledTask.Contains(pointer.TaskId))
            {
                throw new OperationFailedException("Task with specified Task ID was cancelled");
            }

            TaskEnumeratorResult result = null;
            if (taskOutputStore != null) {
                try {
                    result = taskOutputStore.GetTaskEnumerator(pointer);
                    //result.NodeAddress = _context.Render.IPAddress.ToString();
                    result.Pointer = pointer;
                } catch (InvalidTaskEnumeratorException ex) {
                    _context.NCacheLog.Error("TaskTracker.GetTaskEnumerator", ex.Message);
                    throw ex;
                }
            }

            if(_context.NCacheLog.IsInfoEnabled) {
                _context.NCacheLog.Info("TaskTracker.GetTaskEnumerator", "Task Enumerator provided to client.");
            }
            
            return result;
        }

        private Object NextRecord(MapReduceOperation operation) {
            TaskEnumeratorPointer pointer = (TaskEnumeratorPointer) operation.Data;

            // Task cancellation check
            if (this.cancelledTask.Contains(pointer.TaskId))
                throw new OperationFailedException("Task with specified Task ID was Cancelled.");

            TaskEnumeratorResult result = null;
            if (taskOutputStore != null)
            {
                try {
                    result = taskOutputStore.NextRecord(pointer);
                    result.NodeAddress = _context.Render.IPAddress.ToString();
                } catch (InvalidTaskEnumeratorException ex) {
                    _context.NCacheLog.Error("TaskTracker.GetTaskEnumerator", ex.Message);
                    throw ex;
                }
            }
            return result;
        }

        public void RemoveDeadClientsTasks(ArrayList clients) {
            //Locking with starttask
            lock (_lock) {
                //Removing callback entries of dead clients from waiting tasks
                ICollection waitingTaskList = waitingTasks.Keys;
                for (IEnumerator it = clients.GetEnumerator(); it.MoveNext();) {
                    string client = (string) it.Current;
                    if(_context.NCacheLog.IsInfoEnabled)
                        _context.NCacheLog.Info("TaskTracker.RemoveDeadClients", "Removing waiting task listeners for client " + client);
                    foreach (string taskId in waitingTaskList) {
                        TaskBase t = (TaskBase) waitingTasks[taskId];
                        if (t != null && t.CallbackListeners.Count != 0) {
                            for (IEnumerator cbit = t.CallbackListeners.GetEnumerator(); cbit.MoveNext();) {
                                TaskCallbackInfo taskCallbackInfo = (TaskCallbackInfo) cbit.Current;
                                if (taskCallbackInfo.Client.Equals(client)) {
                                    t.CallbackListeners.Remove(taskCallbackInfo);
                                }
                            }
                            if (t.CallbackListeners.Count == 0) {
                                if(_context.NCacheLog.IsInfoEnabled)
                                    _context.NCacheLog.Info("TaskTracker.RemoveDeadClients", "No listeners remaining therefore removing waiting task " + t.TaskId);
                                waitingTasks.Remove(t.TaskId);
                                if(_context.PerfStatsColl != null)
                                    _context.PerfStatsColl.DecrementWaitingTasks();
                            }
                        }
                    }
                }
                //Removing callback entries of dead clients from running tasks
                ICollection runningTaskList = runningTasks.Keys;

                for (IEnumerator it = clients.GetEnumerator(); it.MoveNext();) {
                    string client = (string) it.Current;
                    if(_context.NCacheLog.IsInfoEnabled)
                        _context.NCacheLog.Info("TaskTracker.RemoveDeadClients", "Removing running task listeners for client " + client);
                    foreach (string taskId in runningTaskList) {
                        TaskBase t = (TaskBase) runningTasks[taskId];
                        if (t != null && t.CallbackListeners.Count != 0) {
                            for (IEnumerator cbit = t.CallbackListeners.GetEnumerator(); cbit.MoveNext();) {
                                TaskCallbackInfo taskCallbackInfo = (TaskCallbackInfo) cbit.Current;
                                if (taskCallbackInfo.Client.Equals(client)) {
                                    t.CallbackListeners.Remove(taskCallbackInfo);
                                }
                            }
                            if (t.CallbackListeners.Count == 0) {
                                if(_context.NCacheLog.IsInfoEnabled)
                                    _context.NCacheLog.Info("TaskTracker.RemoveDeadClients", "No listeners remaining therefore removing running task " + t.TaskId);
                                waitingTasks.Remove(t.TaskId);
                                if (_context.PerfStatsColl != null)
                                    _context.PerfStatsColl.DecrementWaitingTasks();
                            }
                        }
                    }
                }
            }
            //Remove Iterators from the task output list
            //Remove Listeners for these clients     
        }

        public object RemoveFromSubmitted(MapReduceOperation operation)
        {
            try
            {
                if(this.submittedTasks.ContainsKey(operation.TaskID))
                    this.submittedTasks.Remove(operation.TaskID);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public object RemoveFromRunning(MapReduceOperation operation)
        {
            try
            {
                if (this.runningTasks.ContainsKey(operation.TaskID))
                {
                    this.runningTasks.Remove(operation.TaskID);
                    if (_context.PerfStatsColl != null)
                        _context.PerfStatsColl.DecrementRunningTasks();
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

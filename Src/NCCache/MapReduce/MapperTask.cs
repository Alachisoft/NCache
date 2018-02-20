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
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.MapReduce;
using System.Threading;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Collections;
using Alachisoft.NCache.MapReduce;

namespace Alachisoft.NCache.MapReduce
{
    internal class MapperTask : Task, Alachisoft.NCache.Common.Threading.IThreadRunnable
    {
        Thread mapperThread;
        private IMapper mapper;
        private volatile bool isAlive = true;
        private bool mapFinished = false;
        private MapReduceTask parent;
        private MapReduceInput inputProvider;
        private IKeyFilter keyFilter;
        private long mappedCount = 0;

        public long MappedCount
        {
            get { return mappedCount; }
            set { mappedCount = value; }
        }

        public MapperTask(IMapper mapper, MapReduceInput input, IKeyFilter filter, MapReduceTask prnt)
        {
            this.mapper = mapper;
            this.parent = prnt;
            this.inputProvider = input;
            this.keyFilter = filter;
        }
        
        public void Run()
        {
            try
            {
                if (parent.Context.NCacheLog != null)
                {
                    if (parent.Context.NCacheLog.IsInfoEnabled)
                        parent.Context.NCacheLog.Info("MapperTask(" + parent.TaskId + ").Start ", "Mapper task is started.");
                }
                bool completedSuccessfully = true;
                while (isAlive && inputProvider.MoveNext())
                {
                    try
                    {
                        object el = inputProvider.Entry;

                        if (el != null)
                        {

                            DictionaryEntry element = (DictionaryEntry)el;
                            if (keyFilter != null && !keyFilter.FilterKey(element.Key))
                            {
                                continue;
                            }

                            OutputMap output = new OutputMap();
                            // Real Map Method call.
                            mapper.Map(element.Key, element.Value, output);
                            this.MappedCount = MappedCount + 1;
                            if (parent.Context.PerfStatsColl != null)
                                parent.Context.PerfStatsColl.IncrementMappedPerSecRate();
                            parent.EnqueueMapperOutput(output);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (parent.ExceptionCount < parent.MaxExceptions)
                        {
                            if (parent.Context.NCacheLog != null)
                                parent.Context.NCacheLog.Error("MapperTask(" + parent.TaskId + ").Run", " Exception : " + ex.Message);
                            parent.ExceptionCount = parent.ExceptionCount + 1;
                        }
                        else
                        {
                            completedSuccessfully = false;
                            parent.LocalMapperFailed();
                            break;
                        }
                    }
                }
                if (completedSuccessfully && isAlive)
                {
                    parent.LocalMapperCompleted();
                    mapper.Dispose();

                }
            }
            catch (Exception ex)
            {
                try
                {
                    parent.LocalMapperFailed();
                }
                catch (Exception)
                {
                }
            }
        }
        
        public override void StartTask()
        {
            try
            {
                PrepareInput();
                mapperThread = new Thread(new ThreadStart(Run));
                mapperThread.Start();
            }
            catch (Exception ex)
            {
                if (parent.Context != null)
                    parent.Context.NCacheLog.Error("MapperTask (" + parent.TaskId + ").Start ", "Error: " + ex.Message);
                throw new OperationFailedException("Error: " + ex.Message);
            }
        }

        private void PrepareInput()
        {
            if (inputProvider == null)
                throw new ArgumentException("Input Provider Cannot be null.");

            Hashtable table = new Hashtable();
            table.Add("CacheName", parent.Context.CacheRoot.Name);
            table.Add("NodeAddress", parent.Context.Render.IPAddress.ToString() + ":" + parent.Context.Render.Port);
            inputProvider.Initialize(table);
            inputProvider.LoadInput();
        }

        public override void StopTask()
        {
            if (mapperThread != null)
            {
                try
                {
                    isAlive = false;
#if !NETCORE
                    mapperThread.Abort();
#else
                    mapperThread.Interrupt();
#endif

                    mapperThread = null;
                }
                catch (Exception ex) { }
            }
        }

        
    }
}

//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    public class PowerShellAdapter
    {
        private StresstestBase Cmdlet { get; set; }
        private BlockingCollection<object> Queue { get; set; }
        private object LockToken { get; set; }
        public bool Finished { get; set; }
        public bool TerminateThreads { get; set; }

        public PowerShellAdapter (StresstestBase cmdlet)
        {
            this.Cmdlet = cmdlet;
            this.LockToken = new object();
            this.Queue = new BlockingCollection<object>();
            this.Finished = false;
        }

        public void Listen()
        {
           
            while (!Finished)
            {
                while (Queue.Count > 0)
                {
                    try
                    {
                        lock (LockToken)
                            Cmdlet.OutputProvider.WriteLine(Queue.Take());
    
                    }
                    catch (Exception e)
                    {
                        if (e is PipelineStoppedException)
                        {
                            Finished = true;

                            break;
                        }
                        throw e;
                    }

                }
                if (TerminateThreads)
                {
                    //Cmdlet.OutputProvider.WriteLine("Too many errors. Aborting stress testing");
                    Cmdlet.StopProcess();
                    return;
                }
                Thread.Sleep(1000);
            }
        }

        public void WriteObject(object obj)
        {
            if (!TerminateThreads)
            {
                lock (LockToken)
                    Queue.Add(obj);
            }
        }
    }
}

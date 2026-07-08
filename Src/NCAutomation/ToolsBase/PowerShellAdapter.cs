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
           
            while (!Finished || Queue.Count > 0)
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

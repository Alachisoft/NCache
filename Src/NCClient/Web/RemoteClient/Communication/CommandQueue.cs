using Alachisoft.NCache.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Web.Communication
{
    internal class CommandQueue
    {
        private readonly Queue<CommandBase>
           regular = new Queue<CommandBase>();

        public object SyncLock => regular;

        public CommandBase Dequeue()
        {
            lock (regular)
            {
                if (regular.Count != 0)
                {
                    return regular.Dequeue();
                }
            }
            return null;
        }

        public bool Push(CommandBase command)
        {
            lock (regular)
            {
                regular.Enqueue(command);
                return regular.Count == 1;
            }
        }

        internal bool Any()
        {
            lock (regular)
            {
                return regular.Count != 0;
            }
        }

        internal int Count()
        {
            lock (regular)
            {
               return regular.Count;
            }
        }
    }
}

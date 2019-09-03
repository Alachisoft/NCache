using System;
using Alachisoft.NGroups.Stack;

namespace Alachisoft.NGroups.Util
{
    class ClusterEvent : IEvent
    {
        private Protocol processor = null;
        private Event eve=null;
        public ClusterEvent(Event e, Protocol proc)
        {
            this.eve = e;
            this.processor = proc;            
        }

        public void Process()
        {
            if (processor != null && eve!=null)
            {
                try
                {
                    processor.up(eve);
                }
                catch (Exception e)
                {
                }

            }
        }
    }
}

using System;

namespace Alachisoft.NCache.Common.Tracing.Tracer
{

    public class EventSource
    {
        public EventSource()
        {
        }
        public bool IsEnabled(EventLevel level, EventKeywords keywords)
        {
            throw new NotImplementedException();
        }

        protected void WriteEvent(int eventId, string arg1, string arg2)
        {
            throw new NotImplementedException();
        }

    }

}
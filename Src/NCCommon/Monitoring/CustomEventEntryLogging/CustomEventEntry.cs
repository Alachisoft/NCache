using System;
using System.Diagnostics;

namespace Alachisoft.NCache.Common.Monitoring.CustomEventEntryLogging
{
    public class CustomEventEntry
    {
        public DateTime TimeStamp { get; set; }
        public string Source { get; set; }
        public long EventId { get; set; }
        public EventLogEntryType Level { get; set; }
        public string Message { get; set; }
    }
}

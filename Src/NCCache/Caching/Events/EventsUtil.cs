using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Caching.Events
{
    internal static class EventsUtil
    {
        internal static EventTypeInternal GetEventTypeInternal(Runtime.Events.EventType eventType)
        {
            EventTypeInternal eventTypeInternal = EventTypeInternal.None;

            if ((eventType & EventType.ItemAdded) != 0)
                eventTypeInternal |= EventTypeInternal.ItemAdded;

            if ((eventType & EventType.ItemUpdated) != 0)
                eventTypeInternal |= EventTypeInternal.ItemUpdated;

            if ((eventType & EventType.ItemRemoved) != 0)
                eventTypeInternal |= EventTypeInternal.ItemRemoved;

            return eventTypeInternal;
        }
    }
}

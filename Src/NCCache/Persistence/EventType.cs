using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Persistence
{
    public enum EventType
    {
        ITEM_REMOVED_CALLBACK,
        ITEM_UPDATED_CALLBACK,
        ITEM_ADDED_EVENT,
        ITEM_REMOVED_EVENT,
        ITEM_UPDATED_EVENT,
        CACHE_CLEARED_EVENT,
        CQ_CALLBACK,
        POLL_REQUEST_EVENT,
        TASK_CALLBACK

    }

}

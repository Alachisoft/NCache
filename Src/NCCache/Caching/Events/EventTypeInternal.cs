using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Caching.Events
{
    /// <summary>
    /// NCache provides an EventType enum which specifies the type of event to be registered by the user. Event types are specified at the time of notification registration.
    /// </summary>
    /// <remarks>
    /// One or more event types can be specified. 
    /// </remarks>
    /// <example>
    /// To specify multiple events at the time of event registration you can use the following code:
    /// <code>
    /// cache.RegisterCacheNotification(key, onCacheDataModifiedCallback, EventType.ItemAdded || EventType.ItemUpdated, EventDataFilter.DataWithMetadata);
    /// </code>
    /// </example>
    [Serializable]
    [Flags]
    internal enum EventTypeInternal
    {
        /// <summary>
        /// internal used only 
        /// </summary>
        None = 0x000,
        /// <summary>
        /// User receives a notification when an item is added in cache.
        /// </summary>
        ItemAdded = 0x001,
        /// <summary>
        ///  when an item is updated in cache.
        /// </summary>
        ItemUpdated = 0x002,
        /// <summary>
        /// User receives a notification when an item is removed from cache
        /// </summary>
        ItemRemoved = 0x004,
        /// <summary>
        /// EventType.ClientCache is used for registering Client Cache Poll based notifications. This is an internally used event type.  
        /// </summary>
        ClientCache = 0x008,
        /// <summary>
        /// EventType.PubSub is used for pub-sub Poll based notifications. This is an internally used event type. 
        /// </summary>
        PubSub = 0x10
    }
}

using System;

namespace Alachisoft.NCache.Runtime.Enum
{
    /// <summary>
    /// An enum demonstrating the type of data contained by the Json class.
    /// </summary>
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public enum JsonDataType : byte
    {
        /// <summary>
        /// Contained data is null. This value is 
        /// actually just a counterpart of <see langword="null" />.
        /// </summary>
        Null,
        /// <summary>
        /// Contained data is of boolean type.
        /// </summary>
        Boolean,
        /// <summary>
        /// Contained data is of number type.
        /// </summary>
        Number,
        /// <summary>
        /// Contained data is of string type.
        /// </summary>
        String,
        /// <summary>
        /// Contained data is a Json Object class.
        /// </summary>
        Object,
        /// <summary>
        /// Contained data is a Json array.
        /// </summary>
        Array,
    }
}

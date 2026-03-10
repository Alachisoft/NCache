using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Runtime.JSON
{
    /// <summary>
    /// Makes a field or property searchable in DataType's item.
    /// If PrimaryField attribute is provided with any Field then only that Field will be Serialized and sent to NCache Server and search operation will only look up for that specific attribute on Server.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [Obsolete("This API is deprecated and will be removed in a future release. This feature is being retired and will not be continued in future versions.", false)]
    public class PrimaryField : System.Attribute
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public PrimaryField()
        {
        }
    }
}

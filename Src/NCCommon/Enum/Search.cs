using System;
using System.Collections.Generic;
using System.Text;

namespace Alachisoft.NCache.Common.Enum
{
    public enum Search
    {
        /// <summary>
        /// Search in local directory.
        /// </summary>
        LocalSearch = 0,
        /// <summary>
        /// Search in local config directory.
        /// </summary>
        LocalConfigSearch,
        /// <summary>
        /// Search in NCache installed config directory.
        /// </summary>
        GlobalSearch
    }
}

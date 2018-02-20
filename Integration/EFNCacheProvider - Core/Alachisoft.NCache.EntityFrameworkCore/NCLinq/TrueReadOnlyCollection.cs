using System.Collections.ObjectModel;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    internal sealed class TrueReadOnlyCollection<T> : ReadOnlyCollection<T>
    {
        /// <summary>
        /// Creates instance of TrueReadOnlyCollection, wrapping passed in array.
        /// !!! DOES NOT COPY THE ARRAY !!!
        /// </summary>
        public TrueReadOnlyCollection(params T[] list)
            : base(list)
        {
        }
    }
}

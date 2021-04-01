using System.Collections;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    public interface IWellKnownStringEqualityComparer
    {
        IEqualityComparer GetRandomizedEqualityComparer();
        IEqualityComparer GetEqualityComparerForSerialization();
    }
}
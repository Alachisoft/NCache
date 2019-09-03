using Alachisoft.NCache.Common.Transactions;
using System.Collections.Generic;

namespace Alachisoft.NCache.Common.DataStructures
{
    public interface IClusteredList<T> : IList<T>,ITransactableStore
    {
        T Last();
        T First();
        void InsertAtHead(T item, out int size);
        void InsertAtTail(T item, out int size);
        T RemoveAt(int index, out int removedSize);
        IClusteredList<T> GetRange(int index, int count);
        bool InsertAfter(T pivot, T value, out int size);
        bool InsertBefore(T pivot, T value, out int size);
        IList<T> Trim(int start, int end, bool getTrimmedData, out int trimmedSize);
        T Update(int index, T item, out int oldItemSize);
        IList<T> RemoveRange(int index, int count, bool getRemovedData, out int removedSize);
        void AddRange(IEnumerable<T> collection, out int addedSize);
    }
}

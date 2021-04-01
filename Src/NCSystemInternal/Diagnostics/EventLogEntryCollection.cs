using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Defines size and enumerators for a collection of System.Diagnostics.EventLogEntry
    //     instances.
    //[DefaultMember("Item")]
    public class EventLogEntryCollection : ICollection, IEnumerable
    {
        //
        // Summary:
        //     Gets an entry in the event log, based on an index that starts at 0 (zero).
        //
        // Parameters:
        //   index:
        //     The zero-based index that is associated with the event log entry.
        //
        // Returns:
        //     The event log entry at the location that is specified by the index parameter.
        public virtual EventLogEntry this[int index] { get { throw new NotImplementedException(); } }

        //
        // Summary:
        //     Gets the number of entries in the event log (that is, the number of elements
        //     in the System.Diagnostics.EventLogEntry collection).
        //
        // Returns:
        //     The number of entries currently in the event log.
        public int Count { get; }

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        //
        // Summary:
        //     Copies the elements of the System.Diagnostics.EventLogEntryCollection to an array
        //     of System.Diagnostics.EventLogEntry instances, starting at a particular array
        //     index.
        //
        // Parameters:
        //   entries:
        //     The one-dimensional array of System.Diagnostics.EventLogEntry instances that
        //     is the destination of the elements copied from the collection. The array must
        //     have zero-based indexing.
        //
        //   index:
        //     The zero-based index in the array at which copying begins.
        public void CopyTo(EventLogEntry[] entries, int index)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        //
        // Summary:
        //     Supports a simple iteration over the System.Diagnostics.EventLogEntryCollection
        //     object.
        //
        // Returns:
        //     An object that can be used to iterate over the collection.
        public IEnumerator GetEnumerator()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}

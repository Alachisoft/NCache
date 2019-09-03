using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;

namespace System.Web.SessionState
{
    //
    // Summary:
    //     Defines the contract for the collection used by ASP.NET session state to manage
    //     session.
    //[DefaultMember("Item")]
    public interface ISessionStateItemCollection : ICollection, IEnumerable
    {
        //
        // Summary:
        //     Gets or sets a value in the collection by name.
        //
        // Parameters:
        //   name:
        //     The key name of the value in the collection.
        //
        // Returns:
        //     The value in the collection with the specified name.
        object this[string name] { get; set; }
        //
        // Summary:
        //     Gets or sets a value in the collection by numerical index.
        //
        // Parameters:
        //   index:
        //     The numerical index of the value in the collection.
        //
        // Returns:
        //     The value in the collection stored at the specified index.
        object this[int index] { get; set; }

        //
        // Summary:
        //     Gets a collection of the variable names for all values stored in the collection.
        //
        // Returns:
        //     The System.Collections.Specialized.NameObjectCollectionBase.KeysCollection that
        //     contains all the collection keys.
        NameObjectCollectionBase.KeysCollection Keys { get; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the collection has been marked as changed.
        //
        // Returns:
        //     true if the System.Web.SessionState.SessionStateItemCollection contents have
        //     been changed; otherwise, false.
        bool Dirty { get; set; }

        //
        // Summary:
        //     Removes all values and keys from the session-state collection.
        void Clear();
        //
        // Summary:
        //     Deletes an item from the collection.
        //
        // Parameters:
        //   name:
        //     The name of the item to delete from the collection.
        void Remove(string name);
        //
        // Summary:
        //     Deletes an item at a specified index from the collection.
        //
        // Parameters:
        //   index:
        //     The index of the item to remove from the collection.
        void RemoveAt(int index);
    }
}

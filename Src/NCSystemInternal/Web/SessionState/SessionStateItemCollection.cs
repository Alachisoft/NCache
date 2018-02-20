using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;

namespace System.Web.SessionState
{
    //
    // Summary:
    //     A collection of objects stored in session state. This class cannot be inherited.
    //[DefaultMember("Item")]
    public sealed class SessionStateItemCollection : NameObjectCollectionBase, ISessionStateItemCollection, ICollection, IEnumerable
    {
        //
        // Summary:
        //     Creates a new, empty System.Web.SessionState.SessionStateItemCollection object.
        public SessionStateItemCollection()
        {
            //TODO: ALACHISOFT
        }

        //
        // Summary:
        //     Gets or sets a value in the collection by name.
        //
        // Parameters:
        //   name:
        //     The key name of the value in the collection.
        //
        // Returns:
        //     The value in the collection with the specified name. If the specified key is
        //     not found, attempting to get it returns null, and attempting to set it creates
        //     a new element using the specified key.
        public object this[string name] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        //
        // Summary:
        //     Gets or sets a value in the collection by numerical index.
        //
        // Parameters:
        //   index:
        //     The numerical index of the value in the collection.
        //
        // Returns:
        //     The value in the collection stored at the specified index. If the specified key
        //     is not found, attempting to get it returns null, and attempting to set it creates
        //     a new element using the specified key.
        public object this[int index] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        //
        // Summary:
        //     Gets or sets a value indicating whether the collection has been marked as changed.
        //
        // Returns:
        //     true if the System.Web.SessionState.SessionStateItemCollection contents have
        //     been changed; otherwise, false.
        public bool Dirty { get; set; }
        //
        // Summary:
        //     Gets a collection of the variable names for all values stored in the collection.
        //
        // Returns:
        //     The System.Collections.Specialized.NameObjectCollectionBase.KeysCollection collection
        //     that contains all the collection keys.
        public override KeysCollection Keys { get; }

        //
        // Summary:
        //     Creates a System.Web.SessionState.SessionStateItemCollection collection from
        //     a storage location that is written to using the System.Web.SessionState.SessionStateItemCollection.Serialize(System.IO.BinaryWriter)
        //     method.
        //
        // Parameters:
        //   reader:
        //     The System.IO.BinaryReader used to read the serialized collection from a stream
        //     or encoded string.
        //
        // Returns:
        //     A System.Web.SessionState.SessionStateItemCollection collection populated with
        //     the contents from a storage location that is written to using the System.Web.SessionState.SessionStateItemCollection.Serialize(System.IO.BinaryWriter)
        //     method.
        //
        // Exceptions:
        //   T:System.Web.HttpException:
        //     The session state information is invalid or corrupted
        public static SessionStateItemCollection Deserialize(BinaryReader reader)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Removes all values and keys from the session-state collection.
        public void Clear()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns an enumerator that can be used to read all the key names in the collection.
        //
        // Returns:
        //     An System.Collections.IEnumerator that can iterate through the variable names
        //     in the session-state collection.
        public override IEnumerator GetEnumerator()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Deletes an item from the collection.
        //
        // Parameters:
        //   name:
        //     The name of the item to delete from the collection.
        public void Remove(string name)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Deletes an item at a specified index from the collection.
        //
        // Parameters:
        //   index:
        //     The index of the item to remove from the collection.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     index is less than zero.- or -index is equal to or greater than System.Collections.ICollection.Count.
        public void RemoveAt(int index)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes the contents of the collection to a System.IO.BinaryWriter.
        //
        // Parameters:
        //   writer:
        //     The System.IO.BinaryWriter used to write the serialized collection to a stream
        //     or encoded string.
        public void Serialize(BinaryWriter writer)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}

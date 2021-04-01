using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Text;

namespace System.Web
{
    //
    // Summary:
    //     Provides a collection of application-scoped objects for the System.Web.HttpApplicationState.StaticObjects
    //     property.
    //[DefaultMember("Item")]
    public sealed class HttpStaticObjectsCollection : ICollection, IEnumerable
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Web.HttpStaticObjectsCollection class.
        public HttpStaticObjectsCollection()
        {
            //TODO: ALACHISOFT
        }

        //
        // Summary:
        //     Gets the object with the specified name from the collection.
        //
        // Parameters:
        //   name:
        //     The case-insensitive name of the object to get.
        //
        // Returns:
        //     An object from the collection.
        public object this[string name] { get { throw new NotImplementedException(); } }

        //
        // Summary:
        //     Gets a Boolean value indicating whether the collection has been accessed before.
        //
        // Returns:
        //     true if the System.Web.HttpStaticObjectsCollection has never been accessed; otherwise,
        //     false.
        public bool NeverAccessed { get; }
        //
        // Summary:
        //     Gets the number of objects in the collection.
        //
        // Returns:
        //     The number of objects in the collection.
        public int Count { get; }
        //
        // Summary:
        //     Gets an object that can be used to synchronize access to the collection.
        //
        // Returns:
        //     The current System.Web.HttpStaticObjectsCollection.
        public object SyncRoot { get; }
        //
        // Summary:
        //     Gets a value indicating whether the collection is read-only.
        //
        // Returns:
        //     Always returns true.
        public bool IsReadOnly { get; }
        //
        // Summary:
        //     Gets a value indicating whether the collection is synchronized (that is, thread-safe).
        //
        // Returns:
        //     In this implementation, this property always returns false.
        public bool IsSynchronized { get; }

        //
        // Summary:
        //     Creates an System.Web.HttpStaticObjectsCollection object from a binary file that
        //     was written by using the System.Web.HttpStaticObjectsCollection.Serialize(System.IO.BinaryWriter)
        //     method.
        //
        // Parameters:
        //   reader:
        //     The System.IO.BinaryReader used to read the serialized collection from a stream
        //     or encoded string.
        //
        // Returns:
        //     An System.Web.HttpStaticObjectsCollection populated with the contents from a
        //     binary file written using the System.Web.HttpStaticObjectsCollection.Serialize(System.IO.BinaryWriter)
        //     method.
        public static HttpStaticObjectsCollection Deserialize(BinaryReader reader)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Copies members of an System.Web.HttpStaticObjectsCollection into an array.
        //
        // Parameters:
        //   array:
        //     The array to copy the System.Web.HttpStaticObjectsCollection into.
        //
        //   index:
        //     The member of the collection where copying starts.
        public void CopyTo(Array array, int index)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns a dictionary enumerator used for iterating through the key-and-value
        //     pairs contained in the collection.
        //
        // Returns:
        //     The enumerator for the collection.
        public IEnumerator GetEnumerator()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns the object with the specified name from the collection. This property
        //     is an alternative to the this accessor.
        //
        // Parameters:
        //   name:
        //     The case-insensitive name of the object to return.
        //
        // Returns:
        //     An object from the collection.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public object GetObject(string name)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes the contents of the collection to a System.IO.BinaryWriter object.
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

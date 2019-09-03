using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace System.DirectoryServices
{
    //
    // Summary:
    //     Contains the properties of a System.DirectoryServices.SearchResult instance.
    [DefaultMember("Item")]
    public class ResultPropertyCollection : DictionaryBase
    {
        //
        // Summary:
        //     Gets the property from this collection that has the specified name.
        //
        // Parameters:
        //   name:
        //     The name of the property to retrieve.
        //
        // Returns:
        //     A System.DirectoryServices.ResultPropertyValueCollection that has the specified
        //     name.
        //public ResultPropertyValueCollection this[string name] { get; }

        //
        // Summary:
        //     Gets the names of the properties in this collection.
        //
        // Returns:
        //     An System.Collections.ICollection that contains the names of the properties in
        //     this collection.
        public ICollection PropertyNames { get; }
        //
        // Summary:
        //     Gets the values of the properties in this collection.
        //
        // Returns:
        //     An System.Collections.ICollection that contains the values of the properties
        //     in this collection.
        public ICollection Values { get; }

        //
        // Summary:
        //     Determines whether the property that has the specified name belongs to this collection.
        //
        // Parameters:
        //   propertyName:
        //     The name of the property to find.
        //
        // Returns:
        //     The return value is true if the specified property belongs to this collection;
        //     otherwise, false.
        public bool Contains(string propertyName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Copies the properties from this collection to an array, starting at a particular
        //     index of the array.
        //
        // Parameters:
        //   array:
        //     An array of type System.DirectoryServices.ResultPropertyValueCollection that
        //     receives this collection's properties.
        //
        //   index:
        //     The zero-based array index at which to begin copying the properties.
        //public void CopyTo(ResultPropertyValueCollection[] array, int index)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
    }
}

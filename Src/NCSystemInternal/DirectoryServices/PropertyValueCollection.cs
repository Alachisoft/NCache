using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices
{
    //
    // Summary:
    //     Contains the values of a System.DirectoryServices.DirectoryEntry property.
    //[DefaultMember("Item")]
    public class PropertyValueCollection : CollectionBase
    {
        //
        // Summary:
        //     Gets or sets the property value that is located at a specified index of this
        //     collection.
        //
        // Parameters:
        //   index:
        //     The zero-based index of the property value.
        //
        // Returns:
        //     The property value at the specified index.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The property value is a null reference (Nothing in Visual Basic).
        //
        //   T:System.IndexOutOfRangeException:
        //     The index is less than zero (0) or greater than the size of the collection.
        public object this[int index] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        //
        // Summary:
        //     Gets the property name for the attributes in the value collection.
        //
        // Returns:
        //     A string that contains the name of the property with the values that are included
        //     in this System.DirectoryServices.PropertyValueCollection object.
        [ComVisible(false)]
        public string PropertyName { get; }
        //
        // Summary:
        //     Gets or sets the values of the collection.
        //
        // Returns:
        //     If the collection is empty, the property value is a null reference (Nothing in
        //     Visual Basic). If the collection contains one value, the property value is that
        //     value. If the collection contains multiple values, the property value equals
        //     a copy of an array of those values.If setting this property, the value or values
        //     are added to the System.DirectoryServices.PropertyValueCollection. Setting this
        //     property to a null reference (Nothing) clears the collection.
        public object Value { get; set; }

        //
        // Summary:
        //     Appends the specified System.DirectoryServices.PropertyValueCollection object
        //     to this collection.
        //
        // Parameters:
        //   value:
        //     The System.DirectoryServices.PropertyValueCollection object to append to this
        //     collection.
        //
        // Returns:
        //     The zero-based index of the System.DirectoryServices.PropertyValueCollection
        //     object that is appended to this collection.
        //
        // Exceptions:
        //   T:System.Runtime.InteropServices.COMException:
        //     An error occurred during the call to the underlying interface.
        //
        //   T:System.ArgumentNullException:
        //     The property value is a null reference (Nothing in Visual Basic).
        public int Add(object value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Appends the contents of the specified System.DirectoryServices.PropertyValueCollection
        //     object to this collection.
        //
        // Parameters:
        //   value:
        //     The System.DirectoryServices.PropertyValueCollection array that contains the
        //     objects to append to this collection.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The property value is a null reference (Nothing in Visual Basic).
        public void AddRange(object[] value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Appends the contents of the System.DirectoryServices.PropertyValueCollection
        //     object to this collection.
        //
        // Parameters:
        //   value:
        //     A System.DirectoryServices.PropertyValueCollection object that contains the objects
        //     to append to this collection.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The property value is a null reference (Nothing in Visual Basic).
        public void AddRange(PropertyValueCollection value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Determines whether the specified System.DirectoryServices.PropertyValueCollection
        //     object is in this collection.
        //
        // Parameters:
        //   value:
        //     The System.DirectoryServices.PropertyValueCollection object to search for in
        //     this collection.
        //
        // Returns:
        //     true if the specified property belongs to this collection; otherwise, false.
        public bool Contains(object value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Copies all System.DirectoryServices.PropertyValueCollection objects in this collection
        //     to the specified array, starting at the specified index in the target array.
        //
        // Parameters:
        //   array:
        //     The array of System.DirectoryServices.PropertyValueCollection objects that receives
        //     the elements of this collection.
        //
        //   index:
        //     The zero-based index in array where this method starts copying this collection.
        public void CopyTo(object[] array, int index)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Retrieves the index of a specified property value in this collection.
        //
        // Parameters:
        //   value:
        //     The property value to find.
        //
        // Returns:
        //     The zero-based index of the specified property value. If the object is not found,
        //     the return value is -1.
        public int IndexOf(object value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Inserts a property value into this collection at a specified index.
        //
        // Parameters:
        //   index:
        //     The zero-based index at which to insert the property value.
        //
        //   value:
        //     The property value to insert.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The property value is a null reference (Nothing in Visual Basic).
        //
        //   T:System.Runtime.InteropServices.COMException:
        //     An error occurred during the call to the underlying interface.
        //
        //   T:System.IndexOutOfRangeException:
        //     The index is less than 0 (zero) or greater than the size of the collection.
        public void Insert(int index, object value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Removes a specified property value from this collection.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The property value is a null reference (Nothing in Visual Basic).
        //
        //   T:System.Runtime.InteropServices.COMException:
        //     An error occurred during the call to the underlying interface.
        public void Remove(object value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Overrides the System.Collections.CollectionBase.OnClearComplete method.
        protected override void OnClearComplete()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Overrides the System.Collections.CollectionBase.OnInsertComplete(System.Int32,System.Object)
        //     method.
        //
        // Parameters:
        //   index:
        //     The zero-based index at which to insert value.
        //
        //   value:
        //     The new value of the element at index.
        protected override void OnInsertComplete(int index, object value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Overrides the System.Collections.CollectionBase.OnRemoveComplete(System.Int32,System.Object)
        //     method.
        //
        // Parameters:
        //   index:
        //     The zero-based index at which value can be found.
        //
        //   value:
        //     The value of the element to remove from index.
        protected override void OnRemoveComplete(int index, object value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Overrides the System.Collections.CollectionBase.OnSetComplete(System.Int32,System.Object,System.Object)
        //     method.
        //
        // Parameters:
        //   index:
        //     The zero-based index at which oldValue can be found.
        //
        //   oldValue:
        //     The value to replace with newValue.
        //
        //   newValue:
        //     The new value of the element at index.
        protected override void OnSetComplete(int index, object oldValue, object newValue)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}

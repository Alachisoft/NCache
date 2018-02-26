// ==++==
//Copyright(c) Microsoft Corporation

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
//associated documentation files (the "Software"), to deal in the Software without restriction, 
//including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or substantial
//portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
//NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
//IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ==--==

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

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
using System.Collections.Generic;
using System.Text;

namespace System.DirectoryServices
{
    //
    // Summary:
    //     The System.DirectoryServices.SearchResult class encapsulates a node in the Active
    //     Directory Domain Services hierarchy that is returned during a search through
    //     System.DirectoryServices.DirectorySearcher.
    public class SearchResult
    {
        //
        // Summary:
        //     Gets the path for this System.DirectoryServices.SearchResult.
        //
        // Returns:
        //     The path of this System.DirectoryServices.SearchResult.
        public string Path { get; }
        //
        // Summary:
        //     Gets a System.DirectoryServices.ResultPropertyCollection collection of properties
        //     for this object.
        //
        // Returns:
        //     A System.DirectoryServices.ResultPropertyCollection of properties set on this
        //     object.
        public ResultPropertyCollection Properties { get; }

        //
        // Summary:
        //     Retrieves the System.DirectoryServices.DirectoryEntry that corresponds to the
        //     System.DirectoryServices.SearchResult from the Active Directory Domain Services
        //     hierarchy.
        //
        // Returns:
        //     The System.DirectoryServices.DirectoryEntry that corresponds to the System.DirectoryServices.SearchResult.
        public DirectoryEntry GetDirectoryEntry()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}

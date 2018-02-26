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
    //     Specifies the possible scopes for a directory search that is performed using
    //     the System.DirectoryServices.DirectorySearcher object.
    public enum SearchScope
    {
        //
        // Summary:
        //     Limits the search to the base object. The result contains a maximum of one object.
        //     When the System.DirectoryServices.DirectorySearcher.AttributeScopeQuery property
        //     is specified for a search, the scope of the search must be set to System.DirectoryServices.SearchScope.Base.
        Base = 0,
        //
        // Summary:
        //     Searches the immediate child objects of the base object, excluding the base object.
        OneLevel = 1,
        //
        // Summary:
        //     Searches the whole subtree, including the base object and all its child objects.
        //     If the scope of a directory search is not specified, a System.DirectoryServices.SearchScope.Subtree
        //     type of search is performed.
        Subtree = 2
    }
}

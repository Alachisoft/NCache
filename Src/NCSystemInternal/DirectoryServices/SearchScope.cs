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

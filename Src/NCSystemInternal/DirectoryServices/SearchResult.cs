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

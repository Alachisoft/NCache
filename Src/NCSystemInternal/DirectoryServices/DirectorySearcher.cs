using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices
{
    //
    // Summary:
    //     Performs queries against Active Directory Domain Services.
    [DSDescription("DirectorySearcherDesc")]
    public class DirectorySearcher : Component
    {
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectorySearcher
        //     class with default values.
        public DirectorySearcher()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectorySearcher
        //     class using the specified search root.
        //
        // Parameters:
        //   searchRoot:
        //     The node in the Active Directory Domain Services hierarchy where the search starts.
        //     The System.DirectoryServices.DirectorySearcher.SearchRoot property is initialized
        //     to this value.
        public DirectorySearcher(DirectoryEntry searchRoot)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectorySearcher
        //     class with the specified search filter.
        //
        // Parameters:
        //   filter:
        //     The search filter string in Lightweight Directory Access Protocol (LDAP) format.
        //     The System.DirectoryServices.DirectorySearcher.Filter property is initialized
        //     to this value.
        public DirectorySearcher(string filter)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectorySearcher
        //     class with the specified search root and search filter.
        //
        // Parameters:
        //   searchRoot:
        //     The node in the Active Directory Domain Services hierarchy where the search starts.
        //     The System.DirectoryServices.DirectorySearcher.SearchRoot property is initialized
        //     to this value.
        //
        //   filter:
        //     The search filter string in Lightweight Directory Access Protocol (LDAP) format.
        //     The System.DirectoryServices.DirectorySearcher.Filter property is initialized
        //     to this value.
        public DirectorySearcher(DirectoryEntry searchRoot, string filter)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectorySearcher
        //     class with the specified search filter and properties to retrieve.
        //
        // Parameters:
        //   filter:
        //     The search filter string in Lightweight Directory Access Protocol (LDAP) format.
        //     The System.DirectoryServices.DirectorySearcher.Filter property is initialized
        //     to this value.
        //
        //   propertiesToLoad:
        //     The set of properties to retrieve during the search. The System.DirectoryServices.DirectorySearcher.PropertiesToLoad
        //     property is initialized to this value.
        public DirectorySearcher(string filter, string[] propertiesToLoad)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectorySearcher
        //     class with the specified search root, search filter, and properties to retrieve..
        //
        // Parameters:
        //   searchRoot:
        //     The node in the Active Directory Domain Services hierarchy where the search starts.
        //     The System.DirectoryServices.DirectorySearcher.SearchRoot property is initialized
        //     to this value.
        //
        //   filter:
        //     The search filter string in Lightweight Directory Access Protocol (LDAP) format.
        //     The System.DirectoryServices.DirectorySearcher.Filter property is initialized
        //     to this value.
        //
        //   propertiesToLoad:
        //     The set of properties that are retrieved during the search. The System.DirectoryServices.DirectorySearcher.PropertiesToLoad
        //     property is initialized to this value.
        public DirectorySearcher(DirectoryEntry searchRoot, string filter, string[] propertiesToLoad)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectorySearcher
        //     class with the specified search filter, properties to retrieve, and search scope.
        //
        // Parameters:
        //   filter:
        //     The search filter string in Lightweight Directory Access Protocol (LDAP) format.
        //     The System.DirectoryServices.DirectorySearcher.Filter property is initialized
        //     to this value.
        //
        //   propertiesToLoad:
        //     The set of properties to retrieve during the search. The System.DirectoryServices.DirectorySearcher.PropertiesToLoad
        //     property is initialized to this value.
        //
        //   scope:
        //     The scope of the search that is observed by the server. The System.DirectoryServices.SearchScope
        //     property is initialized to this value.
        //public DirectorySearcher(string filter, string[] propertiesToLoad, SearchScope scope)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectorySearcher
        //     class with the specified search root, search filter, properties to retrieve,
        //     and search scope.
        //
        // Parameters:
        //   searchRoot:
        //     The node in the Active Directory Domain Services hierarchy where the search starts.
        //     The System.DirectoryServices.DirectorySearcher.SearchRoot property is initialized
        //     to this value.
        //
        //   filter:
        //     The search filter string in Lightweight Directory Access Protocol (LDAP) format.
        //     The System.DirectoryServices.DirectorySearcher.Filter property is initialized
        //     to this value.
        //
        //   propertiesToLoad:
        //     The set of properties to retrieve during the search. The System.DirectoryServices.DirectorySearcher.PropertiesToLoad
        //     property is initialized to this value.
        //
        //   scope:
        //     The scope of the search that is observed by the server. The System.DirectoryServices.SearchScope
        //     property is initialized to this value.
        //public DirectorySearcher(DirectoryEntry searchRoot, string filter, string[] propertiesToLoad, SearchScope scope)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}


        //
        // Summary:
        //     Gets or sets a value indicating the maximum number of objects that the server
        //     returns in a search.
        //
        // Returns:
        //     The maximum number of objects that the server returns in a search. The default
        //     value is zero, which means to use the server-determined default size limit of
        //     1000 entries.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The new value is less than zero.
        [DefaultValue(0)]
        [DSDescription("DSSizeLimit")]
        public int SizeLimit { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating the node in the Active Directory Domain Services
        //     hierarchy where the search starts.
        //
        // Returns:
        //     The System.DirectoryServices.DirectoryEntry object in the Active Directory Domain
        //     Services hierarchy where the search starts. The default is a null reference (Nothing
        //     in Visual Basic).
        [DefaultValue(null)]
        [DSDescription("DSSearchRoot")]
        public DirectoryEntry SearchRoot { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating the property on which the results are sorted.
        //
        // Returns:
        //     A System.DirectoryServices.SortOption object that specifies the property and
        //     direction that the search results should be sorted on.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The property value is null (Nothing in Visual Basic).
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        //[DSDescription("DSSort")]
        //[TypeConverter(typeof(ExpandableObjectConverter))]
        //public SortOption Sort { get; set; }
        //
        // Summary:
        //     Gets or sets a value that indicates if the search is performed asynchronously.
        //
        // Returns:
        //     true if the search is asynchronous; false otherwise.
        [ComVisible(false)]
        [DefaultValue(false)]
        [DSDescription("DSAsynchronous")]
        public bool Asynchronous { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating which security access information for the specified
        //     attributes should be returned by the search.
        //
        // Returns:
        //     One of the System.DirectoryServices.SecurityMasks values.
        //[ComVisible(false)]
        //[DefaultValue(SecurityMasks.None)]
        //[DSDescription("DSSecurityMasks")]
        //public SecurityMasks SecurityMasks { get; set; }
        //
        // Summary:
        //     Gets or sets the LDAP display name of the distinguished name attribute to search
        //     in. Only one attribute can be used for this type of search.
        //
        // Returns:
        //     The LDAP display name of the attribute to perform the search against, or an empty
        //     string of no attribute scope query is set.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.DirectoryServices.DirectorySearcher.SearchScope property is set to
        //     a value other than System.DirectoryServices.SearchScope.Base.
        [ComVisible(false)]
        [DefaultValue("")]
        [DSDescription("DSAttributeQuery")]
        [TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string AttributeScopeQuery { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating how the aliases of objects that are found during
        //     a search should be resolved.
        //
        // Returns:
        //     A System.DirectoryServices.DereferenceAlias value that specifies the behavior
        //     in which aliases are dereferenced. The default setting for this property is System.DirectoryServices.DereferenceAlias.Never.
        //[ComVisible(false)]
        //[DefaultValue(DereferenceAlias.Never)]
        //[DSDescription("DSDerefAlias")]
        //public DereferenceAlias DerefAlias { get; set; }
        //
        // Summary:
        //     The System.DirectoryServices.DirectorySearcher.ServerTimeLimit property gets
        //     or sets a value indicating the maximum amount of time the server spends searching.
        //     If the time limit is reached, only entries that are found up to that point are
        //     returned.
        //
        // Returns:
        //     A System.TimeSpan that represents the amount of time that the server should search.The
        //     default value is -1 seconds, which means to use the server-determined default
        //     of 120 seconds.
        [DSDescription("DSServerTimeLimit")]
        public TimeSpan ServerTimeLimit { get; set; }
        //
        // Summary:
        //     Gets or sets a value that indicates the format of the distinguished names.
        //
        // Returns:
        //     One of the System.DirectoryServices.ExtendedDN values.
        //[ComVisible(false)]
        //[DefaultValue(ExtendedDN.None)]
        //[DSDescription("DSExtendedDn")]
        //public ExtendedDN ExtendedDN { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the search should also return deleted
        //     objects that match the search filter.
        //
        // Returns:
        //     true if deleted objects should be included in the search; false otherwise. The
        //     default value is false.
        [ComVisible(false)]
        [DefaultValue(false)]
        [DSDescription("DSTombstone")]
        public bool Tombstone { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating the maximum amount of time the server should
        //     search for an individual page of results. This is not the same as the time limit
        //     for the entire search.
        //
        // Returns:
        //     A System.TimeSpan that represents the amount of time the server should search
        //     for a page of results.The default value is -1 seconds, which means to search
        //     indefinitely.
        [DSDescription("DSServerPageTimeLimit")]
        public TimeSpan ServerPageTimeLimit { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating the page size in a paged search.
        //
        // Returns:
        //     The maximum number of objects the server can return in a paged search. The default
        //     is zero, which means do not do a paged search.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The new value is less than zero.
        [DefaultValue(0)]
        [DSDescription("DSPageSize")]
        public int PageSize { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating how referrals are chased.
        //
        // Returns:
        //     One of the System.DirectoryServices.ReferralChasingOption values. The default
        //     is System.DirectoryServices.ReferralChasingOption.External.
        //
        // Exceptions:
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     The value is not one of the System.DirectoryServices.ReferralChasingOption values.
        //[DefaultValue(ReferralChasingOption.External)]
        //[DSDescription("DSReferralChasing")]
        //public ReferralChasingOption ReferralChasing { get; set; }
        //
        // Summary:
        //     Gets a value indicating the list of properties to retrieve during the search.
        //
        // Returns:
        //     A System.Collections.Specialized.StringCollection object that contains the set
        //     of properties to retrieve during the search.The default is an empty System.Collections.Specialized.StringCollection,
        //     which retrieves all properties.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [DSDescription("DSPropertiesToLoad")]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public StringCollection PropertiesToLoad { get; }
        //
        // Summary:
        //     Gets or sets an object that represents the directory synchronization control
        //     to use with the search.
        //
        // Returns:
        //     The System.DirectoryServices.DirectorySynchronization object for the search.
        //     null if the directory synchronization control should not be used.
        //[Browsable(false)]
        //[ComVisible(false)]
        //[DefaultValue(null)]
        //[DSDescription("DSDirectorySynchronization")]
        //public DirectorySynchronization DirectorySynchronization { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating the Lightweight Directory Access Protocol (LDAP)
        //     format filter string.
        //
        // Returns:
        //     The search filter string in LDAP format, such as "(objectClass=user)". The default
        //     is "(objectClass=*)", which retrieves all objects.
        [DefaultValue("(objectClass=*)")]
        [DSDescription("DSFilter")]
        [SettingsBindable(true)]
        [TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string Filter { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the search retrieves only the names of
        //     attributes to which values have been assigned.
        //
        // Returns:
        //     true if the search obtains only the names of attributes to which values have
        //     been assigned; false if the search obtains the names and values for all the requested
        //     attributes. The default value is false.
        [DefaultValue(false)]
        [DSDescription("DSPropertyNamesOnly")]
        public bool PropertyNamesOnly { get; set; }
        //
        // Summary:
        //     Gets or sets the maximum amount of time that the client waits for the server
        //     to return results. If the server does not respond within this time, the search
        //     is aborted and no results are returned.
        //
        // Returns:
        //     A System.TimeSpan structure that contains the maximum amount of time for the
        //     client to wait for the server to return results.The default value is -1 second,
        //     which means to wait indefinitely.
        [DSDescription("DSClientTimeout")]
        public TimeSpan ClientTimeout { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the result is cached on the client computer.
        //
        // Returns:
        //     true if the result is cached on the client computer; otherwise, false. The default
        //     is true.
        [DefaultValue(true)]
        [DSDescription("DSCacheResults")]
        public bool CacheResults { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating the scope of the search that is observed by the
        //     server.
        //
        // Returns:
        //     One of the System.DirectoryServices.SearchScope values. The default is System.DirectoryServices.SearchScope.Subtree.
        //
        // Exceptions:
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     The value is not one of the System.DirectoryServices.ReferralChasingOption values.
        [DefaultValue(SearchScope.Subtree)]
        [DSDescription("DSSearchScope")]
        [SettingsBindable(true)]
        public SearchScope SearchScope { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating the virtual list view options for the search.
        //
        // Returns:
        //     A System.DirectoryServices.DirectoryVirtualListView object that contains the
        //     virtual list view search information. The default value for this property is
        //     null, which means do not use the virtual list view search option.
        //[Browsable(false)]
        //[ComVisible(false)]
        //[DefaultValue(null)]
        //[DSDescription("DSVirtualListView")]
        //public DirectoryVirtualListView VirtualListView { get; set; }

        //
        // Summary:
        //     Executes the search and returns a collection of the entries that are found.
        //
        // Returns:
        //     A System.DirectoryServices.SearchResultCollection object that contains the results
        //     of the search.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified System.DirectoryServices.DirectoryEntry is not a container.
        //
        //   T:System.NotSupportedException:
        //     Searching is not supported by the provider that is being used.
        //public SearchResultCollection FindAll();
        //
        // Summary:
        //     Executes the search and returns only the first entry that is found.
        //
        // Returns:
        //     A System.DirectoryServices.SearchResult object that contains the first entry
        //     that is found during the search.
        public SearchResult FindOne()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Releases the managed resources that are used by the System.DirectoryServices.DirectorySearcher
        //     object and, optionally, releases unmanaged resources.
        //
        // Parameters:
        //   disposing:
        //     true if this method releases both managed and unmanaged resources; false if it
        //     releases only unmanaged resources.
        protected override void Dispose(bool disposing)
        {
            // TODO: ALACHISOFT
        }
    }
}

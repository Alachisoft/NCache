using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.Design;
using System.Runtime.InteropServices;
using System.Text;

namespace System.DirectoryServices
{
    //
    // Summary:
    //     The System.DirectoryServices.DirectoryEntry class encapsulates a node or object
    //     in the Active Directory Domain Services hierarchy.
    [Description("DirectoryEntryDesc")]
    [TypeConverter(typeof(DirectoryEntryConverter))]
    public class DirectoryEntry : Component
    {
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectoryEntry class.
        public DirectoryEntry()
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectoryEntry class
        //     that binds this instance to the node in Active Directory Domain Services located
        //     at the specified path.
        //
        // Parameters:
        //   path:
        //     The path at which to bind the System.DirectoryServices.DirectoryEntry.#ctor(System.String)
        //     to the directory. The System.DirectoryServices.DirectoryEntry.Path property is
        //     initialized to this value.
        public DirectoryEntry(string path)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectoryEntry class
        //     that binds to the specified native Active Directory Domain Services object.
        //
        // Parameters:
        //   adsObject:
        //     The name of the native Active Directory Domain Services object to bind to.
        public DirectoryEntry(object adsObject)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectoryEntry class.
        //
        // Parameters:
        //   username:
        //     The user name to use when authenticating the client. The System.DirectoryServices.DirectoryEntry.Username
        //     property is initialized to this value.
        public DirectoryEntry(string path, string username, string password)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.DirectoryServices.DirectoryEntry class.
        //
        // Parameters:
        //   username:
        //     The user name to use when authenticating the client. The System.DirectoryServices.DirectoryEntry.Username
        //     property is initialized to this value.
        //
        //   authenticationType:
        //     One of the System.DirectoryServices.AuthenticationTypes values. The System.DirectoryServices.DirectoryEntry.AuthenticationType
        //     property is initialized to this value.
        public DirectoryEntry(string path, string username, string password, AuthenticationTypes authenticationType)
        {
            //TODO: ALACHISOFT
        }

        //
        // Summary:
        //     Gets the GUID of the System.DirectoryServices.DirectoryEntry.
        //
        // Returns:
        //     A System.Guid structure that represents the GUID of the System.DirectoryServices.DirectoryEntry.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[DSDescription("DSGuid")]
        //public Guid Guid { get; }
        //
        // Summary:
        //     Gets or sets the security descriptor for this entry.
        //
        // Returns:
        //     An System.DirectoryServices.ActiveDirectorySecurity object that represents the
        //     security descriptor for this directory entry.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[DSDescription("DSObjectSecurity")]
        //public ActiveDirectorySecurity ObjectSecurity { get; set; }
        //
        // Summary:
        //     Gets the name of the object as named with the underlying directory service.
        //
        // Returns:
        //     The name of the object as named with the underlying directory service.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[DSDescription("DSName")]
        //public string Name { get; }
        //
        // Summary:
        //     Gets the GUID of the System.DirectoryServices.DirectoryEntry, as returned from
        //     the provider.
        //
        // Returns:
        //     A System.Guid structure that represents the GUID of the System.DirectoryServices.DirectoryEntry,
        //     as returned from the provider.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[DSDescription("DSNativeGuid")]
        //public string NativeGuid { get; }
        //
        // Summary:
        //     Gets the native Active Directory Service Interfaces (ADSI) object.
        //
        // Returns:
        //     The native ADSI object.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[DSDescription("DSNativeObject")]
        //public object NativeObject { get; }
        //
        // Summary:
        //     Gets this entry's parent in the Active Directory Domain Services hierarchy.
        //
        // Returns:
        //     A System.DirectoryServices.DirectoryEntry object that represents the parent of
        //     this entry.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[DSDescription("DSParent")]
        //public DirectoryEntry Parent { get; }
        //
        // Summary:
        //     Gets or sets the path for this System.DirectoryServices.DirectoryEntry.
        //
        // Returns:
        //     The path of this System.DirectoryServices.DirectoryEntry object. The default
        //     is an empty string ("").
        //[DefaultValue("")]
        //[DSDescription("DSPath")]
        //[SettingsBindable(true)]
        //[TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        //public string Path { get; set; }
        //
        // Summary:
        //     Gets the child entries of this node in the Active Directory Domain Services hierarchy.
        //
        // Returns:
        //     A System.DirectoryServices.DirectoryEntries object containing the child entries
        //     of this node in the Active Directory Domain Services hierarchy.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[DSDescription("DSChildren")]
        //public DirectoryEntries Children { get; }
        //
        // Summary:
        //     Gets the Active Directory Domain Services properties for this System.DirectoryServices.DirectoryEntry
        //     object.
        //
        // Returns:
        //     A System.DirectoryServices.PropertyCollection object that contains the properties
        //     that are set on this entry.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DSDescription("DSProperties")]
        public PropertyCollection Properties { get; }
        //
        // Summary:
        //     Gets the name of the schema class for this System.DirectoryServices.DirectoryEntry
        //     object.
        //
        // Returns:
        //     The name of the schema class for this System.DirectoryServices.DirectoryEntry
        //     object.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DSDescription("DSSchemaClassName")]
        public string SchemaClassName { get; }
        //
        // Summary:
        //     Gets the schema object for this entry.
        //
        // Returns:
        //     A System.DirectoryServices.DirectoryEntry object that represents the schema class
        //     for this entry.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [DSDescription("DSSchemaEntry")]
        public DirectoryEntry SchemaEntry { get; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the cache should be committed after each
        //     operation.
        //
        // Returns:
        //     true if the cache should not be committed after each operation; otherwise, false.
        //     The default is true.
        [DefaultValue(true)]
        [DSDescription("DSUsePropertyCache")]
        public bool UsePropertyCache { get; set; }
        //
        // Summary:
        //     Sets the password to use when authenticating the client.
        //
        // Returns:
        //     The password to use when authenticating the client.
        [Browsable(false)]
        [DefaultValue(null)]
        [DSDescription("DSPassword")]
        public string Password { set { throw new NotImplementedException(); } }
        //
        // Summary:
        //     Gets or sets the type of authentication to use.
        //
        // Returns:
        //     One of the System.DirectoryServices.AuthenticationTypes values.
        [DefaultValue(AuthenticationTypes.Secure)]
        [DSDescription("DSAuthenticationType")]
        public AuthenticationTypes AuthenticationType { get; set; }
        //
        // Summary:
        //     Gets the provider-specific options for this entry.
        //
        // Returns:
        //     A System.DirectoryServices.DirectoryEntryConfiguration object that contains the
        //     provider-specific options for this entry.
        //[Browsable(false)]
        //[ComVisible(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[DSDescription("DSOptions")]
        //public DirectoryEntryConfiguration Options { get; }
        //
        // Summary:
        //     Gets or sets the user name to use when authenticating the client.
        //
        // Returns:
        //     The user name to use when authenticating the client.
        [Browsable(false)]
        [DefaultValue(null)]
        [DSDescription("DSUsername")]
        [TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string Username { get; set; }

        //
        // Summary:
        //     Determines if the specified path represents an actual entry in the directory
        //     service.
        //
        // Parameters:
        //   path:
        //     The path of the entry to verify.
        //
        // Returns:
        //     true if the specified path represents a valid entry in the directory service;
        //     otherwise, false.
        public static bool Exists(string path)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Closes the System.DirectoryServices.DirectoryEntry object and releases any system
        //     resources that are associated with this component.
        public void Close()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Saves changes that are made to a directory entry to the underlying directory
        //     store.
        public void CommitChanges()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Creates a copy of this System.DirectoryServices.DirectoryEntry object, as a child
        //     of the specified parent System.DirectoryServices.DirectoryEntry object, with
        //     the specified new name.
        //
        // Parameters:
        //   newParent:
        //     The DN of the System.DirectoryServices.DirectoryEntry object that will be the
        //     parent for the copy that is being created.
        //
        //   newName:
        //     The name of the copy of this entry.
        //
        // Returns:
        //     A renamed copy of this entry as a child of the specified parent.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified System.DirectoryServices.DirectoryEntry object is not a container.
        public DirectoryEntry CopyTo(DirectoryEntry newParent, string newName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Creates a copy of this entry as a child of the specified parent.
        //
        // Parameters:
        //   newParent:
        //     The distinguished name of the System.DirectoryServices.DirectoryEntry object
        //     that will be the parent for the copy that is being created.
        //
        // Returns:
        //     A System.DirectoryServices.DirectoryEntry object that represents the copy of
        //     this entry as a child of the new parent.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified System.DirectoryServices.DirectoryEntry is not a container.
        public DirectoryEntry CopyTo(DirectoryEntry newParent)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Deletes this entry and its entire subtree from the Active Directory Domain Services
        //     hierarchy.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified System.DirectoryServices.DirectoryEntry is not a container.
        public void DeleteTree()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Calls a method on the native Active Directory Domain Services object.
        //
        // Parameters:
        //   methodName:
        //     The name of the method to invoke.
        //
        //   args:
        //     An array of type System.Object objects that contains the arguments of the method
        //     to invoke.
        //
        // Returns:
        //     The return value of the invoked method.
        //
        // Exceptions:
        //   T:System.DirectoryServices.DirectoryServicesCOMException:
        //     The native method threw a System.Runtime.InteropServices.COMException exception.
        //
        //   T:System.Reflection.TargetInvocationException:
        //     The native method threw a System.Reflection.TargetInvocationException exception.
        //     The System.Exception.InnerException property contains a System.Runtime.InteropServices.COMException
        //     exception that contains information about the actual error that occurred.
        public object Invoke(string methodName, params object[] args)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets a property from the native Active Directory Domain Services object.
        //
        // Parameters:
        //   propertyName:
        //     The name of the property to get.
        //
        // Returns:
        //     An object that represents the requested property.
        [ComVisible(false)]
        public object InvokeGet(string propertyName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Sets a property on the native Active Directory Domain Services object.
        //
        // Parameters:
        //   propertyName:
        //     The name of the property to set.
        //
        //   args:
        //     The Active Directory Domain Services object to set.
        [ComVisible(false)]
        public void InvokeSet(string propertyName, params object[] args)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Moves this System.DirectoryServices.DirectoryEntry object to the specified parent
        //     and changes its name to the specified value.
        //
        // Parameters:
        //   newParent:
        //     The parent to which you want to move this entry.
        //
        //   newName:
        //     The new name of this entry.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified System.DirectoryServices.DirectoryEntry is not a container.
        public void MoveTo(DirectoryEntry newParent, string newName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Moves this System.DirectoryServices.DirectoryEntry object to the specified parent.
        //
        // Parameters:
        //   newParent:
        //     The parent to which you want to move this entry.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified System.DirectoryServices.DirectoryEntry is not a container.
        public void MoveTo(DirectoryEntry newParent)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Loads the values of the specified properties into the property cache.
        //
        // Parameters:
        //   propertyNames:
        //     An array of the specified properties.
        public void RefreshCache(string[] propertyNames)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Loads the property values for this System.DirectoryServices.DirectoryEntry object
        //     into the property cache.
        public void RefreshCache()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Changes the name of this System.DirectoryServices.DirectoryEntry object.
        //
        // Parameters:
        //   newName:
        //     The new name of the entry.
        public void Rename(string newName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Disposes of the resources (other than memory) that are used by the System.DirectoryServices.DirectoryEntry.
        //
        // Parameters:
        //   disposing:
        //     true to release both managed and unmanaged resources; false to release only unmanaged
        //     resources.
        protected override void Dispose(bool disposing)
        {
            //TODO: ALACHISOFT
        }
    }
}

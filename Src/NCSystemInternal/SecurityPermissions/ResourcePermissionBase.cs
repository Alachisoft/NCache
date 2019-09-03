using System;
using System.Collections.Generic;
using System.Text;

namespace System.Security.Permissions
{
    //
    // Summary:
    //     Allows control of code access security permissions.
    public abstract class ResourcePermissionBase //: CodeAccessPermission, IUnrestrictedPermission
    {
        //
        // Summary:
        //     Specifies the character to be used to represent the any wildcard character.
        public const string Any = "*";
        //
        // Summary:
        //     Specifies the character to be used to represent a local reference.
        public const string Local = ".";

        //
        // Summary:
        //     Initializes a new instance of the System.Security.Permissions.ResourcePermissionBase
        //     class.
        protected ResourcePermissionBase()
        {
            //TODO: ALACHISOFT
            //throw new NotImplementedException();
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Security.Permissions.ResourcePermissionBase
        //     class with the specified level of access to resources at creation.
        //
        // Parameters:
        //   state:
        //     One of the System.Security.Permissions.PermissionState values.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The state parameter is not a valid value of System.Security.Permissions.PermissionState.
        //protected ResourcePermissionBase(PermissionState state);

        //
        // Summary:
        //     Gets or sets an enumeration value that describes the types of access that you
        //     are giving the resource.
        //
        // Returns:
        //     An enumeration value that is derived from System.Type and describes the types
        //     of access that you are giving the resource.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The property value is null.
        //
        //   T:System.ArgumentException:
        //     The property value is not an enumeration value.
        protected Type PermissionAccessType { get; set; }
        //
        // Summary:
        //     Gets or sets an array of strings that identify the resource you are protecting.
        //
        // Returns:
        //     An array of strings that identify the resource you are trying to protect.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The property value is null.
        //
        //   T:System.ArgumentException:
        //     The length of the array is 0.
        protected string[] TagNames { get; set; }

        //
        // Summary:
        //     Creates and returns an identical copy of the current permission object.
        //
        // Returns:
        //     A copy of the current permission object.
        //public override IPermission Copy()
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Reconstructs a security object with a specified state from an XML encoding.
        //
        // Parameters:
        //   securityElement:
        //     The XML encoding to use to reconstruct the security object.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The securityElement parameter is not a valid permission element.-or- The version
        //     number of the securityElement parameter is not supported.
        //
        //   T:System.ArgumentNullException:
        //     The securityElement parameter is null.
        //public override void FromXml(SecurityElement securityElement)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Creates and returns a permission object that is the intersection of the current
        //     permission object and a target permission object.
        //
        // Parameters:
        //   target:
        //     A permission object of the same type as the current permission object.
        //
        // Returns:
        //     A new permission object that represents the intersection of the current object
        //     and the specified target. This object is null if the intersection is empty.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The target permission object is not of the same type as the current permission
        //     object.
        //public override IPermission Intersect(IPermission target)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Determines whether the current permission object is a subset of the specified
        //     permission.
        //
        // Parameters:
        //   target:
        //     A permission object that is to be tested for the subset relationship.
        //
        // Returns:
        //     true if the current permission object is a subset of the specified permission
        //     object; otherwise, false.
        //public override bool IsSubsetOf(IPermission target)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Gets a value indicating whether the permission is unrestricted.
        //
        // Returns:
        //     true if permission is unrestricted; otherwise, false.
        public bool IsUnrestricted()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Creates and returns an XML encoding of the security object and its current state.
        //
        // Returns:
        //     An XML encoding of the security object, including any state information.
        //public override SecurityElement ToXml()
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Creates a permission object that combines the current permission object and the
        //     target permission object.
        //
        // Parameters:
        //   target:
        //     A permission object to combine with the current permission object. It must be
        //     of the same type as the current permission object.
        //
        // Returns:
        //     A new permission object that represents the union of the current permission object
        //     and the specified permission object.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The target permission object is not of the same type as the current permission
        //     object.
        //public override IPermission Union(IPermission target)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Adds a permission entry to the permission.
        //
        // Parameters:
        //   entry:
        //     The System.Security.Permissions.ResourcePermissionBaseEntry to add.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The specified System.Security.Permissions.ResourcePermissionBaseEntry is null.
        //
        //   T:System.InvalidOperationException:
        //     The number of elements in the System.Security.Permissions.ResourcePermissionBaseEntry.PermissionAccessPath
        //     property is not equal to the number of elements in the System.Security.Permissions.ResourcePermissionBase.TagNames
        //     property.-or- The System.Security.Permissions.ResourcePermissionBaseEntry is
        //     already included in the permission.
        //protected void AddPermissionAccess(ResourcePermissionBaseEntry entry);
        //
        // Summary:
        //     Clears the permission of the added permission entries.
        protected void Clear()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Returns an array of the System.Security.Permissions.ResourcePermissionBaseEntry
        //     objects added to this permission.
        //
        // Returns:
        //     An array of System.Security.Permissions.ResourcePermissionBaseEntry objects that
        //     were added to this permission.
        //protected ResourcePermissionBaseEntry[] GetPermissionEntries();
        //
        // Summary:
        //     Removes a permission entry from the permission.
        //
        // Parameters:
        //   entry:
        //     The System.Security.Permissions.ResourcePermissionBaseEntry to remove.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The specified System.Security.Permissions.ResourcePermissionBaseEntry is null.
        //
        //   T:System.InvalidOperationException:
        //     The number of elements in the System.Security.Permissions.ResourcePermissionBaseEntry.PermissionAccessPath
        //     property is not equal to the number of elements in the System.Security.Permissions.ResourcePermissionBase.TagNames
        //     property.-or- The System.Security.Permissions.ResourcePermissionBaseEntry is
        //     not in the permission.
        //protected void RemovePermissionAccess(ResourcePermissionBaseEntry entry)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
    }
}

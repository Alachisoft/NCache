using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Allows control of code access permissions for System.Diagnostics.PerformanceCounter.
    public sealed class PerformanceCounterPermission : ResourcePermissionBase
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.PerformanceCounterPermission
        //     class.
        public PerformanceCounterPermission()
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.PerformanceCounterPermission
        //     class with the specified permission state.
        //
        // Parameters:
        //   state:
        //     One of the System.Security.Permissions.PermissionState values.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The state parameter is not a valid value of System.Security.Permissions.PermissionState.
        //public PerformanceCounterPermission(PermissionState state);
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.PerformanceCounterPermission
        //     class with the specified permission access level entries.
        //
        // Parameters:
        //   permissionAccessEntries:
        //     An array of System.Diagnostics.PerformanceCounterPermissionEntry objects. The
        //     System.Diagnostics.PerformanceCounterPermission.PermissionEntries property is
        //     set to this value.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     permissionAccessEntries is null.
        //public PerformanceCounterPermission(PerformanceCounterPermissionEntry[] permissionAccessEntries);
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.PerformanceCounterPermission
        //     class with the specified access levels, the name of the computer to use, and
        //     the category associated with the performance counter.
        //
        // Parameters:
        //   permissionAccess:
        //     One of the System.Diagnostics.PerformanceCounterPermissionAccess values.
        //
        //   machineName:
        //     The server on which the performance counter and its associate category reside.
        //
        //   categoryName:
        //     The name of the performance counter category (performance object) with which
        //     the performance counter is associated.
        public PerformanceCounterPermission(PerformanceCounterPermissionAccess permissionAccess, string machineName, string categoryName)
        {
            //TODO: ALACHISOFT
            //throw new NotImplementedException();
        }

        //
        // Summary:
        //     Gets the collection of permission entries for this permissions request.
        //
        // Returns:
        //     A System.Diagnostics.PerformanceCounterPermissionEntryCollection that contains
        //     the permission entries for this permissions request.
        //public PerformanceCounterPermissionEntryCollection PermissionEntries { get; }

        public void Demand()
        {
            //TODO: ALACHISOFT
          //  throw new NotImplementedException();
        }
    }
}

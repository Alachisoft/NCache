using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Security.Permissions
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
    [ComVisible(true)]
    public sealed class HostProtectionAttribute : CodeAccessSecurityAttribute
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Security.Permissions.HostProtectionAttribute
        //     class with default values.
        [SecuritySafeCritical]
        public HostProtectionAttribute() : base(new SecurityAction())
        {

        }
        //
        // Summary:
        //     Initializes a new instance of the System.Security.Permissions.HostProtectionAttribute
        //     class with the specified System.Security.Permissions.SecurityAction value.
        //
        // Parameters:
        //   action:
        //     One of the System.Security.Permissions.SecurityAction values.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     action is not System.Security.Permissions.SecurityAction.LinkDemand.
        public HostProtectionAttribute(SecurityAction action) : base(action)
        {

        }

        //
        // Summary:
        //     Gets or sets flags specifying categories of functionality that are potentially
        //     harmful to the host.
        //
        // Returns:
        //     A bitwise combination of the System.Security.Permissions.HostProtectionResource
        //     values. The default is System.Security.Permissions.HostProtectionResource.None.
        //public HostProtectionResource Resources { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether synchronization is exposed.
        //
        // Returns:
        //     true if synchronization is exposed; otherwise, false. The default is false.
        public bool Synchronization { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether shared state is exposed.
        //
        // Returns:
        //     true if shared state is exposed; otherwise, false. The default is false.
        public bool SharedState { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether external process management is exposed.
        //
        // Returns:
        //     true if external process management is exposed; otherwise, false. The default
        //     is false.
        public bool ExternalProcessMgmt { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether self-affecting process management is
        //     exposed.
        //
        // Returns:
        //     true if self-affecting process management is exposed; otherwise, false. The default
        //     is false.
        public bool SelfAffectingProcessMgmt { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether external threading is exposed.
        //
        // Returns:
        //     true if external threading is exposed; otherwise, false. The default is false.
        public bool ExternalThreading { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether self-affecting threading is exposed.
        //
        // Returns:
        //     true if self-affecting threading is exposed; otherwise, false. The default is
        //     false.
        public bool SelfAffectingThreading { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the security infrastructure is exposed.
        //
        // Returns:
        //     true if the security infrastructure is exposed; otherwise, false. The default
        //     is false.
        [ComVisible(true)]
        public bool SecurityInfrastructure { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the user interface is exposed.
        //
        // Returns:
        //     true if the user interface is exposed; otherwise, false. The default is false.
        public bool UI { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether resources might leak memory if the operation
        //     is terminated.
        //
        // Returns:
        //     true if resources might leak memory on termination; otherwise, false.
        public bool MayLeakOnAbort { get; set; }

        public override IPermission CreatePermission()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}

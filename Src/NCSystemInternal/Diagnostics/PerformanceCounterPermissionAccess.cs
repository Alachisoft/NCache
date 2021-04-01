using System;
using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Defines access levels used by System.Diagnostics.PerformanceCounter permission
    //     classes.
    [Flags]
    public enum PerformanceCounterPermissionAccess
    {
        //
        // Summary:
        //     The System.Diagnostics.PerformanceCounter has no permissions.
        None = 0,
        //
        // Summary:
        //     The System.Diagnostics.PerformanceCounter can read categories.
        Browse = 1,
        //
        // Summary:
        //     The System.Diagnostics.PerformanceCounter can read categories.
        Read = 1,
        //
        // Summary:
        //     The System.Diagnostics.PerformanceCounter can write categories.
        Write = 2,
        //
        // Summary:
        //     The System.Diagnostics.PerformanceCounter can read and write categories.
        Instrument = 3,
        //
        // Summary:
        //     The System.Diagnostics.PerformanceCounter can read, write, and create categories.
        Administer = 7
    }
}

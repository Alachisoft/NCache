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

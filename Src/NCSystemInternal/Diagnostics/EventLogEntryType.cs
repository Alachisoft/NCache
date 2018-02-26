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
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Specifies the event type of an event log entry.
    public enum EventLogEntryType
    {
        //
        // Summary:
        //     An error event. This indicates a significant problem the user should know about;
        //     usually a loss of functionality or data.
        Error = 1,
        //
        // Summary:
        //     A warning event. This indicates a problem that is not immediately significant,
        //     but that may signify conditions that could cause future problems.
        Warning = 2,
        //
        // Summary:
        //     An information event. This indicates a significant, successful operation.
        Information = 4,
        //
        // Summary:
        //     A success audit event. This indicates a security event that occurs when an audited
        //     access attempt is successful; for example, logging on successfully.
        SuccessAudit = 8,
        //
        // Summary:
        //     A failure audit event. This indicates a security event that occurs when an audited
        //     access attempt fails; for example, a failed attempt to open a file.
        FailureAudit = 16
    }
}

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

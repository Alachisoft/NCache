using System;
using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Specifies how to handle entries in an event log that has reached its maximum
    //     file size.
    public enum OverflowAction
    {
        //
        // Summary:
        //     Indicates that existing entries are retained when the event log is full and new
        //     entries are discarded.
        DoNotOverwrite = -1,
        //
        // Summary:
        //     Indicates that each new entry overwrites the oldest entry when the event log
        //     is full.
        OverwriteAsNeeded = 0,
        //
        // Summary:
        //     Indicates that new events overwrite events older than specified by the System.Diagnostics.EventLog.MinimumRetentionDays
        //     property value when the event log is full. New events are discarded if the event
        //     log is full and there are no events older than specified by the System.Diagnostics.EventLog.MinimumRetentionDays
        //     property value.
        OverwriteOlder = 1
    }
}

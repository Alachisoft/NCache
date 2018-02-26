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

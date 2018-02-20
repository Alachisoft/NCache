using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Encapsulates a single record in the event log. This class cannot be inherited.
    [DesignTimeVisible(false)]
    [ToolboxItem(false)]
    public sealed class EventLogEntry : Component, ISerializable
    {
        //
        // Summary:
        //     Gets the name of the computer on which this entry was generated.
        //
        // Returns:
        //     The name of the computer that contains the event log.
        [MonitoringDescription("LogEntryMachineName")]
        public string MachineName { get; }
        //
        // Summary:
        //     Gets the binary data associated with the entry.
        //
        // Returns:
        //     An array of bytes that holds the binary data associated with the entry.
        [MonitoringDescription("LogEntryData")]
        public byte[] Data { get; }
        //
        // Summary:
        //     Gets the index of this entry in the event log.
        //
        // Returns:
        //     The index of this entry in the event log.
        [MonitoringDescription("LogEntryIndex")]
        public int Index { get; }
        //
        // Summary:
        //     Gets the text associated with the System.Diagnostics.EventLogEntry.CategoryNumber
        //     property for this entry.
        //
        // Returns:
        //     The application-specific category text.
        //
        // Exceptions:
        //   T:System.Exception:
        //     The space could not be allocated for one of the insertion strings associated
        //     with the category.
        [MonitoringDescription("LogEntryCategory")]
        public string Category { get; }
        //
        // Summary:
        //     Gets the category number of the event log entry.
        //
        // Returns:
        //     The application-specific category number for this entry.
        [MonitoringDescription("LogEntryCategoryNumber")]
        public short CategoryNumber { get; }
        //
        // Summary:
        //     Gets the application-specific event identifier for the current event entry.
        //
        // Returns:
        //     The application-specific identifier for the event message.
        [MonitoringDescription("LogEntryEventID")]
        [Obsolete("This property has been deprecated.  Please use System.Diagnostics.EventLogEntry.InstanceId instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public int EventID { get; }
        //
        // Summary:
        //     Gets the event type of this entry.
        //
        // Returns:
        //     The event type that is associated with the entry in the event log.
        [MonitoringDescription("LogEntryEntryType")]
        public EventLogEntryType EntryType { get; }
        //
        // Summary:
        //     Gets the localized message associated with this event entry.
        //
        // Returns:
        //     The formatted, localized text for the message. This includes associated replacement
        //     strings.
        //
        // Exceptions:
        //   T:System.Exception:
        //     The space could not be allocated for one of the insertion strings associated
        //     with the message.
        [Editor("System.ComponentModel.Design.BinaryEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [MonitoringDescription("LogEntryMessage")]
        public string Message { get; }
        //
        // Summary:
        //     Gets the name of the application that generated this event.
        //
        // Returns:
        //     The name registered with the event log as the source of this event.
        [MonitoringDescription("LogEntrySource")]
        public string Source { get; }
        //
        // Summary:
        //     Gets the replacement strings associated with the event log entry.
        //
        // Returns:
        //     An array that holds the replacement strings stored in the event entry.
        [MonitoringDescription("LogEntryReplacementStrings")]
        public string[] ReplacementStrings { get; }
        //
        // Summary:
        //     Gets the resource identifier that designates the message text of the event entry.
        //
        // Returns:
        //     A resource identifier that corresponds to a string definition in the message
        //     resource file of the event source.
        [ComVisible(false)]
        [MonitoringDescription("LogEntryResourceId")]
        public long InstanceId { get; }
        //
        // Summary:
        //     Gets the local time at which this event was generated.
        //
        // Returns:
        //     The local time at which this event was generated.
        [MonitoringDescription("LogEntryTimeGenerated")]
        public DateTime TimeGenerated { get; }
        //
        // Summary:
        //     Gets the local time at which this event was written to the log.
        //
        // Returns:
        //     The local time at which this event was written to the log.
        [MonitoringDescription("LogEntryTimeWritten")]
        public DateTime TimeWritten { get; }
        //
        // Summary:
        //     Gets the name of the user who is responsible for this event.
        //
        // Returns:
        //     The security identifier (SID) that uniquely identifies a user or group.
        //
        // Exceptions:
        //   T:System.SystemException:
        //     Account information could not be obtained for the user's SID.
        [MonitoringDescription("LogEntryUserName")]
        public string UserName { get; }

        //
        // Summary:
        //     Performs a comparison between two event log entries.
        //
        // Parameters:
        //   otherEntry:
        //     The System.Diagnostics.EventLogEntry to compare.
        //
        // Returns:
        //     true if the System.Diagnostics.EventLogEntry objects are identical; otherwise,
        //     false.
        public bool Equals(EventLogEntry otherEntry)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        #region Auto-Generated by me 
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        #endregion
    }
}

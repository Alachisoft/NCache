using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Provides interaction with Windows event logs.
    [DefaultEvent("EntryWritten")]
    [InstallerType("System.Diagnostics.EventLogInstaller, System.Configuration.Install, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [MonitoringDescription("EventLogDesc")]
    public class EventLog : Component, ISupportInitialize
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.EventLog class. Does not
        //     associate the instance with any log.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public EventLog()
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.EventLog class. Associates
        //     the instance with a log on the local computer.
        //
        // Parameters:
        //   logName:
        //     The name of the log on the local computer.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The log name is null.
        //
        //   T:System.ArgumentException:
        //     The log name is invalid.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public EventLog(string logName)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.EventLog class. Associates
        //     the instance with a log on the specified computer.
        //
        // Parameters:
        //   logName:
        //     The name of the log on the specified computer.
        //
        //   machineName:
        //     The computer on which the log exists.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The log name is null.
        //
        //   T:System.ArgumentException:
        //     The log name is invalid.-or- The computer name is invalid.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public EventLog(string logName, string machineName)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.EventLog class. Associates
        //     the instance with a log on the specified computer and creates or assigns the
        //     specified source to the System.Diagnostics.EventLog.
        //
        // Parameters:
        //   logName:
        //     The name of the log on the specified computer
        //
        //   machineName:
        //     The computer on which the log exists.
        //
        //   source:
        //     The source of event log entries.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The log name is null.
        //
        //   T:System.ArgumentException:
        //     The log name is invalid.-or- The computer name is invalid.
        public EventLog(string logName, string machineName, string source)
        {
            //TODO: ALACHISOFT
        }

        //
        // Summary:
        //     Gets or sets the object used to marshal the event handler calls issued as a result
        //     of an System.Diagnostics.EventLog entry written event.
        //
        // Returns:
        //     The System.ComponentModel.ISynchronizeInvoke used to marshal event-handler calls
        //     issued as a result of an System.Diagnostics.EventLog.EntryWritten event on the
        //     event log.
        [Browsable(false)]
        [DefaultValue(null)]
        [MonitoringDescription("LogSynchronizingObject")]
        public ISynchronizeInvoke SynchronizingObject { get; set; }
        //
        // Summary:
        //     Gets or sets a value indicating whether the System.Diagnostics.EventLog receives
        //     System.Diagnostics.EventLog.EntryWritten event notifications.
        //
        // Returns:
        //     true if the System.Diagnostics.EventLog receives notification when an entry is
        //     written to the log; otherwise, false.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The event log is on a remote computer.
        [Browsable(false)]
        [DefaultValue(false)]
        [MonitoringDescription("LogMonitoring")]
        public bool EnableRaisingEvents { get; set; }
        //
        // Summary:
        //     Gets the number of days to retain entries in the event log.
        //
        // Returns:
        //     The number of days that entries in the event log are retained. The default value
        //     is 7.
        [Browsable(false)]
        [ComVisible(false)]
        public int MinimumRetentionDays { get; }
        //
        // Summary:
        //     Gets the configured behavior for storing new entries when the event log reaches
        //     its maximum log file size.
        //
        // Returns:
        //     The System.Diagnostics.OverflowAction value that specifies the configured behavior
        //     for storing new entries when the event log reaches its maximum log size. The
        //     default is System.Diagnostics.OverflowAction.OverwriteOlder.
        [Browsable(false)]
        [ComVisible(false)]
        public OverflowAction OverflowAction { get; }
        //
        // Summary:
        //     Gets or sets the maximum event log size in kilobytes.
        //
        // Returns:
        //     The maximum event log size in kilobytes. The default is 512, indicating a maximum
        //     file size of 512 kilobytes.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     The specified value is less than 64, or greater than 4194240, or not an even
        //     multiple of 64.
        //
        //   T:System.InvalidOperationException:
        //     The System.Diagnostics.EventLog.Log value is not a valid log name.- or -The registry
        //     key for the event log could not be opened on the target computer.
        [Browsable(false)]
        [ComVisible(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long MaximumKilobytes { get; set; }
        //
        // Summary:
        //     Gets or sets the name of the computer on which to read or write events.
        //
        // Returns:
        //     The name of the server on which the event log resides. The default is the local
        //     computer (".").
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The computer name is invalid.
        [DefaultValue(".")]
        [MonitoringDescription("LogMachineName")]
        [ReadOnly(true)]
        [SettingsBindable(true)]
        public string MachineName { get; set; }
        //
        // Summary:
        //     Gets or sets the name of the log to read from or write to.
        //
        // Returns:
        //     The name of the log. This can be Application, System, Security, or a custom log
        //     name. The default is an empty string ("").
        [DefaultValue("")]
        [MonitoringDescription("LogLog")]
        [ReadOnly(true)]
        [SettingsBindable(true)]
        [TypeConverter("System.Diagnostics.Design.LogConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string Log { get; set; }
        //
        // Summary:
        //     Gets the event log's friendly name.
        //
        // Returns:
        //     A name that represents the event log in the system's event viewer.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The specified System.Diagnostics.EventLog.Log does not exist in the registry
        //     for this computer.
        [Browsable(false)]
        public string LogDisplayName { get; }
        //
        // Summary:
        //     Gets the contents of the event log.
        //
        // Returns:
        //     An System.Diagnostics.EventLogEntryCollection holding the entries in the event
        //     log. Each entry is associated with an instance of the System.Diagnostics.EventLogEntry
        //     class.
        //[Browsable(false)]
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[MonitoringDescription("LogEntries")]
        public EventLogEntryCollection Entries { get; }
        //
        // Summary:
        //     Gets or sets the source name to register and use when writing to the event log.
        //
        // Returns:
        //     The name registered with the event log as a source of entries. The default is
        //     an empty string ("").
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source name results in a registry key path longer than 254 characters.
        [DefaultValue("")]
        [MonitoringDescription("LogSource")]
        [ReadOnly(true)]
        [SettingsBindable(true)]
        [TypeConverter("System.Diagnostics.Design.StringValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string Source { get; set; }

        //
        // Summary:
        //     Occurs when an entry is written to an event log on the local computer.
    //    [MonitoringDescription("LogEntryWritten")]
    //    public event EntryWrittenEventHandler EntryWritten
    //    {
    //        //TODO: ALACHISOFT
    //        throw new NotImplementedException();
    //    }

    //
    // Summary:
    //     Establishes a valid event source for writing localized event messages, using
    //     the specified configuration properties for the event source and the corresponding
    //     event log.
    //
    // Parameters:
    //   sourceData:
    //     The configuration properties for the event source and its target event log.
    //
    // Exceptions:
    //   T:System.ArgumentException:
    //     The computer name specified in sourceData is not valid.- or - The source name
    //     specified in sourceData is null.- or - The log name specified in sourceData is
    //     not valid. Event log names must consist of printable characters and cannot include
    //     the characters '*', '?', or '\'.- or - The log name specified in sourceData is
    //     not valid for user log creation. The Event log names AppEvent, SysEvent, and
    //     SecEvent are reserved for system use.- or - The log name matches an existing
    //     event source name.- or - The source name specified in sourceData results in a
    //     registry key path longer than 254 characters.- or - The first 8 characters of
    //     the log name specified in sourceData are not unique.- or - The source name specified
    //     in sourceData is already registered.- or - The source name specified in sourceData
    //     matches an existing event log name.
    //
    //   T:System.InvalidOperationException:
    //     The registry key for the event log could not be opened.
    //
    //   T:System.ArgumentNullException:
    //     sourceData is null.
    //public static void CreateEventSource(EventSourceCreationData sourceData)
    //    {
    //        //TODO: ALACHISOFT
    //        throw new NotImplementedException();
    //    }
        //
        // Summary:
        //     Establishes the specified source name as a valid event source for writing entries
        //     to a log on the local computer. This method can also create a new custom log
        //     on the local computer.
        //
        // Parameters:
        //   source:
        //     The source name by which the application is registered on the local computer.
        //
        //   logName:
        //     The name of the log the source's entries are written to. Possible values include
        //     Application, System, or a custom event log.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     source is an empty string ("") or null.- or - logName is not a valid event log
        //     name. Event log names must consist of printable characters, and cannot include
        //     the characters '*', '?', or '\'.- or - logName is not valid for user log creation.
        //     The event log names AppEvent, SysEvent, and SecEvent are reserved for system
        //     use.- or - The log name matches an existing event source name.- or - The source
        //     name results in a registry key path longer than 254 characters.- or - The first
        //     8 characters of logName match the first 8 characters of an existing event log
        //     name.- or - The source cannot be registered because it already exists on the
        //     local computer.- or - The source name matches an existing event log name.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened on the local computer.
        public static void CreateEventSource(string source, string logName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Establishes the specified source name as a valid event source for writing entries
        //     to a log on the specified computer. This method can also be used to create a
        //     new custom log on the specified computer.
        //
        // Parameters:
        //   source:
        //     The source by which the application is registered on the specified computer.
        //
        //   logName:
        //     The name of the log the source's entries are written to. Possible values include
        //     Application, System, or a custom event log. If you do not specify a value, logName
        //     defaults to Application.
        //
        //   machineName:
        //     The name of the computer to register this event source with, or "." for the local
        //     computer.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The machineName is not a valid computer name.- or - source is an empty string
        //     ("") or null.- or - logName is not a valid event log name. Event log names must
        //     consist of printable characters, and cannot include the characters '*', '?',
        //     or '\'.- or - logName is not valid for user log creation. The event log names
        //     AppEvent, SysEvent, and SecEvent are reserved for system use.- or - The log name
        //     matches an existing event source name.- or - The source name results in a registry
        //     key path longer than 254 characters.- or - The first 8 characters of logName
        //     match the first 8 characters of an existing event log name on the specified computer.-
        //     or - The source cannot be registered because it already exists on the specified
        //     computer.- or - The source name matches an existing event source name.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened on the specified computer.
        [Obsolete("This method has been deprecated.  Please use System.Diagnostics.EventLog.CreateEventSource(EventSourceCreationData sourceData) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public static void CreateEventSource(string source, string logName, string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Removes an event log from the specified computer.
        //
        // Parameters:
        //   logName:
        //     The name of the log to delete. Possible values include: Application, Security,
        //     System, and any custom event logs on the specified computer.
        //
        //   machineName:
        //     The name of the computer to delete the log from, or "." for the local computer.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     logName is an empty string ("") or null. - or - machineName is not a valid computer
        //     name.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened on the specified computer.-
        //     or - The log does not exist on the specified computer.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The event log was not cleared successfully.-or- The log cannot be opened. A Windows
        //     error code is not available.
        public static void Delete(string logName, string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Removes an event log from the local computer.
        //
        // Parameters:
        //   logName:
        //     The name of the log to delete. Possible values include: Application, Security,
        //     System, and any custom event logs on the computer.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     logName is an empty string ("") or null.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened on the local computer.-
        //     or - The log does not exist on the local computer.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The event log was not cleared successfully.-or- The log cannot be opened. A Windows
        //     error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void Delete(string logName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Removes the event source registration from the event log of the local computer.
        //
        // Parameters:
        //   source:
        //     The name by which the application is registered in the event log system.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source parameter does not exist in the registry of the local computer.- or
        //     - You do not have write access on the registry key for the event log.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void DeleteEventSource(string source)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Removes the application's event source registration from the specified computer.
        //
        // Parameters:
        //   source:
        //     The name by which the application is registered in the event log system.
        //
        //   machineName:
        //     The name of the computer to remove the registration from, or "." for the local
        //     computer.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The machineName parameter is invalid. - or - The source parameter does not exist
        //     in the registry of the specified computer.- or - You do not have write access
        //     on the registry key for the event log.
        //
        //   T:System.InvalidOperationException:
        //     source cannot be deleted because in the registry, the parent registry key for
        //     source does not contain a subkey with the same name.
        public static void DeleteEventSource(string source, string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Determines whether the log exists on the local computer.
        //
        // Parameters:
        //   logName:
        //     The name of the log to search for. Possible values include: Application, Security,
        //     System, other application-specific logs (such as those associated with Active
        //     Directory), or any custom log on the computer.
        //
        // Returns:
        //     true if the log exists on the local computer; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The logName is null or the value is empty.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static bool Exists(string logName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Determines whether the log exists on the specified computer.
        //
        // Parameters:
        //   logName:
        //     The log for which to search. Possible values include: Application, Security,
        //     System, other application-specific logs (such as those associated with Active
        //     Directory), or any custom log on the computer.
        //
        //   machineName:
        //     The name of the computer on which to search for the log, or "." for the local
        //     computer.
        //
        // Returns:
        //     true if the log exists on the specified computer; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The machineName parameter is an invalid format. Make sure you have used proper
        //     syntax for the computer on which you are searching.-or- The logName is null or
        //     the value is empty.
        public static bool Exists(string logName, string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Searches for all event logs on the local computer and creates an array of System.Diagnostics.EventLog
        //     objects that contain the list.
        //
        // Returns:
        //     An array of type System.Diagnostics.EventLog that represents the logs on the
        //     local computer.
        //
        // Exceptions:
        //   T:System.SystemException:
        //     You do not have read access to the registry.-or- There is no event log service
        //     on the computer.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static EventLog[] GetEventLogs()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Searches for all event logs on the given computer and creates an array of System.Diagnostics.EventLog
        //     objects that contain the list.
        //
        // Parameters:
        //   machineName:
        //     The computer on which to search for event logs.
        //
        // Returns:
        //     An array of type System.Diagnostics.EventLog that represents the logs on the
        //     given computer.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The machineName parameter is an invalid computer name.
        //
        //   T:System.InvalidOperationException:
        //     You do not have read access to the registry.-or- There is no event log service
        //     on the computer.
        public static EventLog[] GetEventLogs(string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Gets the name of the log to which the specified source is registered.
        //
        // Parameters:
        //   source:
        //     The name of the event source.
        //
        //   machineName:
        //     The name of the computer on which to look, or "." for the local computer.
        //
        // Returns:
        //     The name of the log associated with the specified source in the registry.
        public static string LogNameFromSourceName(string source, string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Determines whether an event source is registered on the local computer.
        //
        // Parameters:
        //   source:
        //     The name of the event source.
        //
        // Returns:
        //     true if the event source is registered on the local computer; otherwise, false.
        //
        // Exceptions:
        //   T:System.Security.SecurityException:
        //     source was not found, but some or all of the event logs could not be searched.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static bool SourceExists(string source)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Determines whether an event source is registered on a specified computer.
        //
        // Parameters:
        //   source:
        //     The name of the event source.
        //
        //   machineName:
        //     The name the computer on which to look, or "." for the local computer.
        //
        // Returns:
        //     true if the event source is registered on the given computer; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     machineName is an invalid computer name.
        //
        //   T:System.Security.SecurityException:
        //     source was not found, but some or all of the event logs could not be searched.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static bool SourceExists(string source, string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an entry with the given message text, application-defined event identifier,
        //     and application-defined category to the event log, using the specified registered
        //     event source. The category can be used by the Event Viewer to filter events in
        //     the log.
        //
        // Parameters:
        //   source:
        //     The source by which the application is registered on the specified computer.
        //
        //   message:
        //     The string to write to the event log.
        //
        //   type:
        //     One of the System.Diagnostics.EventLogEntryType values.
        //
        //   eventID:
        //     The application-specific identifier for the event.
        //
        //   category:
        //     The application-specific subcategory associated with the message.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source value is an empty string ("").- or -The source value is null.- or
        //     -eventID is less than zero or greater than System.UInt16.MaxValue.- or -The message
        //     string is longer than 32766 bytes.- or -The source name results in a registry
        //     key path longer than 254 characters.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     type is not a valid System.Diagnostics.EventLogEntryType.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID, short category)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an entry with the given message text, application-defined event identifier,
        //     and application-defined category to the event log (using the specified registered
        //     event source) and appends binary data to the message.
        //
        // Parameters:
        //   source:
        //     The source by which the application is registered on the specified computer.
        //
        //   message:
        //     The string to write to the event log.
        //
        //   type:
        //     One of the System.Diagnostics.EventLogEntryType values.
        //
        //   eventID:
        //     The application-specific identifier for the event.
        //
        //   category:
        //     The application-specific subcategory associated with the message.
        //
        //   rawData:
        //     An array of bytes that holds the binary data associated with the entry.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source value is an empty string ("").- or -The source value is null.- or
        //     -eventID is less than zero or greater than System.UInt16.MaxValue.- or -The message
        //     string is longer than 32766 bytes.- or -The source name results in a registry
        //     key path longer than 254 characters.
        //
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     type is not a valid System.Diagnostics.EventLogEntryType.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID, short category, byte[] rawData)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an information type entry with the given message text to the event log,
        //     using the specified registered event source.
        //
        // Parameters:
        //   source:
        //     The source by which the application is registered on the specified computer.
        //
        //   message:
        //     The string to write to the event log.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source value is an empty string ("").- or -The source value is null.- or
        //     -The message string is longer than 32766 bytes.- or -The source name results
        //     in a registry key path longer than 254 characters.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void WriteEntry(string source, string message)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an error, warning, information, success audit, or failure audit entry
        //     with the given message text to the event log, using the specified registered
        //     event source.
        //
        // Parameters:
        //   source:
        //     The source by which the application is registered on the specified computer.
        //
        //   message:
        //     The string to write to the event log.
        //
        //   type:
        //     One of the System.Diagnostics.EventLogEntryType values.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source value is an empty string ("").- or -The source value is null.- or
        //     -The message string is longer than 32766 bytes.- or -The source name results
        //     in a registry key path longer than 254 characters.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     type is not a valid System.Diagnostics.EventLogEntryType.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void WriteEntry(string source, string message, EventLogEntryType type)
        {
            //TODO: ALACHISOFT
            //throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an entry with the given message text and application-defined event identifier
        //     to the event log, using the specified registered event source.
        //
        // Parameters:
        //   source:
        //     The source by which the application is registered on the specified computer.
        //
        //   message:
        //     The string to write to the event log.
        //
        //   type:
        //     One of the System.Diagnostics.EventLogEntryType values.
        //
        //   eventID:
        //     The application-specific identifier for the event.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source value is an empty string ("").- or -The source value is null.- or
        //     -eventID is less than zero or greater than System.UInt16.MaxValue.- or -The message
        //     string is longer than 32766 bytes.- or -The source name results in a registry
        //     key path longer than 254 characters.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     type is not a valid System.Diagnostics.EventLogEntryType.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void WriteEntry(string source, string message, EventLogEntryType type, int eventID)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an event log entry with the given event data and message replacement strings,
        //     using the specified registered event source.
        //
        // Parameters:
        //   source:
        //     The name of the event source registered for the application on the specified
        //     computer.
        //
        //   instance:
        //     An System.Diagnostics.EventInstance instance that represents a localized event
        //     log entry.
        //
        //   values:
        //     An array of strings to merge into the message text of the event log entry.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source value is an empty string ("").- or -The source value is null.- or
        //     -instance.InstanceId is less than zero or greater than System.UInt16.MaxValue.-
        //     or -values has more than 256 elements.- or -One of the values elements is longer
        //     than 32766 bytes.- or -The source name results in a registry key path longer
        //     than 254 characters.
        //
        //   T:System.ArgumentNullException:
        //     instance is null.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        //public static void WriteEvent(string source, EventInstance instance, params object[] values)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Writes an event log entry with the given event data, message replacement strings,
        //     and associated binary data, and using the specified registered event source.
        //
        // Parameters:
        //   source:
        //     The name of the event source registered for the application on the specified
        //     computer.
        //
        //   instance:
        //     An System.Diagnostics.EventInstance instance that represents a localized event
        //     log entry.
        //
        //   data:
        //     An array of bytes that holds the binary data associated with the entry.
        //
        //   values:
        //     An array of strings to merge into the message text of the event log entry.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The source value is an empty string ("").- or -The source value is null.- or
        //     -instance.InstanceId is less than zero or greater than System.UInt16.MaxValue.-
        //     or -values has more than 256 elements.- or -One of the values elements is longer
        //     than 32766 bytes.- or -The source name results in a registry key path longer
        //     than 254 characters.
        //
        //   T:System.ArgumentNullException:
        //     instance is null.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        //public static void WriteEvent(string source, EventInstance instance, byte[] data, params object[] values)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Begins the initialization of an System.Diagnostics.EventLog used on a form or
        //     used by another component. The initialization occurs at runtime.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     System.Diagnostics.EventLog is already initialized.
        public void BeginInit()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Removes all entries from the event log.
        //
        // Exceptions:
        //   T:System.ComponentModel.Win32Exception:
        //     The event log was not cleared successfully.-or- The log cannot be opened. A Windows
        //     error code is not available.
        //
        //   T:System.ArgumentException:
        //     A value is not specified for the System.Diagnostics.EventLog.Log property. Make
        //     sure the log name is not an empty string.
        //
        //   T:System.InvalidOperationException:
        //     The log does not exist.
        public void Clear()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Closes the event log and releases read and write handles.
        //
        // Exceptions:
        //   T:System.ComponentModel.Win32Exception:
        //     The event log's read handle or write handle was not released successfully.
        public void Close()
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Ends the initialization of an System.Diagnostics.EventLog used on a form or by
        //     another component. The initialization occurs at runtime.
        public void EndInit()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Changes the configured behavior for writing new entries when the event log reaches
        //     its maximum file size.
        //
        // Parameters:
        //   action:
        //     The overflow behavior for writing new entries to the event log.
        //
        //   retentionDays:
        //     The minimum number of days each event log entry is retained. This parameter is
        //     used only if action is set to System.Diagnostics.OverflowAction.OverwriteOlder.
        //
        // Exceptions:
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     action is not a valid System.Diagnostics.EventLog.OverflowAction value.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     retentionDays is less than one, or larger than 365.
        //
        //   T:System.InvalidOperationException:
        //     The System.Diagnostics.EventLog.Log value is not a valid log name.- or -The registry
        //     key for the event log could not be opened on the target computer.
        [ComVisible(false)]
        public void ModifyOverflowPolicy(OverflowAction action, int retentionDays)
        {
            //TODO: ALACHISOFT
            //throw new NotImplementedException();
        }
        //
        // Summary:
        //     Specifies the localized name of the event log, which is displayed in the server
        //     Event Viewer.
        //
        // Parameters:
        //   resourceFile:
        //     The fully specified path to a localized resource file.
        //
        //   resourceId:
        //     The resource identifier that indexes a localized string within the resource file.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The System.Diagnostics.EventLog.Log value is not a valid log name.- or -The registry
        //     key for the event log could not be opened on the target computer.
        //
        //   T:System.ArgumentNullException:
        //     resourceFile is null.
        [ComVisible(false)]
        public void RegisterDisplayName(string resourceFile, long resourceId)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an entry with the given message text, application-defined event identifier,
        //     and application-defined category to the event log, and appends binary data to
        //     the message.
        //
        // Parameters:
        //   message:
        //     The string to write to the event log.
        //
        //   type:
        //     One of the System.Diagnostics.EventLogEntryType values.
        //
        //   eventID:
        //     The application-specific identifier for the event.
        //
        //   category:
        //     The application-specific subcategory associated with the message.
        //
        //   rawData:
        //     An array of bytes that holds the binary data associated with the entry.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.EventLog.Source property of the System.Diagnostics.EventLog
        //     has not been set.-or- The method attempted to register a new event source, but
        //     the computer name in System.Diagnostics.EventLog.MachineName is not valid.- or
        //     -The source is already registered for a different event log.- or -eventID is
        //     less than zero or greater than System.UInt16.MaxValue.- or -The message string
        //     is longer than 32766 bytes.- or -The source name results in a registry key path
        //     longer than 254 characters.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     type is not a valid System.Diagnostics.EventLogEntryType.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category, byte[] rawData)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an entry with the given message text, application-defined event identifier,
        //     and application-defined category to the event log.
        //
        // Parameters:
        //   message:
        //     The string to write to the event log.
        //
        //   type:
        //     One of the System.Diagnostics.EventLogEntryType values.
        //
        //   eventID:
        //     The application-specific identifier for the event.
        //
        //   category:
        //     The application-specific subcategory associated with the message.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.EventLog.Source property of the System.Diagnostics.EventLog
        //     has not been set.-or- The method attempted to register a new event source, but
        //     the computer name in System.Diagnostics.EventLog.MachineName is not valid.- or
        //     -The source is already registered for a different event log.- or -eventID is
        //     less than zero or greater than System.UInt16.MaxValue.- or -The message string
        //     is longer than 32766 bytes.- or -The source name results in a registry key path
        //     longer than 254 characters.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     type is not a valid System.Diagnostics.EventLogEntryType.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category)
        {
            //TODO: ALACHISOFT
            //throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an entry with the given message text and application-defined event identifier
        //     to the event log.
        //
        // Parameters:
        //   message:
        //     The string to write to the event log.
        //
        //   type:
        //     One of the System.Diagnostics.EventLogEntryType values.
        //
        //   eventID:
        //     The application-specific identifier for the event.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.EventLog.Source property of the System.Diagnostics.EventLog
        //     has not been set.-or- The method attempted to register a new event source, but
        //     the computer name in System.Diagnostics.EventLog.MachineName is not valid.- or
        //     -The source is already registered for a different event log.- or -eventID is
        //     less than zero or greater than System.UInt16.MaxValue.- or -The message string
        //     is longer than 32766 bytes.- or -The source name results in a registry key path
        //     longer than 254 characters.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     type is not a valid System.Diagnostics.EventLogEntryType.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void WriteEntry(string message, EventLogEntryType type, int eventID)
        {
            //TODO: ALACHISOFT
            //this is getting called at a few places so commenting out the notImplemented exception
            //throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an error, warning, information, success audit, or failure audit entry
        //     with the given message text to the event log.
        //
        // Parameters:
        //   message:
        //     The string to write to the event log.
        //
        //   type:
        //     One of the System.Diagnostics.EventLogEntryType values.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.EventLog.Source property of the System.Diagnostics.EventLog
        //     has not been set.-or- The method attempted to register a new event source, but
        //     the computer name in System.Diagnostics.EventLog.MachineName is not valid.- or
        //     -The source is already registered for a different event log.- or -The message
        //     string is longer than 32766 bytes.- or -The source name results in a registry
        //     key path longer than 254 characters.
        //
        //   T:System.ComponentModel.InvalidEnumArgumentException:
        //     type is not a valid System.Diagnostics.EventLogEntryType.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void WriteEntry(string message, EventLogEntryType type)
        {
            //TODO: ALACHISOFT
          //  throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes an information type entry, with the given message text, to the event log.
        //
        // Parameters:
        //   message:
        //     The string to write to the event log.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.EventLog.Source property of the System.Diagnostics.EventLog
        //     has not been set.-or- The method attempted to register a new event source, but
        //     the computer name in System.Diagnostics.EventLog.MachineName is not valid.- or
        //     -The source is already registered for a different event log.- or -The message
        //     string is longer than 32766 bytes.- or -The source name results in a registry
        //     key path longer than 254 characters.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void WriteEntry(string message)
        {
            //TODO: ALACHISOFT
            //throw new NotImplementedException();
        }
        //
        // Summary:
        //     Writes a localized entry to the event log.
        //
        // Parameters:
        //   instance:
        //     An System.Diagnostics.EventInstance instance that represents a localized event
        //     log entry.
        //
        //   values:
        //     An array of strings to merge into the message text of the event log entry.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.EventLog.Source property of the System.Diagnostics.EventLog
        //     has not been set.-or- The method attempted to register a new event source, but
        //     the computer name in System.Diagnostics.EventLog.MachineName is not valid.- or
        //     -The source is already registered for a different event log.- or -instance.InstanceId
        //     is less than zero or greater than System.UInt16.MaxValue.- or -values has more
        //     than 256 elements.- or -One of the values elements is longer than 32766 bytes.-
        //     or -The source name results in a registry key path longer than 254 characters.
        //
        //   T:System.ArgumentNullException:
        //     instance is null.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        //[ComVisible(false)]
        //[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        //public void WriteEvent(EventInstance instance, params object[] values)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Writes an event log entry with the given event data, message replacement strings,
        //     and associated binary data.
        //
        // Parameters:
        //   instance:
        //     An System.Diagnostics.EventInstance instance that represents a localized event
        //     log entry.
        //
        //   data:
        //     An array of bytes that holds the binary data associated with the entry.
        //
        //   values:
        //     An array of strings to merge into the message text of the event log entry.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.EventLog.Source property of the System.Diagnostics.EventLog
        //     has not been set.-or- The method attempted to register a new event source, but
        //     the computer name in System.Diagnostics.EventLog.MachineName is not valid.- or
        //     -The source is already registered for a different event log.- or -instance.InstanceId
        //     is less than zero or greater than System.UInt16.MaxValue.- or -values has more
        //     than 256 elements.- or -One of the values elements is longer than 32766 bytes.-
        //     or -The source name results in a registry key path longer than 254 characters.
        //
        //   T:System.ArgumentNullException:
        //     instance is null.
        //
        //   T:System.InvalidOperationException:
        //     The registry key for the event log could not be opened.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     The operating system reported an error when writing the event entry to the event
        //     log. A Windows error code is not available.
        //[ComVisible(false)]
        //public void WriteEvent(EventInstance instance, byte[] data, params object[] values)
        //{
        //    //TODO: ALACHISOFT
        //    throw new NotImplementedException();
        //}
        //
        // Summary:
        //     Releases the unmanaged resources used by the System.Diagnostics.EventLog, and
        //     optionally releases the managed resources.
        //
        // Parameters:
        //   disposing:
        //     true to release both managed and unmanaged resources; false to release only unmanaged
        //     resources.
        protected override void Dispose(bool disposing)
        {
                //TODO: ALACHISOFT
        }
    }
}

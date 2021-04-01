using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Represents a Windows NT performance counter component.
    [InstallerType("System.Diagnostics.PerformanceCounterInstaller,System.Configuration.Install, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    //[SRDescriptionAttribute("PerformanceCounterDesc")]
    public sealed class PerformanceCounter : Component, ISupportInitialize
    {
        //
        // Summary:
        //     Specifies the size, in bytes, of the global memory shared by performance counters.
        //     The default size is 524,288 bytes.
        [Obsolete("This field has been deprecated and is not used.  Use machine.config or an application configuration file to set the size of the PerformanceCounter file mapping.")]
        public static int DefaultFileMappingSize;

        //
        // Summary:
        //     Initializes a new, read-only instance of the System.Diagnostics.PerformanceCounter
        //     class, without associating the instance with any system or custom performance
        //     counter.
        //
        // Exceptions:
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        public PerformanceCounter()
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new, read-only instance of the System.Diagnostics.PerformanceCounter
        //     class and associates it with the specified system or custom performance counter
        //     on the local computer. This constructor requires that the category have a single
        //     instance.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category (performance object) with which
        //     this performance counter is associated.
        //
        //   counterName:
        //     The name of the performance counter.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     categoryName is an empty string ("").-or- counterName is an empty string ("").-or-
        //     The category specified does not exist. -or-The category specified is marked as
        //     multi-instance and requires the performance counter to be created with an instance
        //     name.-or-categoryName and counterName have been localized into different languages.
        //
        //   T:System.ArgumentNullException:
        //     categoryName or counterName is null.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public PerformanceCounter(string categoryName, string counterName)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new, read-only instance of the System.Diagnostics.PerformanceCounter
        //     class and associates it with the specified system or custom performance counter
        //     and category instance on the local computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category (performance object) with which
        //     this performance counter is associated.
        //
        //   counterName:
        //     The name of the performance counter.
        //
        //   instanceName:
        //     The name of the performance counter category instance, or an empty string (""),
        //     if the category contains a single instance.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     categoryName is an empty string ("").-or- counterName is an empty string ("").-or-
        //     The category specified is not valid. -or-The category specified is marked as
        //     multi-instance and requires the performance counter to be created with an instance
        //     name.-or-instanceName is longer than 127 characters.-or-categoryName and counterName
        //     have been localized into different languages.
        //
        //   T:System.ArgumentNullException:
        //     categoryName or counterName is null.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public PerformanceCounter(string categoryName, string counterName, string instanceName)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new, read-only or read/write instance of the System.Diagnostics.PerformanceCounter
        //     class and associates it with the specified system or custom performance counter
        //     on the local computer. This constructor requires that the category contain a
        //     single instance.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category (performance object) with which
        //     this performance counter is associated.
        //
        //   counterName:
        //     The name of the performance counter.
        //
        //   readOnly:
        //     true to access the counter in read-only mode (although the counter itself could
        //     be read/write); false to access the counter in read/write mode.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The categoryName is an empty string ("").-or- The counterName is an empty string
        //     ("").-or- The category specified does not exist. (if readOnly is true). -or-
        //     The category specified is not a .NET Framework custom category (if readOnly is
        //     false). -or-The category specified is marked as multi-instance and requires the
        //     performance counter to be created with an instance name.-or-categoryName and
        //     counterName have been localized into different languages.
        //
        //   T:System.ArgumentNullException:
        //     categoryName or counterName is null.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public PerformanceCounter(string categoryName, string counterName, bool readOnly)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new, read-only instance of the System.Diagnostics.PerformanceCounter
        //     class and associates it with the specified system or custom performance counter
        //     and category instance, on the specified computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category (performance object) with which
        //     this performance counter is associated.
        //
        //   counterName:
        //     The name of the performance counter.
        //
        //   instanceName:
        //     The name of the performance counter category instance, or an empty string (""),
        //     if the category contains a single instance.
        //
        //   machineName:
        //     The computer on which the performance counter and its associated category exist.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     categoryName is an empty string ("").-or- counterName is an empty string ("").-or-
        //     The read/write permission setting requested is invalid for this counter.-or-
        //     The counter does not exist on the specified computer. -or-The category specified
        //     is marked as multi-instance and requires the performance counter to be created
        //     with an instance name.-or-instanceName is longer than 127 characters.-or-categoryName
        //     and counterName have been localized into different languages.
        //
        //   T:System.ArgumentException:
        //     The machineName parameter is not valid.
        //
        //   T:System.ArgumentNullException:
        //     categoryName or counterName is null.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public PerformanceCounter(string categoryName, string counterName, string instanceName, string machineName)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new, read-only or read/write instance of the System.Diagnostics.PerformanceCounter
        //     class and associates it with the specified system or custom performance counter
        //     and category instance on the local computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category (performance object) with which
        //     this performance counter is associated.
        //
        //   counterName:
        //     The name of the performance counter.
        //
        //   instanceName:
        //     The name of the performance counter category instance, or an empty string (""),
        //     if the category contains a single instance.
        //
        //   readOnly:
        //     true to access a counter in read-only mode; false to access a counter in read/write
        //     mode.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     categoryName is an empty string ("").-or- counterName is an empty string ("").-or-
        //     The read/write permission setting requested is invalid for this counter.-or-
        //     The category specified does not exist (if readOnly is true). -or- The category
        //     specified is not a .NET Framework custom category (if readOnly is false). -or-The
        //     category specified is marked as multi-instance and requires the performance counter
        //     to be created with an instance name.-or-instanceName is longer than 127 characters.-or-categoryName
        //     and counterName have been localized into different languages.
        //
        //   T:System.ArgumentNullException:
        //     categoryName or counterName is null.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public PerformanceCounter(string categoryName, string counterName, string instanceName, bool readOnly)
        {
            //TODO: ALACHISOFT
        }


        //
        // Summary:
        //     Gets or sets a value indicating whether this System.Diagnostics.PerformanceCounter
        //     instance is in read-only mode.
        //
        // Returns:
        //     true, if the System.Diagnostics.PerformanceCounter instance is in read-only mode
        //     (even if the counter itself is a custom .NET Framework counter); false if it
        //     is in read/write mode. The default is the value set by the constructor.
        [Browsable(false)]
        [DefaultValue(true)]
        [MonitoringDescription("PC_ReadOnly")]
        public bool ReadOnly { get; set; }
        //
        // Summary:
        //     Gets or sets an instance name for this performance counter.
        //
        // Returns:
        //     The name of the performance counter category instance, or an empty string (""),
        //     if the counter is a single-instance counter.
        //[DefaultValue("")]
        //[ReadOnly(true)]
        //[SettingsBindable(true)]
        //[SRDescriptionAttribute("PCInstanceName")]
        //[TypeConverter("System.Diagnostics.Design.InstanceNameConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        //public string InstanceName { get; set; }
        //
        // Summary:
        //     Gets or sets the lifetime of a process.
        //
        // Returns:
        //     One of the System.Diagnostics.PerformanceCounterInstanceLifetime values. The
        //     default is System.Diagnostics.PerformanceCounterInstanceLifetime.Global.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     The value set is not a member of the System.Diagnostics.PerformanceCounterInstanceLifetime
        //     enumeration.
        //
        //   T:System.InvalidOperationException:
        //     System.Diagnostics.PerformanceCounter.InstanceLifetime is set after the System.Diagnostics.PerformanceCounter
        //     has been initialized.
        //[DefaultValue(PerformanceCounterInstanceLifetime.Global)]
        //[SRDescriptionAttribute("PCInstanceLifetime")]
        //public PerformanceCounterInstanceLifetime InstanceLifetime { get; set; }
        //
        // Summary:
        //     Gets the counter type of the associated performance counter.
        //
        // Returns:
        //     A System.Diagnostics.PerformanceCounterType that describes both how the counter
        //     interacts with a monitoring application and the nature of the values it contains
        //     (for example, calculated or uncalculated).
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The instance is not correctly associated with a performance counter. -or-The
        //     System.Diagnostics.PerformanceCounter.InstanceLifetime property is set to System.Diagnostics.PerformanceCounterInstanceLifetime.Process
        //     when using global shared memory.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[MonitoringDescription("PC_CounterType")]
        //public PerformanceCounterType CounterType { get; }
        //
        // Summary:
        //     Gets or sets the name of the performance counter that is associated with this
        //     System.Diagnostics.PerformanceCounter instance.
        //
        // Returns:
        //     The name of the counter, which generally describes the quantity being counted.
        //     This name is displayed in the list of counters of the Performance Counter Manager
        //     MMC snap in's Add Counters dialog box.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The System.Diagnostics.PerformanceCounter.CounterName is null.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        [DefaultValue("")]
        [ReadOnly(true)]
        [SettingsBindable(true)]
        //[SRDescriptionAttribute("PCCounterName")]
        [TypeConverter("System.Diagnostics.Design.CounterNameConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string CounterName { get; set; }
        //
        // Summary:
        //     Gets the description for this performance counter.
        //
        // Returns:
        //     A description of the item or quantity that this performance counter measures.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The System.Diagnostics.PerformanceCounter instance is not associated with a performance
        //     counter. -or-The System.Diagnostics.PerformanceCounter.InstanceLifetime property
        //     is set to System.Diagnostics.PerformanceCounterInstanceLifetime.Process when
        //     using global shared memory.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [MonitoringDescription("PC_CounterHelp")]
        [ReadOnly(true)]
        public string CounterHelp { get; }
        //
        // Summary:
        //     Gets or sets the name of the performance counter category for this performance
        //     counter.
        //
        // Returns:
        //     The name of the performance counter category (performance object) with which
        //     this performance counter is associated.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The System.Diagnostics.PerformanceCounter.CategoryName is null.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        [DefaultValue("")]
        [ReadOnly(true)]
        [SettingsBindable(true)]
        //[SRDescriptionAttribute("PCCategoryName")]
        [TypeConverter("System.Diagnostics.Design.CategoryValueConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public string CategoryName { get; set; }
        //
        // Summary:
        //     Gets or sets the raw, or uncalculated, value of this counter.
        //
        // Returns:
        //     The raw value of the counter.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     You are trying to set the counter's raw value, but the counter is read-only.-or-
        //     The instance is not correctly associated with a performance counter. -or-The
        //     System.Diagnostics.PerformanceCounter.InstanceLifetime property is set to System.Diagnostics.PerformanceCounterInstanceLifetime.Process
        //     when using global shared memory.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [MonitoringDescription("PC_RawValue")]
        public long RawValue { get; set; }
        //
        // Summary:
        //     Gets or sets the computer name for this performance counter
        //
        // Returns:
        //     The server on which the performance counter and its associated category reside.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.PerformanceCounter.MachineName format is invalid.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        [Browsable(false)]
        [DefaultValue(".")]
        [SettingsBindable(true)]
        //[SRDescriptionAttribute("PCMachineName")]
        public string MachineName { get; set; }

        //
        // Summary:
        //     Frees the performance counter library shared state allocated by the counters.
        public static void CloseSharedResources()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Begins the initialization of a System.Diagnostics.PerformanceCounter instance
        //     used on a form or by another component. The initialization occurs at runtime.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void BeginInit()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Closes the performance counter and frees all the resources allocated by this
        //     performance counter instance.
        public void Close()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Decrements the associated performance counter by one through an efficient atomic
        //     operation.
        //
        // Returns:
        //     The decremented counter value.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The counter is read-only, so the application cannot decrement it.-or- The instance
        //     is not correctly associated with a performance counter. -or-The System.Diagnostics.PerformanceCounter.InstanceLifetime
        //     property is set to System.Diagnostics.PerformanceCounterInstanceLifetime.Process
        //     when using global shared memory.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        public long Decrement()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Ends the initialization of a System.Diagnostics.PerformanceCounter instance that
        //     is used on a form or by another component. The initialization occurs at runtime.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public void EndInit()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Increments the associated performance counter by one through an efficient atomic
        //     operation.
        //
        // Returns:
        //     The incremented counter value.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The counter is read-only, so the application cannot increment it.-or- The instance
        //     is not correctly associated with a performance counter. -or-The System.Diagnostics.PerformanceCounter.InstanceLifetime
        //     property is set to System.Diagnostics.PerformanceCounterInstanceLifetime.Process
        //     when using global shared memory.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        public long Increment()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Increments or decrements the value of the associated performance counter by a
        //     specified amount through an efficient atomic operation.
        //
        // Parameters:
        //   value:
        //     The value to increment by. (A negative value decrements the counter.)
        //
        // Returns:
        //     The new counter value.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The counter is read-only, so the application cannot increment it.-or- The instance
        //     is not correctly associated with a performance counter. -or-The System.Diagnostics.PerformanceCounter.InstanceLifetime
        //     property is set to System.Diagnostics.PerformanceCounterInstanceLifetime.Process
        //     when using global shared memory.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public long IncrementBy(long value)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Obtains a counter sample, and returns the raw, or uncalculated, value for it.
        //
        // Returns:
        //     A System.Diagnostics.CounterSample that represents the next raw value that the
        //     system obtains for this counter.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The instance is not correctly associated with a performance counter. -or-The
        //     System.Diagnostics.PerformanceCounter.InstanceLifetime property is set to System.Diagnostics.PerformanceCounterInstanceLifetime.Process
        //     when using global shared memory.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        //public CounterSample NextSample();
        //
        // Summary:
        //     Obtains a counter sample and returns the calculated value for it.
        //
        // Returns:
        //     The next calculated value that the system obtains for this counter.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The instance is not correctly associated with a performance counter.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public float NextValue()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Deletes the category instance specified by the System.Diagnostics.PerformanceCounter
        //     object System.Diagnostics.PerformanceCounter.InstanceName property.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     This counter is read-only, so any instance that is associated with the category
        //     cannot be removed.-or- The instance is not correctly associated with a performance
        //     counter. -or-The System.Diagnostics.PerformanceCounter.InstanceLifetime property
        //     is set to System.Diagnostics.PerformanceCounterInstanceLifetime.Process when
        //     using global shared memory.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     An error occurred when accessing a system API.
        //
        //   T:System.PlatformNotSupportedException:
        //     The platform is Windows 98 or Windows Millennium Edition (Me), which does not
        //     support performance counters.
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public void RemoveInstance()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        protected override void Dispose(bool disposing)
        {
            //TODO: ALACHISOFT
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace System.Diagnostics
{
    //
    // Summary:
    //     Represents a performance object, which defines a category of performance counters.
    public sealed class PerformanceCounterCategory
    {
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.PerformanceCounterCategory
        //     class, leaves the System.Diagnostics.PerformanceCounterCategory.CategoryName
        //     property empty, and sets the System.Diagnostics.PerformanceCounterCategory.MachineName
        //     property to the local computer.
        public PerformanceCounterCategory()
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.PerformanceCounterCategory
        //     class, sets the System.Diagnostics.PerformanceCounterCategory.CategoryName property
        //     to the specified value, and sets the System.Diagnostics.PerformanceCounterCategory.MachineName
        //     property to the local computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category, or performance object, with which
        //     to associate this System.Diagnostics.PerformanceCounterCategory instance.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The categoryName is an empty string ("").
        //
        //   T:System.ArgumentNullException:
        //     The categoryName is null.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public PerformanceCounterCategory(string categoryName)
        {
            //TODO: ALACHISOFT
        }
        //
        // Summary:
        //     Initializes a new instance of the System.Diagnostics.PerformanceCounterCategory
        //     class and sets the System.Diagnostics.PerformanceCounterCategory.CategoryName
        //     and System.Diagnostics.PerformanceCounterCategory.MachineName properties to the
        //     specified values.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category, or performance object, with which
        //     to associate this System.Diagnostics.PerformanceCounterCategory instance.
        //
        //   machineName:
        //     The computer on which the performance counter category and its associated counters
        //     exist.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The categoryName is an empty string ("").-or- The machineName syntax is invalid.
        //
        //   T:System.ArgumentNullException:
        //     The categoryName is null.
        public PerformanceCounterCategory(string categoryName, string machineName)
        {
            //TODO: ALACHISOFT
        }

        //
        // Summary:
        //     Gets the category's help text.
        //
        // Returns:
        //     A description of the performance object that this category measures.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The System.Diagnostics.PerformanceCounterCategory.CategoryName property is null.
        //     The category name must be set before getting the category help.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        public string CategoryHelp { get; }
        //
        // Summary:
        //     Gets or sets the name of the performance object that defines this category.
        //
        // Returns:
        //     The name of the performance counter category, or performance object, with which
        //     to associate this System.Diagnostics.PerformanceCounterCategory instance.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.PerformanceCounterCategory.CategoryName is an empty string
        //     ("").
        //
        //   T:System.ArgumentNullException:
        //     The System.Diagnostics.PerformanceCounterCategory.CategoryName is null.
        public string CategoryName { get; set; }
        //
        // Summary:
        //     Gets the performance counter category type.
        //
        // Returns:
        //     One of the System.Diagnostics.PerformanceCounterCategoryType values.
        //public PerformanceCounterCategoryType CategoryType { get; }
        //
        // Summary:
        //     Gets or sets the name of the computer on which this category exists.
        //
        // Returns:
        //     The name of the computer on which the performance counter category and its associated
        //     counters exist.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The System.Diagnostics.PerformanceCounterCategory.MachineName syntax is invalid.
        public string MachineName { get; set; }
        //
        // Summary:
        //     Determines whether the specified counter is registered to the specified category
        //     on the local computer.
        //
        // Parameters:
        //   counterName:
        //     The name of the performance counter to look for.
        //
        //   categoryName:
        //     The name of the performance counter category, or performance object, with which
        //     the specified performance counter is associated.
        //
        // Returns:
        //     true, if the counter is registered to the specified category on the local computer;
        //     otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The categoryName is null.-or- The counterName is null.
        //
        //   T:System.ArgumentException:
        //     The categoryName is an empty string ("").
        //
        //   T:System.InvalidOperationException:
        //     The category name does not exist.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static bool CounterExists(string counterName, string categoryName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Determines whether the specified counter is registered to the specified category
        //     on a remote computer.
        //
        // Parameters:
        //   counterName:
        //     The name of the performance counter to look for.
        //
        //   categoryName:
        //     The name of the performance counter category, or performance object, with which
        //     the specified performance counter is associated.
        //
        //   machineName:
        //     The name of the computer on which the performance counter category and its associated
        //     counters exist.
        //
        // Returns:
        //     true, if the counter is registered to the specified category on the specified
        //     computer; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The categoryName is null.-or- The counterName is null.
        //
        //   T:System.ArgumentException:
        //     The categoryName is an empty string ("").-or- The machineName is invalid.
        //
        //   T:System.InvalidOperationException:
        //     The category name does not exist.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public static bool CounterExists(string counterName, string categoryName, string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Registers the custom performance counter category containing a single counter
        //     of type System.Diagnostics.PerformanceCounterType.NumberOfItems32 on the local
        //     computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the custom performance counter category to create and register with
        //     the system.
        //
        //   categoryHelp:
        //     A description of the custom category.
        //
        //   categoryType:
        //     One of the System.Diagnostics.PerformanceCounterCategoryType values specifying
        //     whether the category is System.Diagnostics.PerformanceCounterCategoryType.MultiInstance,
        //     System.Diagnostics.PerformanceCounterCategoryType.SingleInstance, or System.Diagnostics.PerformanceCounterCategoryType.Unknown.
        //
        //   counterName:
        //     The name of a new counter to create as part of the new category.
        //
        //   counterHelp:
        //     A description of the counter that is associated with the new custom category.
        //
        // Returns:
        //     A System.Diagnostics.PerformanceCounterCategory that is associated with the new
        //     system category, or performance object.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     counterName is null or is an empty string ("").-or- The counter that is specified
        //     by counterName already exists.-or- counterName has invalid syntax. It might contain
        //     backslash characters ("\") or have length greater than 80 characters.
        //
        //   T:System.InvalidOperationException:
        //     The category already exists on the local computer.
        //
        //   T:System.ArgumentNullException:
        //     categoryName is null. -or-counterHelp is null.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        //public static PerformanceCounterCategory Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, string counterName, string counterHelp);
        //
        // Summary:
        //     Registers the custom performance counter category containing the specified counters
        //     on the local computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the custom performance counter category to create and register with
        //     the system.
        //
        //   categoryHelp:
        //     A description of the custom category.
        //
        //   counterData:
        //     A System.Diagnostics.CounterCreationDataCollection that specifies the counters
        //     to create as part of the new category.
        //
        // Returns:
        //     A System.Diagnostics.PerformanceCounterCategory that is associated with the new
        //     custom category, or performance object.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     A counter name that is specified within the counterData collection is null or
        //     an empty string ("").-or- A counter that is specified within the counterData
        //     collection already exists.-or- The counterName parameter has invalid syntax.
        //     It might contain backslash characters ("\") or have length greater than 80 characters.
        //
        //   T:System.ArgumentNullException:
        //     The categoryName parameter is null.
        //
        //   T:System.InvalidOperationException:
        //     The category already exists on the local computer.-or- The layout of the counterData
        //     collection is incorrect for base counters. A counter of type AverageCount64,
        //     AverageTimer32, CounterMultiTimer, CounterMultiTimerInverse, CounterMultiTimer100Ns,
        //     CounterMultiTimer100NsInverse, RawFraction, SampleFraction or SampleCounter has
        //     to be immediately followed by one of the base counter types (AverageBase, MultiBase,
        //     RawBase, or SampleBase).
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        //[Obsolete("This method has been deprecated.  Please use System.Diagnostics.PerformanceCounterCategory.Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, CounterCreationDataCollection counterData) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        //[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        //public static PerformanceCounterCategory Create(string categoryName, string categoryHelp, CounterCreationDataCollection counterData);
        //
        // Summary:
        //     Registers the custom performance counter category containing the specified counters
        //     on the local computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the custom performance counter category to create and register with
        //     the system.
        //
        //   categoryHelp:
        //     A description of the custom category.
        //
        //   categoryType:
        //     One of the System.Diagnostics.PerformanceCounterCategoryType values.
        //
        //   counterData:
        //     A System.Diagnostics.CounterCreationDataCollection that specifies the counters
        //     to create as part of the new category.
        //
        // Returns:
        //     A System.Diagnostics.PerformanceCounterCategory that is associated with the new
        //     custom category, or performance object.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     A counter name that is specified within the counterData collection is null or
        //     an empty string ("").-or- A counter that is specified within the counterData
        //     collection already exists.-or- counterName has invalid syntax. It might contain
        //     backslash characters ("\") or have length greater than 80 characters.
        //
        //   T:System.ArgumentNullException:
        //     categoryName is null. -or-counterData is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     categoryType value is outside of the range of the following values: MultiInstance,
        //     SingleInstance, or Unknown.
        //
        //   T:System.InvalidOperationException:
        //     The category already exists on the local computer.-or- The layout of the counterData
        //     collection is incorrect for base counters. A counter of type AverageCount64,
        //     AverageTimer32, CounterMultiTimer, CounterMultiTimerInverse, CounterMultiTimer100Ns,
        //     CounterMultiTimer100NsInverse, RawFraction, SampleFraction, or SampleCounter
        //     must be immediately followed by one of the base counter types (AverageBase, MultiBase,
        //     RawBase, or SampleBase).
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        //public static PerformanceCounterCategory Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, CounterCreationDataCollection counterData);
        //
        // Summary:
        //     Registers a custom performance counter category containing a single counter of
        //     type NumberOfItems32 on the local computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the custom performance counter category to create and register with
        //     the system.
        //
        //   categoryHelp:
        //     A description of the custom category.
        //
        //   counterName:
        //     The name of a new counter, of type NumberOfItems32, to create as part of the
        //     new category.
        //
        //   counterHelp:
        //     A description of the counter that is associated with the new custom category.
        //
        // Returns:
        //     A System.Diagnostics.PerformanceCounterCategory that is associated with the new
        //     system category, or performance object.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     counterName is null or is an empty string ("").-or- The counter that is specified
        //     by counterName already exists.-or- counterName has invalid syntax. It might contain
        //     backslash characters ("\") or have length greater than 80 characters.
        //
        //   T:System.InvalidOperationException:
        //     The category already exists on the local computer.
        //
        //   T:System.ArgumentNullException:
        //     categoryName is null. -or-counterHelp is null.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [Obsolete("This method has been deprecated.  Please use System.Diagnostics.PerformanceCounterCategory.Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, string counterName, string counterHelp) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public static PerformanceCounterCategory Create(string categoryName, string categoryHelp, string counterName, string counterHelp)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Removes the category and its associated counters from the local computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the custom performance counter category to delete.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The categoryName parameter is null.
        //
        //   T:System.ArgumentException:
        //     The categoryName parameter has invalid syntax. It might contain backslash characters
        //     ("\") or have length greater than 80 characters.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.InvalidOperationException:
        //     The category cannot be deleted because it is not a custom category.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public static void Delete(string categoryName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        //
        // Summary:
        //     Determines whether the category is registered on the local computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category to look for.
        //
        // Returns:
        //     true if the category is registered; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The categoryName parameter is null.
        //
        //   T:System.ArgumentException:
        //     The categoryName parameter is an empty string ("").
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static bool Exists(string categoryName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        //
        // Summary:
        //     Determines whether the category is registered on the specified computer.
        //
        // Parameters:
        //   categoryName:
        //     The name of the performance counter category to look for.
        //
        //   machineName:
        //     The name of the computer to examine for the category.
        //
        // Returns:
        //     true if the category is registered; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The categoryName parameter is null.
        //
        //   T:System.ArgumentException:
        //     The categoryName parameter is an empty string ("").-or- The machineName parameter
        //     is invalid.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.IO.IOException:
        //     The network path cannot be found.
        //
        //   T:System.UnauthorizedAccessException:
        //     The caller does not have the required permission.-or-Code that is executing without
        //     administrative privileges attempted to read a performance counter.
        public static bool Exists(string categoryName, string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Retrieves a list of the performance counter categories that are registered on
        //     the local computer.
        //
        // Returns:
        //     An array of System.Diagnostics.PerformanceCounterCategory objects indicating
        //     the categories that are registered on the local computer.
        //
        // Exceptions:
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static PerformanceCounterCategory[] GetCategories()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Retrieves a list of the performance counter categories that are registered on
        //     the specified computer.
        //
        // Parameters:
        //   machineName:
        //     The computer to look on.
        //
        // Returns:
        //     An array of System.Diagnostics.PerformanceCounterCategory objects indicating
        //     the categories that are registered on the specified computer.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The machineName parameter is invalid.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public static PerformanceCounterCategory[] GetCategories(string machineName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        //
        // Summary:
        //     Determines whether the specified counter is registered to this category, which
        //     is indicated by the System.Diagnostics.PerformanceCounterCategory.CategoryName
        //     and System.Diagnostics.PerformanceCounterCategory.MachineName properties.
        //
        // Parameters:
        //   counterName:
        //     The name of the performance counter to look for.
        //
        // Returns:
        //     true if the counter is registered to the category that is specified by the System.Diagnostics.PerformanceCounterCategory.CategoryName
        //     and System.Diagnostics.PerformanceCounterCategory.MachineName properties; otherwise,
        //     false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The counterName is null.
        //
        //   T:System.InvalidOperationException:
        //     The System.Diagnostics.PerformanceCounterCategory.CategoryName property has not
        //     been set.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public bool CounterExists(string counterName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Retrieves a list of the counters in a performance counter category that contains
        //     exactly one instance.
        //
        // Returns:
        //     An array of System.Diagnostics.PerformanceCounter objects indicating the counters
        //     that are associated with this single-instance performance counter category.
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     The category is not a single instance.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.InvalidOperationException:
        //     The category does not have an associated instance.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public PerformanceCounter[] GetCounters()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Retrieves a list of the counters in a performance counter category that contains
        //     one or more instances.
        //
        // Parameters:
        //   instanceName:
        //     The performance object instance for which to retrieve the list of associated
        //     counters.
        //
        // Returns:
        //     An array of System.Diagnostics.PerformanceCounter objects indicating the counters
        //     that are associated with the specified object instance of this performance counter
        //     category.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The instanceName parameter is null.
        //
        //   T:System.InvalidOperationException:
        //     The System.Diagnostics.PerformanceCounterCategory.CategoryName property for this
        //     System.Diagnostics.PerformanceCounterCategory instance has not been set.-or-
        //     The category does not contain the instance that is specified by the instanceName
        //     parameter.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public PerformanceCounter[] GetCounters(string instanceName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }

        //
        // Summary:
        //     Retrieves the list of performance object instances that are associated with this
        //     category.
        //
        // Returns:
        //     An array of strings representing the performance object instance names that are
        //     associated with this category or, if the category contains only one performance
        //     object instance, a single-entry array that contains an empty string ("").
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The System.Diagnostics.PerformanceCounterCategory.CategoryName property is null.
        //     The property might not have been set. -or-The category does not have an associated
        //     instance.
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        public string[] GetInstanceNames()
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
        //
        // Summary:
        //     Determines whether a specified category on the local computer contains the specified
        //     performance object instance.
        //
        // Parameters:
        //   instanceName:
        //     The performance object instance to search for.
        //
        //   categoryName:
        //     The performance counter category to search.
        //
        // Returns:
        //     true if the category contains the specified performance object instance; otherwise,
        //     false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     The instanceName parameter is null.-or- The categoryName parameter is null.
        //
        //   T:System.ArgumentException:
        //     The categoryName parameter is an empty string ("").
        //
        //   T:System.ComponentModel.Win32Exception:
        //     A call to an underlying system API failed.
        //
        //   T:System.UnauthorizedAccessException:
        //     Code that is executing without administrative privileges attempted to read a
        //     performance counter.
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static bool InstanceExists(string instanceName, string categoryName)
        {
            //TODO: ALACHISOFT
            throw new NotImplementedException();
        }
    }
}

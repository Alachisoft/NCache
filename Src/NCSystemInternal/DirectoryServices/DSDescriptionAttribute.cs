using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace System.DirectoryServices
{
    //
    // Summary:
    //     Supports the .NET Framework infrastructure and is not intended to be used directly
    //     from code.
    [AttributeUsage(AttributeTargets.All)]
    public class DSDescriptionAttribute : DescriptionAttribute
    {
        //
        // Summary:
        //     Supports the .NET Framework infrastructure and is not intended to be used directly
        //     from code.
        //
        // Parameters:
        //   description:
        //     The description text.
        public DSDescriptionAttribute(string description)
        {
            //TODO: ALACHISOFT
        }

        //
        // Summary:
        //     Supports the .NET Framework infrastructure and is not intended to be used directly
        //     from code.
        //
        // Returns:
        //     A string that contains a description of a property or other element. The System.DirectoryServices.DSDescriptionAttribute.Description
        //     property contains a description that is meaningful to the user.
        public override string Description { get; }
    }
}

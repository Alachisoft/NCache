#region Using directives

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("#SNMP Agent")]
[assembly: AssemblyDescription("#SNMP Agent")]
[assembly: AssemblyConfiguration("MIT/X11")]
[assembly: AssemblyCompany("LeXtudio")]
[assembly: AssemblyProduct("#SNMP Agent")]
[assembly: AssemblyCopyright("(C) 2008-2010 Lex Li")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// This sets the default COM visibility of types in the assembly to invisible.
// If you need to expose a type to COM, use [ComVisible(true)] on that type.
[assembly: ComVisible(false)]

[assembly: CLSCompliant(true)]
[assembly: NeutralResourcesLanguage("en-US")]

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

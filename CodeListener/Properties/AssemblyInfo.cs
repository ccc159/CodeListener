using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeListener;
using Rhino.PlugIns;

// Plug-in Description Attributes - all of these are optional.
// These will show in Rhino's option dialog, in the tab Plug-ins.
[assembly: PlugInDescription(DescriptionType.Address, "Seestrasse 78, 8703 Erlenbach/Zürich")]
[assembly: PlugInDescription(DescriptionType.Country, "Switzerland")]
[assembly: PlugInDescription(DescriptionType.Email, "info@designtoproduction.com")]
[assembly: PlugInDescription(DescriptionType.Phone, "+41 (0) 44 914 74 90")]
[assembly: PlugInDescription(DescriptionType.Fax, "+41 (0) 44 914 74 99")]
[assembly: PlugInDescription(DescriptionType.Organization, "Design-to-Production GmbH")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "https://github.com/ccc159/CodeListener")]
[assembly: PlugInDescription(DescriptionType.WebSite, "http://designtoproduction.com/")]

// Icons should be Windows .ico files and contain 32-bit images in the following sizes: 16, 24, 32, 48, and 256.
// This is a Rhino 6-only description.

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("CodeListener")]

// This will be used also for the plug-in description.
[assembly: AssemblyDescription("CodeListener utility plug-in")]

[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Design-To-Production")]
[assembly: AssemblyProduct("CodeListener")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("8c4235b6-64bc-4508-9166-bef8aa151085")] // This will also be the Guid of the Rhino plug-in

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion("0.1.7")]
[assembly: AssemblyFileVersion("0.1.7")]

// Make compatible with Rhino Installer Engine
[assembly: AssemblyInformationalVersion("2")]

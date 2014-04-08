using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "InstanceVoidCutTest Revit Add-In" )]
[assembly: AssemblyDescription( "Test InstanceVoidCutUtils" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk Inc." )]
[assembly: AssemblyProduct( "InstanceVoidCutTest Revit Add-In" )]
[assembly: AssemblyCopyright( "Copyright 2014 © Jeremy Tammik Autodesk Inc." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "321044f7-b0b2-4b1c-af18-e71a19252be0" )]

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
// 2014-04-08 2014.0.0.0 initial commit throwing a message saying "The element is not a family instance with an unattached void that can cut. Parameter name: cuttingInstance"
// 2014-04-08 2014.0.0.1 added call to regenerate and all works well
// 2014-04-08 2014.0.0.2 added transaction group, refactored RetrieveOrLoadCuttingSymbol, cleaned up code
// 2014-04-08 2014.0.0.3 added call to assimilate the transactions into the group
[assembly: AssemblyVersion( "2014.0.0.3" )]
[assembly: AssemblyFileVersion( "2014.0.0.3" )]

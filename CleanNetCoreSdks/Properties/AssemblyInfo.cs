using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Austin.CleanNetCoreSdks")]
[assembly: AssemblyDescription("Removes .NET Core SDKs that are not needed.")]
#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyCompany("Austin Wise")]
[assembly: AssemblyProduct("Austin.CleanNetCoreSdks")]
[assembly: AssemblyCopyright("Copyright © Austin Wise 2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7dc738c6-a5dd-45ac-8cc5-e3bd02f1e91e")]

// Version information is managed by version.json and Nerdbank.GitVersioning

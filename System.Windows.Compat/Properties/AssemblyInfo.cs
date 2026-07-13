using System.Reflection;
using System.Runtime.CompilerServices;

// 4.1.0.0 > the System.Windows type-forwarder facade in Microsoft.NETCore.App (4.0.0.0),
// so MSBuild conflict resolution and the framework-dependent host (dotnet test) pick this
// assembly instead of the facade, which contains none of the compat types.
[assembly: AssemblyVersion("4.1.0.0")]
[assembly: AssemblyFileVersion("4.1.0.0")]
[assembly: AssemblyTitle("System.Windows")]
[assembly: AssemblyDescription("Headless WPF compatibility layer")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("System.Windows")]
[assembly: AssemblyCopyright("")]

// Note: The public key token b03f5f7f11d50a3a belongs to Microsoft's WPF assemblies.
// We're creating a stub assembly that mimics the same identity for compatibility.
// This is for internal use only in a controlled environment.

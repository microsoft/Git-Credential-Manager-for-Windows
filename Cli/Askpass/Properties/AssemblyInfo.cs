using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle(Microsoft.Alm.Cli.Program.AssemblyTitle)]
[assembly: AssemblyDescription(Microsoft.Alm.Cli.Program.AssemblyDesciption + ". " + Microsoft.Alm.Cli.Program.SourceUrl)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyProduct(Microsoft.Alm.Cli.Program.AssemblyTitle + " command line interface.")]
[assembly: AssemblyCopyright("Copyright © Microsoft Corporation 2018. All rights reserved.")]
[assembly: AssemblyTrademark("Microsoft Corporation")]
[assembly: AssemblyCulture("")]
[assembly: Guid("19770407-63d4-1230-a9df-f1c4b473308a")]
[assembly: AssemblyVersion("1.19.0.0")]
[assembly: AssemblyFileVersion("1.19.0.0")]
[assembly: NeutralResourcesLanguage("en-US")]

// Only expose internals when the binary isn't signed.
#if !SIGNED
[assembly: InternalsVisibleTo("Microsoft.Alm.Cli.Proxy")]
[assembly: InternalsVisibleTo("Microsoft.Alm.Cli.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif

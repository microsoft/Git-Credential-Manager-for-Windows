using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Microsoft Alm Authentication")]
[assembly: AssemblyDescription("Microsoft Application Lifecycle Management Authentication Library for Windows")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyProduct("https://github.com/Microsoft/Git-Credential-Manager-for-Windows")]
[assembly: AssemblyCopyright("Copyright © Microsoft Corporation 2018. All rights reserved.")]
[assembly: AssemblyTrademark("Microsoft Corporation")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("19770407-b493-0415-bb4f-04fbefb1ba13")]
[assembly: AssemblyVersion("4.6.0.0")]
[assembly: AssemblyFileVersion("4.6.0.0")]
[assembly: NeutralResourcesLanguage("en-US")]

// Only expose internals when the binary isn't signed.
#if !SIGNED
[assembly: InternalsVisibleTo("Microsoft.Alm.Authentication.Proxy")]
[assembly: InternalsVisibleTo("Microsoft.Alm.Authentication.Test")]
[assembly: InternalsVisibleTo("Microsoft.Alm.Cli.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif

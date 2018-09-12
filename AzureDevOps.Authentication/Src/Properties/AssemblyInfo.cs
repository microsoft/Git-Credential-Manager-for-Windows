using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Azure DevOps Authentication")]
[assembly: AssemblyDescription("Microsoft Azure DevOps Authentication Library for Windows")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyProduct("https://github.com/Microsoft/Git-Credential-Manager-for-Windows")]
[assembly: AssemblyCopyright("Copyright © Microsoft Corporation 2018. All rights reserved.")]
[assembly: AssemblyTrademark("Microsoft Corporation")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("19770407-b493-1230-bb4f-04fbefb1ba13")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: NeutralResourcesLanguage("en-US")]

// Only expose internals when the binary isn't signed.
#if !SIGNED
[assembly: InternalsVisibleTo("AzureDevOps.Authentication.Proxy")]
[assembly: InternalsVisibleTo("AzureDevOps.Authentication.Test")]
#endif

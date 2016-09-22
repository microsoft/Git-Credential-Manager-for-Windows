# Development and Debugging

 Developing for GCM and/or Askpass requires Visual Studio 2015 or newer, any version (including the free [Community Edition](https://www.visualstudio.com/products/visual-studio-community-vs)).

## Getting Started

 The easiest way to get started is to:
 
  1. Install Visual Studio
  2. [Clone the repository](https://github.com/Microsoft/Git-Credential-Manager-for-Windows.git)
  3. Open the solution file (Microsoft.Alm.sln) using Visual Studio
  4. Right-click the solution node in Solution Explorer and choose 'Restore NuGet Packages'. This will download and setup all of the dependencies.
  5. Right-click the 'Cli-CredentialHelper' project in Solution Explorer and select 'Properties'.
  6. In the "Properties" window, select the 'Debug' tab from the left side.
  7. In the "Properties: Debug" window, add the word "get" to the 'Command line arguments:' text box.
  8. Close the "Properties" window.
  5. Hit \<F5\>, or 'Debug' \>\> 'Start Debugging' from the top menu of Visual Studio.

### Debugging Code

 Once the GCM starts you'll be presented with a very *unsatifying* console window. This is because the GCM expects to be launched by Git, not directly. However, it is easy to play the role of Git. The GCM expects Git to supply at least two pieces of information: the protocol being use and the host name for which the current operation is happening.
 
 An example of faking Git request for GitHub credentials:
 
 ```
     protocol=https
     host=github.com
     
 ```

 Notice the **blank last line**. That empty line is how Git and the GCM notify the otherside that they're done sending data. Until an empty line is sent to the GCM, it'll keep attempting to read from standard input.

 Once the blank line is fed to the GCM it'll "do its thing". Ideally, you can watch it work, so that you can learn how it works and then improve it. To do so, place a break point in the `Main` method of the 'Program.cs' file. Doing so will allow you to "break in" when the execition pointer reaches your break point. You'll notice that the GCM doesn't read from standard input immediately; instead it does some setup work to determine what it expected of it and then only reads from standard input if it is expected to.

 In the case of `git credential-manager get`, the `Main` method will call the `Get` method, which in turn will allocate a new `OperationArguments` object and initialize it with the process' standard input pipe. This is when stanard input will be consumed by the GCM.

### Notable Code
 
  * `Program.LoadOperationArguments` method is where the GCM scans, parses, and consumes environmental and configuraiton setting values.
  * `Program.QueryCredentials` method is where the "action" happens.
  * `OperationArguments` class is where the GCM consumes standard input and keeps internal state.

## Installer (setup.exe)

 Changes to the installer (setup.exe) requires [Inno Setup Compiler 5.5.6](http://www.jrsoftware.org/isinfo.php) or later to compile. Additionally, the [IDP plugin for Inno Setup](https://mitrichsoftware.wordpress.com/inno-setup-tools/inno-download-plugin/) is also required.

 The setup compiler pulls content from the "Deploy/" folder, therefore a completed Debug or Release build needs to have been completed prior to running the setup compiler. Content in the "Deploy/" folder will be used in the setup compilation.

## Microsoft.Alm.Authentication NuGet Package

 The [Microsoft.Alm.Authentication](https://www.nuget.org/packages/Microsoft.Alm.Authentication/) NuGet package is automatically created when the Microsoft.Alm.Authentication project is built. The generated .nupkg files can be found in the "Debug/" or "Release/" (depending on your build target) under "Microsoft.Alm.Authentication/bin/". Both the binary and symbold packages are automatically created.

 Updates to the NuGet package stream are reserved for officially built binaries.

## Signing

 Only Microsoft can sign binaries with the Microsoft certificate. Therefore, while anyone can build and use their own binaies from the GCM souce code, only officially releases binaries will be signed by Microsoft.

## Documents

 The documentation is formatted using [markdown](https://daringfireball.net/projects/markdown/syntax) and generated using [Pandoc](http://http://pandoc.org/).

## Logging

 To enable logging, use the following:
 
     git config --global credential.writelog true

 Log files will be written to the repo's local `.git/` folder.

 Debug build of the GCM will perform extended logging to the console, which is convenient for debugging purposes bug too noisy for day-to-day usage.

## Contribute

 There are many ways to contribute.

  * [Submit bugs](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/issues) and help us verify fixes as they are checked in.
  * Review [code changes](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/pulls).
  * Contribute bug fixes and features.

### Code Contributions

 For code contributions, you will need to complete a Contributor License Agreement (CLA). Briefly, this agreement testifies that you grant us permission to use the submitted change according to the terms of the project's license, and that the work being submitted is under the appropriate copyright.

 Please submit a Contributor License Agreement (CLA) before submitting a pull request. You may visit <https://cla.microsoft.com> to sign digitally. Alternatively, download the agreement [Microsoft Contribution License Agreement.pdf](https://cla.microsoft.com/cladoc/microsoft-contribution-license-agreement.pdf), sign, scan, and email it back to <cla@microsoft.com>. Be sure to include your GitHub user name along with the agreement. Once we have received the signed CLA, we'll review the request.

### Code of Conduct

 This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## License

 This project uses the [MIT License](https://github.com/Microsoft/Git-Credential-Manager-for-Windows/blob/master/LICENSE.txt).
# Release - Temp instructions
Update AssemblyInfo.cs versions of projects that have changed
1. Do a 'release' build
1. use 7zip to zip up the contents of the Deploy folder in PortableGcmSt-#.#.#.#.7z 
1. upload to downloads.atlassian.com\software\sourcetree\windows

1. Do a 'debug' build
1. use 7zip to zip up the contents of the Deploy folder in PortableGcmSt-#.#.#.#-debug.7z 
1. upload to downloads.atlassian.com\software\sourcetree\windows

update refs in SourceTree project to use new version.

# TODO
BUG - refresh token is getting deleted somewhere.

Upgrade on to 1.8.0
Review code to get PR with MS
ST team PR

# Questions
* Why is there an interactive command line for clear/delete, but no others?
* Why doesn't Program.Delete() use Program.DeleteCredentials() when Clear() and Erase() do?
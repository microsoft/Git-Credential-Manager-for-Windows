# Release - Temp instructions
Update AssemblyInfo.cs versions of projects that have changed
Do a 'release' build
use 7zip to zip up the contents of the Deploy folder in PortableGcm-#.#.#.#.7z 
upload to downloads.atlassian.com\software\sourcetree\windows
update refs in SourceTree project to use new version.

# TODO
BUG - refresh token is getting deleted somewhere.

Upgrade on to 1.8.0
Review code to get PR with MS
ST team PR

# Questions
* Why is there an interactive command line for clear/delete, but no others?
* Why doesn't Program.Delete() use Program.DeleteCredentials() when Clear() and Erase() do?
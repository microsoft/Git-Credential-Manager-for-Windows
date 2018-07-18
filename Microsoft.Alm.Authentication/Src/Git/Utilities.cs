/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Alm.Authentication.Git
{
    public interface IUtilities : IRuntimeService
    {
        /// <summary>
        /// Enumerates all processes visible by the current user, and builds process parentage chain from the current process back to System.
        /// <para/>
        /// Then walks up the chain from the current process, inspecting each parent process, looking for "git-remote-https" or "git-remote-http".
        /// <para/>
        /// When it find the target process, it then reads the process' memory and extracts the command line used to start the process and the image path of its executable.
        /// <para/>
        /// Returns `<see langword="true"/>` if able to extract information from the desired process; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="commandLine">The command line used to create the target process if successful; otherwise `<see langword="null"/>`.</param>
        /// <param name="imagePath">The path to the image used to create the process if successful; otherwise `<see langword="null"/>`.</param>
        bool TryReadGitRemoteHttpDetails(out string commandLine, out string imagePath);
    }

    internal class Utilities : Base, IUtilities
    {
        public Utilities(RuntimeContext context)
            : base(context)
        { }

        public Type ServiceType
            => typeof(IUtilities);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "error")]
        public bool TryReadGitRemoteHttpDetails(out string commandLine, out string imagePath)
        {
            try
            {
                // Find and enumerate the parent processes of the current process.
                var processList = EnumerateParentProcesses();

                // Search the process list, looking for the first instance of "git-remote-https.exe",
                // we'll accept "git-remote-http.exe" as well (even if users ought to not be using insecure protocols).
                foreach (var entry in processList)
                {
                    if (entry.ExeFileName.Equals("git-remote-https.exe", StringComparison.OrdinalIgnoreCase)
                        || entry.ExeFileName.Equals("git-remote-http.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        // Once we've found the process we want, open a handle to it.
                        using (var processHandle = Win32.Kernel32.OpenProcess(desiredAccess: Win32.DesiredAccess.AllAccess,
                                                                              inheritHandle: false,
                                                                                  processId: entry.ProcessId))
                        {
                            return ReadProcessMemory(processHandle: processHandle,
                                                       commandLine: out commandLine,
                                                         imagePath: out imagePath);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                var error = Win32.Kernel32.GetLastError();

                Trace.WriteLine($"failed to read process details [{error}].");
                Trace.WriteException(exception);
            }

            commandLine = null;
            imagePath = null;

            return false;
        }

        internal static IEnumerable<Win32.ProcessEntry32> EnumerateParentProcesses()
        {
            var processTable = new Dictionary<uint, uint>();
            var processEntries = new Dictionary<uint, Win32.ProcessEntry32>();
            var processEntry = new Win32.ProcessEntry32
            {
                Size = Marshal.SizeOf(typeof(Win32.ProcessEntry32)),
            };

            // Create a snapshot of all the processes currently running in the local system reachable from this process/principle.
            using (var snapshotHandle = Win32.Kernel32.CreateToolhelp32Snapshot(flags: Win32.ToolhelpSnapshotFlags.Process,
                                                                            processId: Win32.Kernel32.GetCurrentProcessId()))
            {
                // Read the first process from the snapshot and record it in the lookup table.
                if (Win32.Kernel32.Process32First(snapshotHandle: snapshotHandle,
                                                    processEntry: ref processEntry))
                {
                    processEntries.Add(processEntry.ProcessId, processEntry);
                    processTable.Add(processEntry.ProcessId, processEntry.ParentProcessId);

                    // Iterate through the remaining processes in the snapshot and record them in the lookup table.
                    while (Win32.Kernel32.Process32Next(snapshotHandle: snapshotHandle,
                                                          processEntry: ref processEntry))
                    {
                        processEntries.Add(processEntry.ProcessId, processEntry);
                        processTable.Add(processEntry.ProcessId, processEntry.ParentProcessId);
                    }
                }
            }

            var cycleGuard = new HashSet<uint>();
            var processList = new LinkedList<Win32.ProcessEntry32>();
            uint processId = Win32.Kernel32.GetCurrentProcessId();

            // Starting with the current process, build a parentage chain back to the initial system process.
            // The resulting list will be a list of processes in invocation order starting with system, and ending with the current process.
            // Since cycles are possible, use `cycleGuard` as a guard - if a process has already been seen, stop building the list.
            while (cycleGuard.Add(processId)
                && processTable.ContainsKey(processId))
            {
                processEntry = processEntries[processId];

                processList.AddFirst(processEntry);

                processId = processEntry.ParentProcessId;
            }

            return processList;
        }

        internal bool ReadProcessMemory(Win32.SafeProcessHandle processHandle, out string commandLine, out string imagePath)
        {
            if (processHandle is null)
                throw new ArgumentNullException(nameof(processHandle));

            commandLine = null;
            imagePath = null;

            if (processHandle.IsInvalid)
                return false; 

            // Gloves off...
            unsafe
            {
                int bytesRead = 0;
                long outResult = 0;

                var basicInfo = new Win32.ProcessBasicInformation { };

                // Ask the OS for information about the process, this will include the address of the PEB or
                // Process Environment Block, which contains useful information (like the offset of the process' parameters).
                var hresult = Win32.Ntdll.QueryInformationProcess(processHandle: processHandle,
                                                        processInformationClass: Win32.ProcessInformationClass.BasicInformation,
                                                             processInformation: &basicInfo,
                                                       processInformationLength: sizeof(Win32.ProcessBasicInformation),
                                                                   returnLength: &outResult);

                if (hresult != Win32.Hresult.Ok)
                {
                    var error = Win32.Kernel32.GetLastError();

                    Trace.WriteLine($"failed to query process information [{error}].");

                    return false;
                }

                var peb = new Win32.ProcessEnvironmentBlock { };

                // Now that we know the offsets of the process' parameters, read it because
                // we want the offset to the image-path and the command-line strings.
                if (!Win32.Kernel32.ReadProcessMemory(processHandle: processHandle,
                                                        baseAddress: basicInfo.ProcessEnvironmentBlock,
                                                             buffer: &peb,
                                                         bufferSize: sizeof(Win32.ProcessEnvironmentBlock),
                                                          bytesRead: out bytesRead)
                    || bytesRead < sizeof(Win32.ProcessEnvironmentBlock))
                {
                    var error = Win32.Kernel32.GetLastError();

                    Trace.WriteLine($"failed to read process environment block [{error}] ({bytesRead:n0} bytes read).");

                    return false;
                }

                var processParameters = new Win32.PebProcessParameters { };

                // Read the process parameters data structure to get the offsets to
                // the image-path and command-line strings.
                if (!Win32.Kernel32.ReadProcessMemory(processHandle: processHandle,
                                                        baseAddress: peb.ProcessParameters,
                                                             buffer: &processParameters,
                                                         bufferSize: sizeof(Win32.PebProcessParameters),
                                                          bytesRead: out bytesRead)
                    || bytesRead < sizeof(Win32.PebProcessParameters))
                {
                    var error = Win32.Kernel32.GetLastError();

                    Trace.WriteLine($"failed to read process parameters [{error}] ({bytesRead:n0} bytes read).");

                    return false;
                }

                byte* buffer = stackalloc byte[4096];

                // Read the image-path string, then use it to produce a new string object.
                // Don't give up if the read fails, move on to the next value - have hope.
                if (!Win32.Kernel32.ReadProcessMemory(processHandle: processHandle,
                                                        baseAddress: processParameters.ImagePathName.Buffer,
                                                             buffer: buffer,
                                                         bufferSize: processParameters.ImagePathName.MaximumSize,
                                                          bytesRead: out bytesRead)
                    || bytesRead != processParameters.ImagePathName.MaximumSize)
                {
                    var error = Win32.Kernel32.GetLastError();

                    Trace.WriteLine($"failed to read process image path [{error}] ({bytesRead:n0} bytes read).");
                }
                else
                {
                    // Only allocate the string object if the read was successful.
                    imagePath = new string((char*)buffer);
                }

                // Read the command-line string, then use it to produce a new string object.
                if (!Win32.Kernel32.ReadProcessMemory(processHandle: processHandle,
                                                        baseAddress: processParameters.CommandLine.Buffer,
                                                             buffer: buffer,
                                                         bufferSize: processParameters.CommandLine.MaximumSize,
                                                          bytesRead: out bytesRead)
                    || bytesRead != processParameters.CommandLine.MaximumSize)
                {
                    var error = Win32.Kernel32.GetLastError();

                    Trace.WriteLine($"failed to read process command line [{error}] ({bytesRead:n0} bytes read).");
                }
                else
                {
                    // Only allocate the string object if the read was successful.
                    commandLine = new string((char*)buffer);
                }
            }

            return true;
        }
    }
}

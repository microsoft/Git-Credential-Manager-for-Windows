/**** Git Process Management Library ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the ""Software""), to deal
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
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
**/

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Alm.Win32
{
    internal enum DesiredAccess : uint
    {
        Unchanged = 0,

        /// <summary>
        /// Required to terminate a process using `<see cref="Kernel32.TerminateProcess(SafeProcessHandle, int)"/>`.
        /// </summary>
        Terminate = 0x00000001,

        /// <summary>
        /// Required to create a thread.
        /// </summary>
        CreateThread = 0x00000002,

        /// <summary>
        /// Required to perform an operation on the address space of a process.
        /// </summary>
        VirtualMemoryOperation = 0x00000008,

        /// <summary>
        /// Required to read memory in a process using ReadProcessMemory.
        /// </summary>
        VirtualMemoryRead = 0x00000010,

        /// <summary>
        /// Required to write to memory in a process using WriteProcessMemory.
        /// </summary>
        VirtualMemoryWrite = 0x00000020,

        /// <summary>
        /// Required to duplicate a handle using <see cref="Kernel32.DuplicateHandle"/>.
        /// </summary>
        DuplicateHandle = 0x00000040,

        /// <summary>
        /// Required to create a process
        /// </summary>
        CreateProcess = 0x00000080,

        /// <summary>
        /// Required to set memory limits using SetProcessWorkingSetSize.
        /// </summary>
        SetQuota = 0x00000100,

        /// <summary>
        /// Required to set certain information about a process, such as its priority class.
        /// </summary>
        SetInformation = 0x00000200,

        /// <summary>
        /// Required to retrieve certain information about a process, such as its token, exit code, and priority class.
        /// </summary>
        QueryInformation = 0x00000400,

        /// <summary>
        /// Required to suspend or resume a process.
        /// </summary>
        SuspendResume = 0x00000800,

        /// <summary>
        /// Required to retrieve certain information about a process.
        /// </summary>
        QueryLimitedInformation = 0x00001000,

        /// <summary>
        /// Required to wait for the process to terminate using the wait functions.
        /// </summary>
        Synchronize = 0x00100000,

        /// <summary>
        /// All possible access rights for a process object.
        /// </summary>
        AllAccess = Terminate
                  | CreateThread
                  | VirtualMemoryOperation
                  | VirtualMemoryRead
                  | VirtualMemoryWrite
                  | DuplicateHandle
                  | CreateProcess
                  | SetQuota
                  | SetInformation
                  | QueryInformation
                  | SuspendResume
                  | QueryLimitedInformation
                  | Synchronize,
    }

    [Flags]
    internal enum DuplicateHandleOptions : uint
    {
        None = 0,

        /// <summary>
        /// Closes the source handle.
        /// <para/>
        /// This occurs regardless of any error status returned.
        /// </summary>
        CloseSource = 0x00000001,

        /// <summary>
        /// Ignores the desiredAccess parameter.
        /// <para/>
        /// The duplicate handle has the same access as the source handle.
        /// </summary>
        SameAccess = 0x00000002,
    }

    /// <summary>
    /// Description of error code values returned by `<see cref="Marshal.GetLastWin32Error"/>`.
    /// </summary>
    internal enum ErrorCode
    {
        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Incorrect function.
        /// </summary>
        InvalidFunction = 1,

        /// <summary>
        /// The system cannot find the file specified.
        /// </summary>
        FileNotFound = 2,

        /// <summary>
        /// The system cannot find the path specified.
        /// </summary>
        PathNotFound = 3,

        /// <summary>
        /// The system cannot open the file.
        /// </summary>
        TooManyOpenFiles = 4,

        /// <summary>
        /// Access is denied.
        /// </summary>
        AccessDenied = 5,

        /// <summary>
        /// The handle is invalid.
        /// </summary>
        InvalidHandle = 6,

        /// <summary>
        /// An attempt was made to load a program with an incorrect format.
        /// </summary>
        BadFormat = 11,

        /// <summary>
        /// The program issued a command but the command length is incorrect.
        /// </summary>
        BadLength = 24,

        /// <summary>
        /// The parameter is incorrect.
        /// </summary>
        InvalidParameter = 87,

        /// <summary>
        /// The pipe has been ended.
        /// </summary>
        BrokenPipe = 109,

        /// <summary>
        /// The data area passed to a system call is too small.
        /// </summary>
        InsufficientBuffer = 122,

        /// <summary>
        /// Cannot create a file when that file already exists.
        /// </summary>
        AlreadyExists = 183,

        /// <summary>
        /// The executable is not compatible with the version of Windows you're running.
        /// </summary>
        MachineTypeMismatch = 216,

        /// <summary>
        /// The pipe is local.
        /// </summary>
        PipeLocal = 229,

        /// <summary>
        /// The pipe state is invalid.
        /// </summary>
        BadPipe = 230,

        /// <summary>
        /// All pipe instances are busy.
        /// </summary>
        PipeBusy = 231,

        /// <summary>
        /// The pipe is being closed.
        /// </summary>
        NoData = 232,

        /// <summary>
        /// No process is on the other end of the pipe.
        /// </summary>
        PipeNotConnected = 233,

        /// <summary>
        /// More data is available.
        /// </summary>
        MoreData = 234,

        /// <summary>
        /// Only part of a ReadProcessMemory or WriteProcessMemory request was completed.
        /// </summary>
        PartialCopy = 299,

        /// <summary>
        /// Attempt to access invalid address.
        /// </summary>
        InvalidAddress = 487,

        /// <summary>
        /// The named pipe has already been connected to.
        /// </summary>
        PipeConnected = 535,

        /// <summary>
        /// Overlapped I/O event is not in a signaled state.
        /// </summary>
        IoIncomplete = 996,

        /// <summary>
        /// Overlapped I/O operation is in progress.
        /// </summary>
        IoPending = 997,

        /// <summary>
        /// Invalid access to memory location.
        /// </summary>
        NoAccess = 998,

        /// <summary>
        /// Invalid flags.
        /// </summary>
        InvalidFlags = 1004,

        /// <summary>
        /// An attempt was made to reference a token that does not exist.
        /// </summary>
        NoToken = 1008,

        /// <summary>
        /// Element not found.
        /// </summary>
        NotFound = 1168,

        /// <summary>
        /// A specified logon session does not exist.
        /// <para/>
        /// It may already have been terminated.
        /// </summary>
        NoSuchLogonSession = 1312,

        /// <summary>
        /// A required privilege is not held by the client.
        /// </summary>
        PrivilegeNotHeld = 1314,

        /// <summary>
        /// The paging file is too small for this operation to complete.
        /// </summary>
        CommitLimit = 1455,

        /// <summary>
        /// Not enough quota is available to process this command.
        /// </summary>
        NotEnoughQuota = 1816,

        /// <summary>
        /// The specified username is invalid.
        /// </summary>
        BadUserName = 2202,
    }

    internal enum Hresult : uint
    {
        Ok = 0x00000000,

        /// <summary>
        /// Not implemented.
        /// </summary>
        NotImplemented = 0x80004001,

        /// <summary>
        /// No such interface supported.
        /// </summary>
        NoInterface = 0x80004002,

        /// <summary>
        /// Invalid pointer.
        /// </summary>
        InvalidPointer = 0x80004003,

        /// <summary>
        /// Operation aborted.
        /// </summary>
        Abort = 0x80004004,

        /// <summary>
        /// Unspecified error.
        /// </summary>
        Fail = 0x80004005,

        /// <summary>
        /// Catastrophic failure.
        /// </summary>
        Unexpected = 0x8000FFFF,

        /// <summary>
        /// General access denied error.
        /// </summary>
        AccessDenied = 0x80070005,

        InvalidHandle = 0x80070006,

        OutOfMemory = 0x8007000E,

        /// <summary>
        /// One or more arguments are invalid.
        /// </summary>
        InvalidArgument = 0x80070057,

        /// <summary>
        /// There is not enough space on the disk.
        /// </summary>
        DiskFull = 0x80070070,
    }

    /// <summary>
    /// Describes an entry from a list of the processes residing in the system address space when a snapshot was taken.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal unsafe struct ProcessEntry32
    {
        /// <summary>
        /// The size of the structure, in bytes.
        /// </summary>
        public int Size;

        /// <summary>
        /// This member is no longer used and is always set to zero.
        /// </summary>
        public uint Usage;

        /// <summary>
        /// The process identifier.
        /// </summary>
        public uint ProcessId;

        /// <summary>
        /// This member is no longer used and is always set to zero.
        /// </summary>
        public UIntPtr DefaultHeapId;

        /// <summary>
        /// This member is no longer used and is always set to zero.
        /// </summary>
        public uint ModuleId;

        /// <summary>
        /// The number of execution threads started by the process.
        /// </summary>
        public uint Threads;

        /// <summary>
        /// The identifier of the process that created this process (its parent process).
        /// </summary>
        public uint ParentProcessId;

        /// <summary>
        /// The base priority of any threads created by this process.
        /// </summary>
        public int PriClassBase;

        /// <summary>
        /// This member is no longer used, and is always set to zero.
        /// </summary>
        public uint Flags;

        /// <summary>
        /// The name of the executable file for the process.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExeFileName;
    }

    /// <summary>
    /// Contains process information.
    /// </summary>
    /// <remarks>
    /// Documentation: https://docs.microsoft.com/en-us/windows/desktop/api/winternl/ns-winternl-_peb
    /// <para/>
    /// Note that this structure does not attempt to mimic the original C structure field by field, instead
    /// it relied on offsets based on the original. This is because much of the original structure is difficult
    /// or impossible to reliable define in C# when pointer size and packing of structs can be determined only
    /// at runtime or during the JITter.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ProcessEnvironmentBlock
    {
        private fixed byte _[256];

        /// <summary>
        /// Gets `<see langword="true"/> if the process is currently being debugged; otherwise `<see langword="false"/>`.
        /// <para/>
        /// It is best to use the `CheckRemoteDebuggerPresent` function instead.
        /// </summary>
        public bool IsBeingDebugged
        {
            get { fixed (byte* p = _) { return p[2] != 0; } }
        }

        /// <summary>
        /// Gets a pointer a `<seealso cref="PebProcessParameters"/>` structure.
        /// </summary>
        public PebProcessParameters* ProcessParameters
        {
            get
            {
                fixed (byte* p = _)
                {
                    return IntPtr.Size == 4
                        ? *((PebProcessParameters**)(p + 0x10))
                        : *((PebProcessParameters**)(p + 0x20));
                }
            }
        }
    }

    /// <summary>
    /// Contains process parameter information.
    /// </summary>
    /// <remarks>
    /// Documentation: https://docs.microsoft.com/en-us/windows/desktop/api/winternl/ns-winternl-_rtl_user_process_parameters
    /// <para/>
    /// Note that this structure does not attempt to mimic the original C structure field by field, instead
    /// it relied on offsets based on the original. This is because much of the original structure is difficult
    /// or impossible to reliable define in C# when pointer size and packing of structs can be determined only
    /// at runtime or during the JITter.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PebProcessParameters
    {
        private fixed byte _[128];

        /// <summary>
        /// Gets the command-line string passed to the process.
        /// </summary>
        public UnicodeString CommandLine
        {
            get
            {
                fixed (byte* p = _)
                {
                    return IntPtr.Size == 4
                        ? *((UnicodeString*)(p + 0x40))
                        : *((UnicodeString*)(p + 0x70));
                }
            }
        }

        /// <summary>
        /// Gets the path of the image file for the process.
        /// </summary>
        public UnicodeString ImagePathName
        {
            get
            {
                fixed (byte* p = _)
                {
                    return IntPtr.Size == 4
                        ? *((UnicodeString*)(p + 0x38))
                        : *((UnicodeString*)(p + 0x60));
                }
            }
        }
    }

    /// <summary>
    /// Retrieves information about the specified process.
    /// </summary>
    /// <remarks>
    /// Documentation: https://docs.microsoft.com/en-us/windows/desktop/api/winternl/nf-winternl-ntqueryinformationprocess#process_basic_information
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ProcessBasicInformation
    {
        private void* _reserved1;
        private void* _pebBaseAddress;
        private void* _reserved2;
        private void* _reserved3;
        private uint* _uniqueProcessId;
        private void* _reserved4;

        /// <summary>
        /// Gets the address of the `<seealso cref="ProcessEnvironmentBlock"/>`.
        /// </summary>
        public void* ProcessEnvironmentBlock
        {
            get { return _pebBaseAddress; }
        }

        /// <summary>
        /// Gets a pointer to the system's unique identifier for this process.
        /// <para/>
        /// Use the `<seealso cref="Kernel32.GetCurrentProcessId(IntPtr)"/>` function to retrieve this information.
        /// </summary>
        public uint* UniqueProcessId
        {
            get { return _uniqueProcessId; }
        }
    }

    internal enum ProcessInformationClass : uint
    {
        /// <summary>
        /// Retrieves a pointer to a `<see cref="ProcessEnvironmentBlock"/>` that can be used to determine whether the specified process is being debugged, and a unique value used by the system to identify the specified process.
        /// </summary>
        BasicInformation = 0,

        Default = BasicInformation,

        DebugPort = 7,

        Wow64Information = 26,

        /// <summary>
        /// Retrieves a `<see cref="UnicodeString"/>` value containing the name of the image file for the process.
        /// </summary>
        ImageFileName = 27,

        BreakOnTermination = 29,

        SubsystemInformation = 75,
    }

    [Flags]
    internal enum ToolhelpSnapshotFlags : uint
    {
        /// <summary>
        /// Includes all heaps of the process specified in `th32ProcessID` in the snapshot.
        /// <para/>
        /// To enumerate the heaps, see Heap32ListFirst.
        /// </summary>
        HeapList = 0x00000001,

        /// <summary>
        /// Includes all processes in the system in the snapshot.
        /// <para/>
        /// To enumerate the processes, see `<see cref="Kernel32.Process32First(SafeSnapshotHandle, ref ProcessEntry32)"/>`.
        /// </summary>
        Process = 0x00000002,

        /// <summary>
        /// Includes all threads in the system in the snapshot. To enumerate the threads, see Thread32First.
        /// <para/>
        /// To identify the threads that belong to a specific process, compare its process identifier to the processId member of the `THREADENTRY32` structure when enumerating the threads.
        /// </summary>
        Thread = 0x00000004,

        /// <summary>
        /// Includes all modules of the process specified in processId in the snapshot.
        /// <para/>
        /// To enumerate the modules, see `Module32First`.
        /// <para/>
        /// If the function fails with `ERROR_BAD_LENGTH`, retry the function until it succeeds.
        /// <para/>
        /// 64-bit Windows: Using this flag in a 32-bit process includes the 32-bit modules of the process specified in processId, while using it in a 64-bit process includes the 64-bit modules.
        /// <para/>
        /// To include the 32-bit modules of the process specified in processId from a 64-bit process, use the `<see cref="Module32"/>` flag.
        /// </summary>
        Module = 0x00000008,

        /// <summary>
        /// Includes all 32-bit modules of the process specified in processId in the snapshot when called from a 64-bit process.
        /// </summary>
        Module32 = 0x00000010,

        /// <summary>
        /// Indicates that the snapshot handle is to be inheritable.
        /// </summary>
        Inherit = 0x80000000,

        /// <summary>
        /// Includes all processes and threads in the system, plus the heaps and modules of the process specified in `th32ProcessID`.
        /// </summary>
        All = HeapList
            | Module
            | Process
            | Thread,
    }

    /// <summary>
    /// Represents a Unicode encoded string.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    internal unsafe struct UnicodeString
    {
        fixed byte _[16];

        /// <summary>
        /// Gets the pointer to the character data buffer.
        /// </summary>
        public char* Buffer
        {
            get
            {
                fixed (byte* p = _)
                {
                    return (IntPtr.Size == 4)
                        ? *((char**)(p + 0x04))
                        : *((char**)(p + 0x08));
                }
            }
        }

        /// <summary>
        /// Gets the length of the string as the count of `<see langword="char"/>` values.
        /// </summary>
        public int Length
        {
            get { fixed (byte* p = _) { return *((ushort*)(p + 0x00)) / sizeof(char); } }
        }

        /// <summary>
        /// Gets the size of the buffer in bytes.
        /// </summary>
        public int MaximumSize
        {
            get { fixed (byte* p = _) { return *((ushort*)(p + 0x02)); } }
        }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(UnicodeString)}: \"{ToString() ?? "<NULL>"}\""; }
        }

        public override string ToString()
        {
            if (Buffer == null)
                return null;

            if (Length == 0)
                return string.Empty;

            return new string(Buffer);
        }
    }
}

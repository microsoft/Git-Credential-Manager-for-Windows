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
    }

    internal enum Hresult : uint
    {
        Ok = 0x00000000,
        NotImplemented = 0x80004001,
        NoInterface = 0x80004002,
        InvalidPointer = 0x80004003,
        Abort = 0x80004004,
        Fail = 0x80004005,
        Unexpected = 0x8000FFFF,
        AccessDenied = 0x80070005,
        InvalidHandle = 0x80070006,
        OutOfMemory = 0x8007000E,
        InvalidArgument = 0x80070057,
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

    [StructLayout(LayoutKind.Explicit, Size = 0x30)]
    internal unsafe struct ProcessEnvironmentBlock
    {
        [FieldOffset(0x02)]
        private byte _offset_0x02;
        [FieldOffset(0x20)]
        private PebProcessParameters* _offset_0x20;

        public bool IsBeingDebugged
        {
            get { return _offset_0x02 != 0; }
        }
        public PebProcessParameters* ProcessParameters
        {
            get { return _offset_0x20; }
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 104)]
    internal unsafe struct PebProcessParameters
    {
        [FieldOffset(0x60)]
        private UnicodeString _offset_0x60;
        [FieldOffset(0x70)]
        private UnicodeString _offset_0x70;

        public UnicodeString CommandLine
        {
            get { return _offset_0x70; }
        }

        public UnicodeString ImagePathName
        {
            get { return _offset_0x60; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ProcessBasicInformation
    {
        private IntPtr _exitStatus;
        private IntPtr _pebBaseAddress;
        private IntPtr _affinityMask;
        private IntPtr _basePriority;
        private UIntPtr _uniqueProcessId;
        private IntPtr _inheritedFromUniqueProcessId;

        public byte* ProcessEnvironmentBlock
        {
            get { return (byte*)_pebBaseAddress.ToPointer(); }
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
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    internal unsafe struct UnicodeString
    {
        [FieldOffset(0x00)]
        private ushort _field1;
        [FieldOffset(0x02)]
        private ushort _field2;
        [FieldOffset(0x08)]
        private IntPtr _field3;

        /// <summary>
        /// Gets the pointer to the character data buffer.
        /// </summary>
        public char* Buffer
        {
            get { return (char*)_field3.ToPointer(); }
        }

        /// <summary>
        /// Gets the length of the string as the count of `<see langword="char"/>` values.
        /// </summary>
        public int Length
        {
            get { return _field1 / sizeof(char); }
        }

        /// <summary>
        /// Gets the size of the buffer in bytes.
        /// </summary>
        public int MaximumSize
        {
            get { return _field2; }
        }
    }
}

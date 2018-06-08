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
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Alm.Win32
{
    internal static class Kernel32
    {
        public static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
        public const string Name = "kernel32.dll";

        /// <summary>
        /// Closes an open object handle.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="handle">
        /// A valid handle to an open object.
        /// </param>
        /// <remarks>
        /// If the function fails, the return value is zero. To get extended error information, call `<see cref="Marshal.GetLastWin32Error"/>`.
        /// </remarks>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "CloseHandle", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(
            [In] IntPtr handle);

        /// <summary>
        /// Takes a snapshot of the specified processes, as well as the heaps, modules, and threads used by these processes.
        /// <para/>
        /// Returns a handle to the snapshot is successful; otherwise `<see cref="IntPtr.Zero"/>` or `<see cref="InvalidHandleValue"/>`.
        /// </summary>
        /// <param name="flags">
        /// The portions of the system to be included in the snapshot.
        /// </param>
        /// <param name="processId">
        /// The process identifier of the process to be included in the snapshot.
        /// <para/>
        /// This parameter can be zero to indicate the current process.
        /// <para/>
        /// This parameter is used when the `<see cref="ToolhelpSnapshotFlags.HeapList"/>`, `<see cref="ToolhelpSnapshotFlags.Module"/>`, `<see cref="ToolhelpSnapshotFlags.Module32"/>`, or `<see cref="ToolhelpSnapshotFlags.All"/>` value is specified.
        /// <para/>
        /// Otherwise, it is ignored and all processes are included in the snapshot.
        /// <para/>
        /// If the specified process is the Idle process or one of the CSRSS processes, this function fails and the last error code is `<see cref="ErrorCode.AccessDenied"/>` because their access restrictions prevent user-level code from opening them.
        /// <para/>
        /// If the specified process is a 64-bit process and the caller is a 32-bit process, this function fails and the last error code is `<see cref="ErrorCode.PartialCopy"/>`.
        /// </param>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "CreateToolhelp32Snapshot", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        public static extern SafeSnapshotHandle CreateToolhelp32Snapshot(
            [In, MarshalAs(UnmanagedType.U4)] ToolhelpSnapshotFlags flags,
            [In, MarshalAs(UnmanagedType.U4)] uint processId);

        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetLastError", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern ErrorCode GetLastError();

        /// <summary>
        /// Retrieves a pseudo handle for the current process.
        /// <para/>
        /// Returns a value that is a pseudo handle to the current process
        /// </summary>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetCurrentProcess", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        public static extern SafeProcessHandle GetCurrentProcess();

        /// <summary>
        /// Retrieves the process identifier of the calling process.
        /// <para/>
        /// Returns a process identifier of the calling process.
        /// </summary>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetCurrentProcessId", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern uint GetCurrentProcessId();

        /// <summary>
        /// Returns the process identifier of the specified process.
        /// </summary>
        /// <param name="processHandle">
        /// A handle to the process.
        /// <para/>
        /// The handle must have the `<seealso cref="DesiredAccess.QueryInformation"/>` or `<seealso cref="DesiredAccess.QueryLimitedInformation"/>` access right.
        /// </param>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetProcessId", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern uint GetProcessId(
            [In] SafeProcessHandle processHandle);

        /// <summary>
        /// Returns the process identifier of the specified process.
        /// </summary>
        /// <param name="processHandle">
        /// A handle to the process.
        /// <para/>
        /// The handle must have the `<seealso cref="DesiredAccess.QueryInformation"/>` or `<seealso cref="DesiredAccess.QueryLimitedInformation"/>` access right.
        /// </param>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetProcessId", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern uint GetProcessId(
            [In] IntPtr processHandle);

        /// <summary>
        /// Opens an existing local process object.
        /// <para/>
        /// Returns an open handle to the specified process if successful; otherwise `<see langword="null"/>` or an invalid/closed handle.
        /// </summary>
        /// <param name="desiredAccess">
        /// The access to the process object.
        /// <para/>
        /// This access right is checked against the security descriptor for the process.
        /// </param>
        /// <param name="inheritHandle">
        /// If this value is `<see langword="true"/>`, processes created by this process will inherit the handle; otherwise, the processes do not inherit this handle.
        /// </param>
        /// <param name="processId">
        /// The identifier of the local process to be opened.
        /// <para/>
        /// If the specified process is the System Process (0x00000000), the function fails and the last error code is `<see cref="ErrorCode.InvalidParameter"/>`.
        /// <para/>
        /// If the specified process is the Idle process or one of the CSRSS processes, this function fails and the last error code is `<see cref="ErrorCode.AccessDenied"/>` because their access restrictions prevent user-level code from opening them.
        /// </param>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "OpenProcess", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        public static extern SafeProcessHandle OpenProcess(
            [In, MarshalAs(UnmanagedType.U4)] DesiredAccess desiredAccess,
            [In, MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            [In, MarshalAs(UnmanagedType.U4)] uint processId);

        /// <summary>
        /// Retrieves information about the first process encountered in a system snapshot.
        /// <para/>
        /// Returns `<see langword="true"/>` if the first entry of the process list has been copied to the buffer; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="snapshotHandle">
        /// A handle to the snapshot returned from a previous call to the `<see cref="CreateToolhelp32Snapshot"/>` function.
        /// </param>
        /// <param name="processEntry">
        /// A pointer to a `<see cref="ProcessEntry32"/>` structure.
        /// <para/>
        /// It contains process information such as the name of the executable file, the process identifier, and the process identifier of the parent process.
        /// </param>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "Process32FirstW", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32First(
            [In] SafeSnapshotHandle snapshotHandle,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref ProcessEntry32 processEntry);

        /// <summary>
        /// Retrieves information about the first process encountered in a system snapshot.
        /// <para/>
        /// Returns `<see langword="true"/>` if the next entry of the process list has been copied to the buffer; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="snapshotHandle">
        /// A handle to the snapshot returned from a previous call to the `<see cref="CreateToolhelp32Snapshot"/>` function.
        /// </param>
        /// <param name="processEntry">
        /// A pointer to a `<see cref="ProcessEntry32"/>` structure.
        /// <para/>
        /// It contains process information such as the name of the executable file, the process identifier, and the process identifier of the parent process.
        /// </param>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "Process32NextW", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32Next(
            [In] SafeSnapshotHandle snapshotHandle,
            [In, Out, MarshalAs(UnmanagedType.Struct)] ref ProcessEntry32 processEntry);


        /// <summary>
        /// Reads data from an area of memory in a specified process.
        /// <para/>
        /// The entire area to be read must be accessible or the operation fails.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`; for extended error information use `<seealso cref="Marshal.GetLastWin32Error()"/>`.
        /// </summary>
        /// <param name="processHandle">
        /// A handle to the process with memory that is being read.
        /// <para/>
        /// The handle must have `<seealso cref="DesiredAccess.VirtualMemoryRead"/>` access to the process.
        /// </param>
        /// <param name="baseAddress">
        /// A pointer to the base address in the specified process from which to read.
        /// <para/>
        /// Before any data transfer occurs, the system verifies that all data in the base address and memory of the specified size is accessible for read access, and if it is not accessible the function fails.
        /// </param>
        /// <param name="buffer">
        /// A buffer that receives the contents from the address space of the specified process.
        /// </param>
        /// <param name="bufferSize">
        /// The number of bytes to be read from the specified process.
        /// </param>
        /// <param name="bytesRead">
        /// The number of bytes transferred into the specified buffer.
        /// </param>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "ReadProcessMemory", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool _ReadProcessMemory(
            [In] SafeProcessHandle processHandle,
            [In] void* baseAddress,
            [In, Out] void* buffer,
            [In] IntPtr bufferSize,
            [Out] out IntPtr bytesRead);

        public static unsafe bool ReadProcessMemory(SafeProcessHandle processHandle, void* baseAddress, void* buffer, int bufferSize, out int bytesRead)
        {
            bool result = _ReadProcessMemory(processHandle, baseAddress, buffer, new IntPtr(bufferSize), out IntPtr outRead);

            bytesRead = result
                ? outRead.ToInt32()
                : 0;

            return result;
        }
    }
}

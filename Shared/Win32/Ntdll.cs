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
    internal static class Ntdll
    {
        public static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
        public const string Name = "ntdll.dll";

        /// <summary>
        /// Retrieves information about the specified process.
        /// <para/>
        /// Returns an NTSTATUS success or error code.
        /// </summary>
        /// <param name="processHandle">
        /// A handle to the process for which information is to be retrieved.
        /// </param>
        /// <param name="processInformationClass">
        /// The type of process information to be retrieved.
        /// </param>
        /// <param name="processInformation">
        /// A pointer to a buffer supplied by the calling application into which the function writes the requested information.
        /// <para/>
        /// The size of the information written varies depending on the data type of the `<paramref name="processInformationClass"/>` parameter.
        /// </param>
        /// <param name="processInformationLength">
        /// The size of the buffer pointed to by the `<paramref name="processInformation"/>` parameter, in bytes.
        /// </param>
        /// <param name="returnLength">
        /// A pointer to a variable in which the function returns the size of the requested information.
        /// <para/>
        /// If the function was successful, this is the size of the information written to the buffer pointed to by the `<paramref name="processInformation"/>` parameter, but if the buffer was too small, this is the minimum size of buffer needed to receive the information successfully.
        /// </param>
        [DllImport(Name, BestFitMapping = false, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "NtQueryInformationProcess", ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        public static extern unsafe Hresult QueryInformationProcess(
            [In] SafeProcessHandle processHandle,
            [In] ProcessInformationClass processInformationClass,
            [In, Out] ProcessBasicInformation* processInformation,
            [In, MarshalAs(UnmanagedType.I4)] int processInformationLength,
            [In, Out, Optional] long* returnLength);
    }
}

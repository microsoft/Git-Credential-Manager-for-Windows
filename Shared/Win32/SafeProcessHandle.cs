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

namespace Microsoft.Alm.Win32
{
    internal class SafeProcessHandle : System.Runtime.InteropServices.SafeHandle, IEquatable<SafeProcessHandle>
    {
        public static readonly SafeProcessHandle CurrentProcessHandle = Kernel32.GetCurrentProcess();
        public static readonly SafeProcessHandle Null = new SafeProcessHandle(IntPtr.Zero, false);

        public SafeProcessHandle()
            : base(IntPtr.Zero, true)
        { }

        public SafeProcessHandle(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        private SafeProcessHandle(IntPtr handle, bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
            SetHandle(handle);
        }

        public bool IsCurrentProcess { get { return this == CurrentProcessHandle; } }

        public override bool IsInvalid { get { return IsClosed || handle == IntPtr.Zero; } }

        public override bool Equals(object obj)
            => Equals(obj as SafeProcessHandle);

        public bool Equals(SafeProcessHandle other)
            => this == other;

        public override int GetHashCode()
            => handle.GetHashCode();

        protected override bool ReleaseHandle()
            => Kernel32.CloseHandle(handle);

        public static bool operator ==(SafeProcessHandle left, SafeProcessHandle right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;

            return left.handle == right.handle;
        }

        public static bool operator !=(SafeProcessHandle left, SafeProcessHandle right)
            => !(left == right);
    }
}

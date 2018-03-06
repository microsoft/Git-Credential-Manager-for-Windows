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
using System.Threading;

namespace Microsoft.Alm.Authentication
{
    public class RuntimeContext
    {
        public static readonly RuntimeContext Default;

        public RuntimeContext(Git.ITrace trace)
            : this()
        {
            if (trace is null)
                throw new ArgumentNullException(nameof(trace));

            _trace = trace;
        }

        private RuntimeContext()
        {
            _id = Interlocked.Increment(ref _count);
            _syncpoint = new object();
        }

        static RuntimeContext()
        {
            Volatile.Write(ref _count, 0);

            Default = new RuntimeContext();
            Default._trace = new Git.Trace(Default);
        }

        private static int _count;
        private readonly int _id;
        private readonly object _syncpoint;
        private Git.ITrace _trace;

        public int Id
        {
            get { return _id; }
        }

        public Git.ITrace Trace
        {
            get { return _trace; }
        }
    }
}

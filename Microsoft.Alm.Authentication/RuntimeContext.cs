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

        internal RuntimeContext(
            IFileSystem fileSystem,
            INetwork network,
            ITrace trace,
            IWhere where)
            : this()
        {
            if (fileSystem is null)
                throw new ArgumentNullException(nameof(fileSystem));
            if (network is null)
                throw new ArgumentNullException(nameof(network));
            if (trace is null)
                throw new ArgumentNullException(nameof(trace));
            if (where is null)
                throw new ArgumentNullException(nameof(where));

            _fileSystem = fileSystem;
            _network = network;
            _trace = trace;
            _where = where;
        }

        private RuntimeContext()
        {
            _syncpoint = new object();

            _id = Interlocked.Increment(ref _count);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static RuntimeContext()
        {
            Volatile.Write(ref _count, 0);

            Default = new RuntimeContext();
            Default.FileSystem = new FileSystem(Default);
            Default.Network = new Network(Default);
            Default.Where = new Where(Default);
            Default.Trace = new Trace(Default);
        }

        private static int _count;
        private IFileSystem _fileSystem;
        private readonly int _id;
        private INetwork _network;
        private readonly object _syncpoint;
        private ITrace _trace;
        private IWhere _where;

        public int Id
        {
            get { return _id; }
        }

        public virtual IFileSystem FileSystem
        {
            get { lock (_syncpoint) return _fileSystem; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(FileSystem));

                lock (_syncpoint)
                {
                    _fileSystem = value;
                }
            }
        }

        public virtual INetwork Network
        {
            get { lock (_syncpoint) return _network; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(Network));

                lock (_syncpoint)
                {
                    _network = value;
                }
            }
        }

        public virtual ITrace Trace
        {
            get { lock (_syncpoint) return _trace; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(Trace));

                lock (_syncpoint)
                {
                    _trace = value;
                }
            }
        }

        public virtual IWhere Where
        {
            get { lock (_syncpoint) return _where; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(Where));

                lock (_syncpoint)
                {
                    _where = value;
                }
            }
        }
    }
}

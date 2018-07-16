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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Alm.Authentication
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public class RuntimeContext
    {
        /// <summary>
        /// The default `<see cref="RuntimeContext"/>` instance.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RuntimeContext Default = Create();

        public RuntimeContext(INetwork network, ISettings settings, IStorage storage, Git.ITrace trace, Git.IUtilities utilities, Git.IWhere where)
            : this()
        {
            if (network is null)
                throw new ArgumentNullException(nameof(network));
            if (settings is null)
                throw new ArgumentNullException(nameof(settings));
            if (storage is null)
                throw new ArgumentNullException(nameof(storage));
            if (trace is null)
                throw new ArgumentNullException(nameof(trace));
            if (utilities is null)
                throw new ArgumentNullException(nameof(utilities));
            if (where is null)
                throw new ArgumentNullException(nameof(where));

            SetService(network);
            SetService(settings);
            SetService(storage);
            SetService(trace);
            SetService(utilities);
            SetService(where);
        }

        private RuntimeContext()
        {
            _id = Interlocked.Increment(ref _count);
            _services = new Dictionary<Type, IRuntimeService>();
            _syncpoint = new object();
        }

        private static int _count = 0;
        private readonly int _id;
        private readonly Dictionary<Type, IRuntimeService> _services;
        private readonly object _syncpoint;

        public int Id
        {
            get { return _id; }
        }

        public INetwork Network
        {
            get { return GetService<INetwork>(); }
            internal set { SetService(value); }
        }

        public ISettings Settings
        {
            get { return GetService<ISettings>(); }
            internal set { SetService(value); }
        }

        public IStorage Storage
        {
            get { return GetService<IStorage>(); }
            internal set { SetService(value); }
        }

        public Git.ITrace Trace
        {
            get { return GetService<Git.ITrace>(); }
            internal set { SetService(value); }
        }

        public Git.IUtilities Utilities
        {
            get { return GetService<Git.IUtilities>(); }
            internal set { SetService(value); }
        }

        public Git.IWhere Where
        {
            get { return GetService<Git.IWhere>(); }
            internal set { SetService(value); }
        }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(RuntimeContext)}: Id = {_id}, Count = {_services.Count}"; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static RuntimeContext Create()
        {
            var context = new RuntimeContext();

            context.SetService<INetwork>(new Network(context));
            context.SetService<ISettings>(new Settings(context));
            context.SetService<IStorage>(new Storage(context));
            context.SetService<Git.ITrace>(new Git.Trace(context));
            context.SetService<Git.IUtilities>(new Git.Utilities(context));
            context.SetService<Git.IWhere>(new Git.Where(context));

            return context;
        }

        internal IEnumerable<IRuntimeService> EnumerateServices()
        {
            List<IRuntimeService> services;

            lock (_syncpoint)
            {
                services = new List<IRuntimeService>(_services.Values.Count);

                foreach (var service in _services.Values)
                {
                    services.Add(service);
                }
            }

            return services;
        }

        internal T GetService<T>() where T : class, IRuntimeService
        {
            lock (_syncpoint)
            {
                _services.TryGetValue(typeof(T), out IRuntimeService service);

                return service as T;
            }
        }

        internal void SetService<T>(T service) where T : class, IRuntimeService
        {
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            lock (_syncpoint)
            {
                if (_services.ContainsKey(service.ServiceType))
                {
                    _services[service.ServiceType] = service;
                }
                else
                {
                    _services.Add(service.ServiceType, service);
                }
            }
        }
    }
}
